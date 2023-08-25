// #define TUNING
// #define LOGGING

using System;
using System.Linq;
using ChessChallenge.API;

public class JeffBot : IChessBot
{
    
    public int Nodes;
#if LOGGING
    public int QNodes;
#endif

#if TUNING
    private ConcurrentDictionary<string, int> parameters = new();

    public void setValue(string name, int value)
    {
        parameters[name] = value;
    }
#endif

    // Transposition Table: keep track of positions that were already calculated and possibly re-use information
    // Key, Score, Depth, Flag, BestMove
    private (ulong, int, int, int, Move)[] _tt = new (ulong, int, int, int, Move)[0x400000];

    // History Heuristics: keep track on great moves that caused a cutoff to retry them
    // Based on a lookup by color, piece type and target square
    public int[,,] HistoryHeuristics;

    // Killer moves: keep track on great moves that caused a cutoff to retry them
    // Based on a lookup by depth
    Move[] _killers = new Move[256];

    // Globals
    public Timer timer;
    public Board board;
    public int TimeLimit;

    // Keep track on the best move
    public Move BestMove;

    public int Pvs(int depth, int plyFromRoot, int alpha, int beta)
    {
        // Reuse search variables
        bool notRoot = plyFromRoot++ > 0,
            canPrune = false,
            isZw = beta - alpha <= 1,
            inCheck = board.IsInCheck();
        int alphaStart = alpha,
            bestEval = -100_000,
            movesSearched = 0;

        Move bestMove = default;

        // Check for repetition since TT doesn't know that and we don't want draws when we can win
        if (notRoot && board.IsRepeatedPosition() || plyFromRoot > 50) return 0;

        // Try to find the board position in the tt
        ulong zobristKey = board.ZobristKey;
        
        var (ttKey, ttScore, ttDepth, ttFlag, ttMove) = _tt[zobristKey % 0x400000];
        
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
        else if (isZw && !inCheck)
        {
            int staticEval = Evaluate();

            // Reverse futility pruning: if our position is much better than beta and even if we start losing material 
            // every depth from now and we'd still be above beta, cut it off.
            // Use symbolic value of 100 centi pawn per ply
#if TUNING
            if (depth < parameters["RFPMaxDepth"] && beta <= staticEval - parameters["RFPMargin"] * depth)
#else
            if (depth < 6 && beta <= staticEval - 216 * depth) // RFP DEPTH TUNED < 6, RFP MARGIN TUNED 216
#endif
                return staticEval;

            // Null move pruning on pre-frontier nodes and higher
            if (depth > 1) // NMP MIN DEPTH TUNED 1
            {
                board.ForceSkipTurn();
#if TUNING
                int nullMoveEval = -Pvs(depth - parameters["NMPDepthReduction"], plyFromRoot, -beta, 1 - beta);
#else
                int nullMoveEval = -Pvs(depth - 3, plyFromRoot, -beta, 1 - beta); // NMP DEPTH REDUCTION TUNED 3
#endif
                board.UndoSkipTurn();
                // Prune branch when the side who got a free move can't even improve
                if (beta <= nullMoveEval) return nullMoveEval;
            }

            // Futility pruning: if our position is so bad that even if we improve a lot
            // We can't improve alpha, so we'll give up on this branch
            // It's the pure form, only depth 1, classic minor piece value
#if TUNING
            if (depth == parameters["FutilityPruningDepth"])
                canPrune = staticEval + parameters["FutilityPruningMargin"] <= alpha;
#else
            if (depth <= 1) // TODO tune 1
                canPrune = staticEval + 300 <= alpha; // TODO tune 1
#endif

            // // Classic razoring in pre-pre-frontier node by x margin, who knows
            // if (depth == 3 && staticEval + 500 <= alpha)
            //     depth--;
        }

        var movesWithKeys = board.GetLegalMoves(inQSearch)
            .Select(move => (
                move,
                move == ttMove ? 9_000_000 :
                move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                move.IsPromotion ? 1_000_000 :
                _killers[plyFromRoot] == move ? 900_000 :
                HistoryHeuristics[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index]
            )).ToArray();
        // order in descending order
        Array.Sort(movesWithKeys, (a, b) => b.Item2.CompareTo(a.Item2));
        
        foreach (var (move, moveScore) in movesWithKeys)
        // foreach (var move in moves)
        {
            // On certain nodes (tactical nodes), static eval, even with a wide margin, isn't safe enough to exclude
            bool tactical = move.IsCapture || move.IsPromotion;
            
            // LMP - Late Move Pruning
            if (!inQSearch && isZw && moveScore < 900_000 && movesSearched > 25 + depth * depth) break; // also try !qsearch

            // Only futility prune on non tactical nodes and when we've fully searched 1 line to prevent pruning everything
            if (canPrune && !tactical && movesSearched > 0) continue;
            
#if LOGGING
            if (depth > 0) Nodes++;
            else QNodes++;
#else
            Nodes++;
#endif

            board.MakeMove(move);
            

            // Principle Variation Search: search our first moves fully with normal bounds
            // After we're using a small window search (performant) to know if it has potential
            // Also later moves (movesSearched > 5) will have less depth (late move reduction)
            bool isFullSearch = inQSearch || movesSearched++ == 0;
#if TUNING
            int lmr = !isFullSearch && !tactical && !inCheck && movesSearched > parameters["LMRinMovesSearched"]
                ? Math.ILogB(movesSearched)
                : 0;
#else
            int lmr = !isFullSearch && !tactical && !inCheck && movesSearched > 5
                ? Math.ILogB(movesSearched) * Math.ILogB(depth) / 2
                : 0; // TODO tune 2
#endif
            // int lmr = !isFullSearch && !tactical && !inCheck && movesSearched > 5 ? 1 : 0;
            int score = -Pvs(depth - 1 - lmr, plyFromRoot, isFullSearch || lmr > 0 ? -beta : -alpha - 1, -alpha);
            
            // If the branch has potential, if it can improve alpha, we'll need to fully search it for exact score
            if (!isFullSearch && score > alpha && (!isZw || lmr > 0))
                score = -Pvs(depth - 1, plyFromRoot, -beta, -alpha);
            board.UndoMove(move);
            
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
                        HistoryHeuristics[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                        _killers[plyFromRoot] = move;
                    }
                    break;
                }
            }

            // Out of time break out of the loop
            if (Nodes % 2048 == 0 && timer.MillisecondsElapsedThisTurn > TimeLimit) return 100_000;
        }

        // Performant way to check for stalemate and checkmate
        if (!inQSearch && movesWithKeys.Length == 0) return inCheck ? plyFromRoot - 100_000 : 0;
        
        _tt[zobristKey % 0x400000] = (
            zobristKey,
            bestEval,
            depth,
            bestEval <= alphaStart ? 3 : bestEval >= beta ? 1 : 2,
            bestMove
        );

        return bestEval;
    }

    public Move Think(Board Board, Timer Timer)
    {
        Nodes = 0;
#if LOGGING
        QNodes = 0;
#endif
        timer = Timer;
        board = Board;

        // Amount of time for this search
        TimeLimit = timer.MillisecondsRemaining / 30;

        // Initialise HH every time we're trying to find a move with Iterative Deepening
        HistoryHeuristics = new int[2, 7, 64];

        // Reset to prevent lingering previous moves
        BestMove = board.GetLegalMoves()[0];

        // Iterative deepening
        for (int depth = 9;;)
        {
#if LOGGING
            int score = Pvs(depth, 0, -100000, 100000);
            DebugHelper.LogDepth(timer, depth++, score, this);
#else
            Pvs(depth++, 0, -100000, 100000);
#endif

            if (timer.MillisecondsElapsedThisTurn * 2 > TimeLimit)
                break;
        }

        return BestMove;
    }
    
    // PST Packer and Un-packer - credits to Tyrant
    readonly int[] _phaseWeight = { 0, 1, 1, 2, 4, 0 };

    // Pawn, Knight, Bishop, Rook, Queen, King 
    private readonly short[] _pieceValues =
    {
        82, 337, 365, 477, 1025, 20000,
        94, 281, 297, 512, 936, 20000
    };

    private readonly decimal[] _packedPst =
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
    };

    private readonly int[][] _pst;

    private int Evaluate()
    {
        int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
        for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
        for (piece = -1; ++piece < 6;)
        for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
        {
            // Gamephase, middlegame -> endgame
            gamephase += _phaseWeight[piece];

            // Material and square evaluation
            square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
            middlegame += _pst[square][piece];
            endgame += _pst[square][piece + 6];
        }

        // Tempo bonus to help with aspiration windows
        return (middlegame * gamephase + endgame * (24 - gamephase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }
    public JeffBot()
    {
        _pst = _packedPst.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(bit => BitConverter.GetBytes(bit)
                    .Select(square => (int)((sbyte)square * 1.461) + _pieceValues[pieceType++]))
                .ToArray();
        }).ToArray();
    }
}