#define X

using System;
using System.Linq;
using ChessChallenge.API;

/*
 * So here we go, 8 august, let's start from scratch, let's name this project NebulaAI
 * V1: NegaMax, Q Search, Move ordering, Piece Square Tables and Transposition Tables
 * V2: Null move pruning, History heuristics
 * V3: Reversed futility pruning, futility pruning and from NegaMax to PVS
 * V4: Killer moves, check extensions, razoring, time management
 * V5: Late move reduction, token savings
 * V6: Span<Move> instead of Linq, allowNull to prevent multiple nmp in a search
 */
public class MyBot : IChessBot
{
#if X
    public int Nodes;
    public int QNodes;
#endif

    // Transposition Table: keep track of positions that were already calculated and possibly re-use information
    // Token optimised: Key, Score, Depth, Flag, Move
    private (ulong, int, int, int, Move)[] _tt = new (ulong, int, int, int, Move)[0x400000];

    // History Heuristics: keep track on great moves that caused a cutoff to retry them
    // Based on a lookup by color, piece type and target square
    public int[,,] Hh;

    // Killer moves: keep track on great moves that caused a cutoff to retry them
    // Based on a lookup by depth
    public Move[] Killers = new Move[2048];

    // The current legal moves ordered by their score
    public readonly int[] MoveScores = new int[256];

    // Globals
    public Timer Timer;
    public Board Board;
    public int TimeLimit;

    // Keep track on the best move
    public Move BestMove;

    public int Pvs(int depth, int plyFromRoot, int alpha, int beta, bool allowNull)
    {
        // Reuse search variables
        bool notRoot = plyFromRoot++ > 0,
            canFutilityPrune = false,
            inCheck = Board.IsInCheck();

        // Draw detection first since TT doesn't know how many times a certain position was played
        if (notRoot && Board.IsRepeatedPosition()) return 0;

        // Try to find the board position in the tt
        ulong zobristKey = Board.ZobristKey;
        ref var ttEntry = ref _tt[zobristKey & 0x3FFFFF];

        // Declare search variables
        int alphaStart = alpha,
            bestEval = -100_000,
            movesSearched = 0,
            movesScored = 0,
            newScore,
            entryScore = ttEntry.Item2,
            entryFlag = ttEntry.Item4;

        // Using local method to simplify multiple similar calls to the Pvs (to combine with Late move reduction)
        int Search(int newAlpha, int reduction = 1, bool canNull = true) =>
            newScore = -Pvs(depth - reduction, plyFromRoot, -newAlpha, -alpha, canNull);

        // Transposition lookup, do not try to get mate scores since their ply is not accurate
        if (ttEntry.Item1 == zobristKey && ttEntry.Item3 >= depth && notRoot && (
                entryFlag == 1 ||
                entryFlag == 2 && entryScore <= alpha ||
                entryFlag == 3 && entryScore >= beta))
            return entryScore;

        // Check extensions
        if (inCheck)
            depth++;

        // Search quiescence position to prevent horizon effect
        bool inQSearch = depth < 1;
        if (inQSearch)
        {
            bestEval = Evaluate();
            if (beta <= bestEval) return bestEval;
            alpha = Math.Max(alpha, bestEval);
        }
        // No pruning in QSearch 
        // If this node is not part of the PV and not in check (too unstable to judge)
        else if (beta - alpha == 1 && !inCheck)
        {
            int staticEval = Evaluate();

            // Reverse futility pruning: if our position is much better than beta, even if we start losing material every depth
            // we'd still be above beta, so cutoff since unlikely opponent will allow us this path
            if (depth < 4 && beta <= staticEval - 100 * depth)
                return staticEval;

            // Null move pruning on pre-frontier nodes
            // When the side who got a free move can't even improve after a null move we prune this branch
            if (depth > 1 && allowNull)
            {
                Board.ForceSkipTurn();
                // Depth is 1 - R(eduction), we'll use 2 for now and scale it a bit with depth
                Search(beta, 3 + depth / 4, false);
                Board.UndoSkipTurn();
                if (beta <= newScore) return newScore;
            }

            // Futility pruning: if our position is so bad that even if we improve a lot
            // and we can't improve alpha, so we'll give up on this branch
            canFutilityPrune = depth == 1 && staticEval + 300 <= alpha;

            // Classic razoring in pre-pre-frontier node by rook margin
            if (depth == 3 && staticEval + 500 <= alpha)
                depth--;
        }

        // Generate appropriate moves depending on whether we're in QSearch
        Span<Move> moveSpan = stackalloc Move[256];
        Board.GetLegalMovesNonAlloc(ref moveSpan, inQSearch);

        // Order moves in reverse order -> negative values are ordered higher hence the flipped values
        foreach (Move move in moveSpan)
            MoveScores[movesScored++] = -(
                move == ttEntry.Item5 ? 9_000_000 :
                move.IsPromotion ? 8_000_000 :
                move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                Killers[plyFromRoot] == move ? 900_000 :
                Hh[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index]
            );

        MoveScores.AsSpan(0, moveSpan.Length).Sort(moveSpan);

        // Performant way to check for stalemate and checkmate
        if (!inQSearch && moveSpan.IsEmpty) return inCheck ? plyFromRoot - 100_000 : 0;

        Move bestMove = default;
        foreach (Move move in moveSpan)
        {
#if X
            if (depth > 0) Nodes++;
            else QNodes++;
#endif

            // Futility prune on non tactical nodes and never on first move
            if (canFutilityPrune && !(movesSearched == 0 || move.IsCapture || move.IsPromotion)) continue;

            Board.MakeMove(move);

            // PVS + LMR

            // Full search in Q search or on first move
            if (movesSearched++ == 0 || inQSearch)
                Search(beta);
            else
            {
                // Late move reduction search
                if (movesSearched > 6 && depth > 2) Search(alpha + 1, 3 + depth / 4);
                // Hack to ensure we'll go into a zero window search
                else newScore = alpha + 1;

                // Zero window search when reduced search improved alpha (or alpha hacked see above)
                if (newScore > alpha && Search(alpha + 1) > alpha)
                    Search(beta);
            }

            Board.UndoMove(move);

            if (newScore > bestEval)
            {
                if (!notRoot) BestMove = move;

                bestMove = move;
                bestEval = newScore;
                alpha = Math.Max(alpha, bestEval);

                // Beta cutoff, move is too good, opposing player has a better option (beta) and won't play this subtree
                if (beta <= alpha)
                {
                    if (!move.IsCapture)
                    {
                        Hh[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                        Killers[plyFromRoot] = move;
                    }

                    break;
                }
            }

            // Out of time break out of the loop
            if (Timer.MillisecondsElapsedThisTurn > TimeLimit) return 100_000;
        }

        // Save position to transposition table, Key, Score, Depth, Flag, Move
        ttEntry = new(
            zobristKey,
            bestEval,
            depth,
            bestEval >= beta ? 3 : bestEval <= alphaStart ? 2 : 1,
            bestMove == default ? ttEntry.Item5 : bestMove);

        return bestEval;
    }

    public Move Think(Board board, Timer timer)
    {
#if X
        Nodes = 0;
        QNodes = 0;
#endif

        Timer = timer;
        Board = board;

        TimeLimit = Timer.MillisecondsRemaining / 30;

        // Empty / Initialise HH every new turn
        Hh = new int[2, 7, 64];

        for (int depth = 1, alpha = -100_000, beta = 100_000;;)
        {
            int score = Pvs(depth, 0, alpha, beta, true);

            // Out of time
            if (Timer.MillisecondsElapsedThisTurn > TimeLimit)
                break;

            if (score <= alpha) alpha -= 100;
            else if (score >= beta) beta += 100;
            else
            {
                alpha = score - 20;
                beta = score + 20;
                depth++;
            }
        }

        return BestMove;
    }

    #region Evaluation

    // Each piece taken off the board will count towards the endgame strategy
    private readonly int[] _gamePhaseWeight = { 0, 1, 1, 2, 4, 0 };

    //  P   N    B    R     Q    K
    private readonly short[] _pieceValues =
    {
        82, 337, 365, 477, 1025, 0,
        94, 281, 297, 512, 936, 0
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
        // Variables re-used during evaluate
        int mg = 0, eg = 0, gamePhase = 0, sideToMove = 2, perspective = Board.IsWhiteToMove ? 1 : -1;

        // Loop the two sides that have to move (white and black)
        // Flip score for optimised token count (always white perspective due to double flip)
        // Eg. White eval = 2300 -> flip -> -2300 -> black eval = 2000 -> -300 -> flip -> 300 k)
        for (; --sideToMove >= 0; mg = -mg, eg = -eg)
        {
            for (int piece = -1; ++piece < 6;)
            for (ulong mask = Board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
            {
                // The less pieces, the more we bend towards our endgame piece square tables
                gamePhase += _gamePhaseWeight[piece];

                // A number between 0 to 63 that indicates which square the piece is on, flip for black
                int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;

                // Piece (material) values are baked into the PST (!)
                mg += _pst[squareIndex][piece];
                eg += _pst[squareIndex][piece + 6];
            }
        }

        // Tapered eval
        return (mg * gamePhase + eg * (24 - gamePhase)) / 24 * perspective +
               gamePhase / 2;
    }

    #endregion
}