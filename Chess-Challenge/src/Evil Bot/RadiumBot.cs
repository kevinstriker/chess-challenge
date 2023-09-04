#define INFO

using System;
using System.Linq;
using ChessChallenge.API;

public class RadiumBot : IChessBot
{
    Board board;
    Timer timer;
    Move bestmoveRoot;
    record struct TTEntry(ulong key, Move move, int depth, int score, int bound);
    TTEntry[] tt = new TTEntry[0x400000];
    short[] pvm = { 82, 337, 365, 477, 1025, 0,
                    94, 281, 297, 512,  936, 0 };
    int[][] UnpackedPestoTables;
    int[,,] HistoryHeuristics;
    Move[] killer;
    int searchMaxTime;
#if INFO
    long nodes; // #DEBUG
#endif
    int Evaluate()
    {
        int mg = 0, eg = 0, phase = 0;
        foreach (bool stm in new[] { true, false })
        {
            for (int piece = -1; ++piece < 6;)
            {
                ulong bb = board.GetPieceBitboard((PieceType)(piece + 1), stm);
                while (bb != 0)
                {
                    int sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (stm ? 56 : 0);
                    mg += UnpackedPestoTables[sq][piece];
                    eg += UnpackedPestoTables[sq][piece + 6];
                    phase += 0x00042110 >> piece * 4 & 0x0F;
                    
                }
            }

            mg = -mg;
            eg = -eg;
        }
        phase = Math.Min(phase, 24);
        return (mg * phase + eg * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24) + phase / 2;
    }

    public int PVS(int alpha, int beta, int depth, int ply, bool canNullMove = true)
    {
#if INFO
        nodes++; // #DEBUG
#endif
        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0;
        bool notRoot = ply > 0;
        int best = -9999999;
        int stm = board.IsWhiteToMove ? 0 : 1;
        bool canFPrune = false;

        if (notRoot && board.IsRepeatedPosition())
            return 0;

        TTEntry entry = tt[key & 0x3FFFFF];

        if (notRoot && entry.key == key && entry.depth >= depth && (
            entry.bound == 3
                || entry.bound == 2 && entry.score >= beta
                || entry.bound == 1 && entry.score <= alpha
        )) return entry.score;

        int eval = Evaluate();

        int Search(int newAlpha, int R = 1, bool canNull = true) => eval = -PVS(-newAlpha, -alpha, depth-R, ply+1, canNull);

        if (qsearch)
        {
            best = eval;
            if (best >= beta) return best;
            alpha = Math.Max(alpha, best);
        }

        else if (beta - alpha == 1 && !board.IsInCheck())
        {
            int staticEval = Evaluate();
            if (depth <= 10 && staticEval - 94 * depth >= beta)
                return staticEval;

            if (canNullMove && depth >= 2)
            {
                board.ForceSkipTurn();
                Search(beta, 3 + (depth >> 2), false);
                board.UndoSkipTurn();

                if (beta <= eval)
                    return eval;
            }
            canFPrune = depth <= 8 && staticEval + depth * 141 <= alpha;
        }

        var moves = board.GetLegalMoves(qsearch).OrderByDescending(
            move =>
                move == entry.move ? 9000000 :
                move.IsCapture ? 100000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                move.IsPromotion ? 1000000 : HistoryHeuristics[stm, move.StartSquare.Index, move.TargetSquare.Index]
        ).ToArray();

        Move bestMove = default;
        int origAlpha = alpha;

        if (board.IsInCheck() && !qsearch) depth++;

        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= searchMaxTime)
                return 9999999;

            Move move = moves[i];

            if (canFPrune && !(i == 0 || move.IsCapture || move.IsPromotion))
                continue;

            // syntax hell from Tyrant7

            board.MakeMove(move);
            if(i==0 || qsearch)
                Search(beta);
            else if ((i <= 6 || depth < 2
                        ? eval = alpha + 1
                        : Search(alpha + 1, 3)) > alpha &&
                        alpha < Search(alpha + 1))
                Search(beta);
            board.UndoMove(move);

            if (eval > best)
            {
                best = eval;
                bestMove = move;
                if (ply == 0) bestmoveRoot = move;
                alpha = Math.Max(alpha, best);
                if (alpha >= beta)
                {
                    if (!move.IsCapture){
                        HistoryHeuristics[stm, move.StartSquare.Index, move.TargetSquare.Index] += depth * depth;
                        killer[ply] = move;
                    }
                    break;
                }
            }
        }

        if (!qsearch && moves.Length == 0) return board.IsInCheck() ? ply - 999999 : 0;
        tt[key & 0x3FFFFF] = new TTEntry(key, bestMove, depth, best, best >= beta ? 2 : best > origAlpha ? 3 : 1);

        return best;
    }
    public Move Think(Board _board, Timer _timer)
    {
#if INFO
        Console.WriteLine(""); // #DEBUG
        nodes = 0; // #DEBUG
#endif
        board = _board;
        timer = _timer;
        HistoryHeuristics = new int[2, 64, 64];
        killer = new Move[2048];
        bestmoveRoot = default;
        searchMaxTime = timer.MillisecondsRemaining / 30;
        for (int depth = 2, eval, alpha = -9999999, beta = 9999999; ;)
        {
            eval = PVS(alpha, beta, depth, 0);
            if (timer.MillisecondsElapsedThisTurn >= searchMaxTime)
                break;
            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            else
            {
#if INFO
                string evalWithMate = eval.ToString(); // #DEBUG
                if (Math.Abs(eval) > 999900 && Math.Abs(eval) <= 999999) // #DEBUG
                { // #DEBUG
                    evalWithMate = eval < 0 ? "-M" : "M"; // #DEBUG
                    evalWithMate += Math.Ceiling((double)(999999 - Math.Abs(eval)) / 2).ToString(); // #DEBUG
                } // #DEBUG
                int timeElapsed = timer.MillisecondsElapsedThisTurn; // #DEBUG
                Console.WriteLine("Info: depth: {0, 2} | eval: {1, 6} | nodes: {2, 9} | nps: {3, 8} | time: {4, 5}ms | best move: {5}{6}", // #DEBUG
                                  depth, // #DEBUG
                                  evalWithMate, // #DEBUG
                                  nodes, // #DEBUG
                                  1000 * nodes / (timeElapsed + 1), // #DEBUG
                                  timeElapsed, // #DEBUG
                                  bestmoveRoot.StartSquare.Name, // #DEBUG
                                  bestmoveRoot.TargetSquare.Name // #DEBUG
                ); // #DEBUG
#endif
                alpha = eval - 17; beta = eval + 17;
                depth++;
            }
        }
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }
    public RadiumBot()
    {
        UnpackedPestoTables = new[] {
            63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
            77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
            2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
            77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
            75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
            75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
            73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
            68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
        }.Select(packedTable =>
        new System.Numerics.BigInteger(packedTable).ToByteArray().Take(12)
                    .Select(square => (int)((sbyte)square * 1.461) + pvm[searchMaxTime++ % 12])
                .ToArray()
        ).ToArray();
    }
}