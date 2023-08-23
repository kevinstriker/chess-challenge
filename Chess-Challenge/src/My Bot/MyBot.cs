using System;
using System.Linq;
using ChessChallenge.API;

/*
 * So here we go, 8 august, let's start from scratch, let's name this project NebulaAI
 * V1: NegaMax, Q Search, Move ordering, Piece Square Tables and Transposition Tables
 * V2: Null move pruning, History heuristics
 * V3: Reversed futility pruning, futility pruning and from NegaMax to PVS
 * V4: Killer moves, check extensions, razoring, time management
 * V5: Pruning improvements, token savings
 */
public class MyBot : IChessBot
{
    public int Nodes;
    public int QNodes;

    // Transposition Table: keep track of positions that were already calculated and possibly re-use information
    record struct TtEntry(ulong Key, int Score, int Depth, int Flag, Move BestMove);

    TtEntry[] _tt = new TtEntry[0x400000];

    // History Heuristics: keep track on great moves that caused a cutoff to retry them
    // Based on a lookup by color, piece type and target square
    public int[,,] HistoryHeuristics;

    // Killer moves: keep track on great moves that caused a cutoff to retry them
    // Based on a lookup by depth
    Move[] _killers = new Move[256];

    // Globals
    public Timer Timer;
    public Board Board;
    public int TimeLimit;

    // Keep track on the best move
    public Move BestMove;

    public int Pvs(int depth, int plyFromRoot, int alpha, int beta)
    {
        // Reuse search variables
        bool notRoot = plyFromRoot++ > 0,
            canFutilityPrune = false,
            isPv = beta - alpha > 1,
            inCheck = Board.IsInCheck();
        int alphaStart = alpha,
            bestEval = -100_000,
            movesSearched = 0;

        // Check for repetition since TT doesn't know that and we don't want draws when we can win
        if (notRoot && Board.IsRepeatedPosition()) return 0;

        // Try to find the board position in the tt
        ulong zobristKey = Board.ZobristKey;
        var (ttKey, ttScore, ttDepth, ttFlag, ttBestMove) = _tt[zobristKey % 0x400000];

        // When we find the transposition check if we can use it to narrow our alpha beta bounds
        if (notRoot && ttKey == zobristKey && ttDepth >= depth)
        {
            // 1 = lower bound; 2 = exact; 3 = upper bound
            switch (ttFlag)
            {
                case 1:
                    alpha = Math.Max(alpha, ttScore);
                    break;
                case 2:
                    return ttScore;
                case 3:
                    beta = Math.Min(beta, ttScore);
                    break;
            }

            // Beta cutoff, move is too good, opposing player has a better option (beta) and won't play this subtree
            if (beta <= alpha) return ttScore;
        }

        // Check extensions
        if (inCheck)
            depth++;

        // Search quiescence position to prevent horizon effect
        bool inQSearch = depth < 1;
        if (inQSearch)
        {
            bestEval = Evaluate();
            alpha = Math.Max(alpha, bestEval);
            if (beta <= bestEval) return bestEval;
        }
        // No pruning in QSearch and not when there is a check (unstable situation)
        else if (!isPv && !inCheck)
        {
            int staticEval = Evaluate();

            // Reverse futility pruning: if our position is much better than beta and even if we start losing material 
            // every depth from now and we'd still be above beta, cut it off.
            if (depth < 4 && beta <= staticEval - 100 * depth)
                return staticEval;

            // Null move pruning on pre-frontier nodes and higher
            if (depth > 1)
            {
                Board.ForceSkipTurn();
                // depth - (1 + R(eduction)), using the classic 2 for reduction
                int nullMoveEval = -Pvs(depth - 3, plyFromRoot, -beta, 1 - beta);
                Board.UndoSkipTurn();
                // Prune branch when the side who got a free move can't even improve
                if (beta <= nullMoveEval) return nullMoveEval;
            }

            // Futility pruning: if our position is so bad that even if we improve a lot
            // We can't improve alpha, so we'll give up on this branch
            // It's the pure form, only depth 1, classic minor piece value
            if (depth == 1)
                canFutilityPrune = staticEval + 300 <= alpha;

            // Classic razoring in pre-pre-frontier node by rook margin
            if (depth == 3 && staticEval + 500 <= alpha)
                depth--;
        }

        // Move Ordering
        var moves = Board.GetLegalMoves(inQSearch).OrderByDescending(
            move =>
                move == ttBestMove ? 9_000_000 :
                move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                move.IsPromotion ? 1_000_000 :
                _killers[plyFromRoot] == move ? 900_000 :
                HistoryHeuristics[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index]
        ).ToArray();

        Move bestMove = default;
        foreach (Move move in moves)
        {
            // On certain nodes (tactical nodes), static eval, even with a wide margin, isn't safe enough to exclude
            bool tactical = move.IsCapture || move.IsPromotion;

            // Futility prune on non tactical nodes and never on first move
            if (canFutilityPrune && !tactical && movesSearched > 0) continue;

            // Debug
            if (depth > 0) Nodes++;
            else QNodes++;

            Board.MakeMove(move);

            // Principle Variation Search 
            bool isFullSearch = inQSearch || movesSearched++ == 0;
            int score = -Pvs(depth - 1, plyFromRoot, isFullSearch ? -beta : -alpha - 1, -alpha);
            
            // When we improved alpha with our small window search we'll have it fully searched
            if (!isFullSearch && score > alpha)
                score = -Pvs(depth - 1, plyFromRoot, -beta, -alpha);

            Board.UndoMove(move);

            if (score > bestEval)
            {
                if (!notRoot) BestMove = move;
                bestMove = move;
                bestEval = score;
                alpha = Math.Max(alpha, bestEval);

                // Beta cutoff, move is too good, opposing player has a better option (beta) and won't play this subtree
                if (beta <= alpha)
                {
                    if (!move.IsCapture)
                    {
                        HistoryHeuristics[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index] +=
                            depth * depth;
                        _killers[plyFromRoot] = move;
                    }

                    break;
                }
            }

            // Out of time break out of the loop
            if (Timer.MillisecondsElapsedThisTurn > TimeLimit) return 100_000;
        }

        // Performant way to check for stalemate and checkmate
        if (!inQSearch && moves.Length == 0) return inCheck ? plyFromRoot - 100_000 : 0;

        // Insert entry in tt
        _tt[zobristKey % 0x400000] = new TtEntry(zobristKey,
            bestEval,
            depth,
            bestEval <= alphaStart ? 3 : bestEval >= beta ? 1 : 2,
            bestMove);

        return bestEval;
    }

    public Move Think(Board board, Timer timer)
    {
        Nodes = 0;
        QNodes = 0;

        Timer = timer;
        Board = board;

        TimeLimit = timer.MillisecondsRemaining / 30;

        // Empty / Initialise HH every new turn
        HistoryHeuristics = new int[2, 7, 64];

        // Iterative deepening
        for (int depth = 1; depth < 50; depth++)
        {
            int score = Pvs(depth, 0, -100000, 100000);

            if (Timer.MillisecondsElapsedThisTurn > TimeLimit) break;
        }

        return BestMove;
    }

    #region Evaluation

    // Each piece taken off the board will count towards the endgame strategy
    private readonly int[] _gamePhaseIncrement = { 0, 1, 1, 2, 4, 0 };

    //                                        P   K    B    R    Q     K
    private readonly short[] _pieceValues =
    {
        82, 337, 365, 477, 1025, 20000,
        94, 281, 297, 512, 936, 20000
    };

    // The unpacked piece square lookup table
    private readonly int[][] _pst;

    public MyBot()
    {
        _pst = new[]
        {
            63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m,
            75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m,
            936945638387574698250991104m, 75531285965747665584902616832m, 77047302762000299964198997571m,
            3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m,
            3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m,
            2475894077091727551177487608m, 2458978764687427073924784380m, 3718684080556872886692423941m,
            4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m,
            9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
            77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m,
            5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m,
            5619082524459738931006868492m, 649197923531967450704711664m, 75809334407291469990832437230m,
            78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m,
            5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m,
            76772801025035254361275759599m, 75502243563200070682362835182m, 78896921543467230670583692029m,
            2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m,
            3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
            73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m,
            3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m,
            78580145051212187267589731866m, 75798434925965430405537592305m, 68369566912511282590874449920m,
            72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m,
            73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m,
            70529879645288096380279255040m,
        }.Select(packedTable =>
            new System.Numerics.BigInteger(packedTable).ToByteArray().Take(12)
                .Select(square => (int)((sbyte)square * 1.461) + _pieceValues[TimeLimit++ % 12])
                .ToArray()
        ).ToArray();
    }

    private int Evaluate()
    {
        int mg = 0, eg = 0, phase = 0, sideToMove = 2, piece, squareIndex;
        for (; --sideToMove >= 0; mg = -mg, eg = -eg)
        for (piece = -1; ++piece < 6;)
        for (ulong mask = Board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
        {
            // The less pieces, the more we bend towards our endgame strategy
            phase += _gamePhaseIncrement[piece];

            // A number between 0 to 63 that indicates which square the piece is on, flip for black
            squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;

            // Piece values are baked into the pst (see constructor of the bot)
            mg += _pst[squareIndex][piece];
            eg += _pst[squareIndex][piece + 6];
        }

        // Tapered eval
        return (mg * phase + eg * (24 - phase)) / 24 * (Board.IsWhiteToMove ? 1 : -1);
    }

    #endregion
}