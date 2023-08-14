using System;
using System.Linq;
using System.Numerics;
using ChessChallenge.API;

public class MagnesiumBot : IChessBot
{
    // Renamed / custom for analyse reasons
    public Move BestMove = Move.NullMove;
    public int Nodes;
    public int QNodes;
    public Timer Timer;

    struct TTEntry
    {
        public ulong key;
        public Move move;
        public int depth, score, bound;

        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
        {
            key = _key;
            move = _move;
            depth = _depth;
            score = _score;
            bound = _bound;
        }
    }

    const int entries = (1 << 20);
    TTEntry[] tt = new TTEntry[entries];
    int[] phase_weight = { 0, 1, 1, 2, 4, 0 };

    short[] pvm =
    {
        82, 337, 365, 477, 1025, 0,
        94, 281, 297, 512, 936, 0
    };

    decimal[] PackedPestoTables =
    {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m,
        75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m,
        936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m,
        3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m,
        4977175895537975520060507415m, 2475894077091727551177487608m,
        2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m,
        3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m,
        9301461106541282841985626641m, 2793818196182115168911564530m,
        77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m,
        5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m,
        5619082524459738931006868492m, 649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m,
        4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m,
        1890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m,
        4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m,
        1557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m,
        3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m,
        78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m,
        77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m,
        74568981255592060493492515584m, 70529879645288096380279255040m,
    };

    int[][] UnpackedPestoTables;

    int Evaluate(Board board)
    {
        int mg = 0, eg = 0, phase = 0;
        foreach (bool stm in new[] { true, false })
        {
            for (int piece = -1; ++piece < 6;)
            {
                ulong bb = board.GetPieceBitboard((PieceType)(piece + 1), stm);
                if (piece == -1)
                    for (int i = 0; i < 8; i++)
                    {
                        ulong mask = 0x0101010101010101ul << i;
                        if (BitOperations.PopCount(mask & bb) > 1)
                        {
                            mg -= 9;
                            eg -= 9;
                        }
                    }

                while (bb != 0)
                {
                    int sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (stm ? 56 : 0);
                    mg += UnpackedPestoTables[sq][piece];
                    eg += UnpackedPestoTables[sq][piece + 6];
                    phase += phase_weight[piece];
                }
            }

            mg += BitOperations.PopCount(board.GetPieceBitboard(PieceType.Pawn, stm) & 0x1818000000) * 10;
            mg = -mg;
            eg = -eg;
        }

        phase = Math.Min(phase, 24);
        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    public int Search(Board board, int depth, int ply, int alpha, int beta)
    {
        // Debug keep track on nodes and qnodes searching
        if (depth > 0) Nodes++;
        else QNodes++;

        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0;
        bool notRoot = ply > 0;
        int best = -32001;

        if (notRoot && board.IsRepeatedPosition())
            return 0;

        TTEntry entry = tt[key % entries];

        if (notRoot && entry.key == key && entry.depth >= depth && (
                entry.bound == 3
                || entry.bound == 2 && entry.score >= beta
                || entry.bound == 1 && entry.score <= alpha
            )) return entry.score;

        int eval = Evaluate(board);

        if (qsearch)
        {
            best = eval;
            if (best >= beta) return best;
            alpha = Math.Max(alpha, best);
        }

        Move[] moves = board.GetLegalMoves(qsearch);
        int[] scores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            if (move == entry.move) scores[i] = 1000000;
            else if (move.IsCapture) scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        Move bestMove = Move.NullMove;
        int origAlpha = alpha;

        for (int i = 0; i < moves.Length; i++)
        {
            if (Timer.MillisecondsElapsedThisTurn >= Timer.MillisecondsRemaining / 30)
                return 32001;

            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Search(board, depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);

            if (score > best)
            {
                best = score;
                bestMove = move;
                if (ply == 0) BestMove = move;
                alpha = Math.Max(alpha, score);
                if (alpha >= beta) break;
            }
        }

        if (!qsearch && moves.Length == 0) return board.IsInCheck() ? -32000 + ply : 0;
        int bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;
        tt[key % entries] = new TTEntry(key, bestMove, depth, best, bound);

        return best;
    }

    public Move Think(Board board, Timer timer)
    {
        Nodes = 0;
        QNodes = 0;
        Timer = timer;
        
        Random rng = new();
        Move[] leg_moves = board.GetLegalMoves();
        BestMove = Move.NullMove;
        
        for (int depth = 1; depth <= 50; depth++)
        {
            int score = Search(board, depth,0, -32001, 32001);
            
            DebugHelper.LogDepth(timer, depth, score, this);

            if (Timer.MillisecondsElapsedThisTurn >= Timer.MillisecondsRemaining / 30)
                break;
            
        }
        
        Console.WriteLine();

        return BestMove == Move.NullMove ? leg_moves[rng.Next(leg_moves.Length)] : BestMove;
    }
    

    public MagnesiumBot()
    {
        UnpackedPestoTables = new int[64][];
        UnpackedPestoTables = PackedPestoTables.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + pvm[pieceType++]))
                .ToArray();
        }).ToArray();
    }
}