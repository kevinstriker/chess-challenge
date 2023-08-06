using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    private Move _bestMoveRoot = Move.NullMove;
    
    // https://www.chessprogramming.org/Transposition_Table
    private struct TtEntry
    {
        public ulong Key;
        public Move Move;
        public int Depth, Score, Bound;

        public TtEntry(ulong key, int depth, int score, int bound, Move move)
        {
            Key = key;
            Depth = depth;
            Score = score;
            Bound = bound;
            Move = move;
        }
    }
    private const int TtEntriesCount = 1 << 22;
    private TtEntry[] _tt = new TtEntry[TtEntriesCount];
    
    // Evaluation and unpacking credits to LiteBlueBot
    readonly int[] _phaseWeight = { 0, 1, 1, 2, 4, 0 };
    private readonly short[] _pvm = { 82, 337, 365, 477, 1025, 20000, 94, 281, 297, 512, 936, 20000};
    private readonly decimal[] _packedPsqt = {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
        2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
        77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
    };
    private readonly int[][] _psqt;
    
    public MyBot()
    {
        _psqt = new int[64][];
        _psqt = _packedPsqt.Select(packedPsqt =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedPsqt).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + _pvm[pieceType++]))
                .ToArray();
        }).ToArray();
    }

    private int Evaluate(Board board)
    {
        // Define evaluation variables
        int mg = 0, eg = 0, phase = 0;
        // Iterate through both players
        foreach (bool stm in new[] { true, false })
        {
            // Iterate through all piece types
            for (int piece = -1; ++piece < 6;)
            {
                // Get piece bitboard
                ulong bb = board.GetPieceBitboard((PieceType)piece + 1, stm);
                // Iterate through each individual piece
                while (bb != 0)
                {
                    // Get square index for pst based on color
                    int sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (stm ? 56 : 0);
                    // Increment mg and eg score
                    mg += _psqt[sq][piece];
                    eg += _psqt[sq][piece + 6];
                    // Updating position phase
                    phase += _phaseWeight[piece];
                }
            }
            // Flip sign of eval before switching sides
            mg = -mg;
            eg = -eg;
        }
        // Tapered evaluation
        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    // https://www.chessprogramming.org/Negamax
    // https://www.chessprogramming.org/Quiescence_Search
    private int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply)
    {
        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0;
        bool notRoot = ply > 0;
        
        int bestScore = -30000;

        // Check for repetition (this is much more important than material and 50 move rule draws)
        if (notRoot && board.IsRepeatedPosition())
            return 0;

        TtEntry entry = _tt[key % TtEntriesCount];

        // TT cutoffs
        if (notRoot && entry.Key == key && entry.Depth >= depth && (
                entry.Bound == 3
                || entry.Bound == 2 && entry.Score >= beta
                || entry.Bound == 1 && entry.Score <= alpha
            )) return entry.Score;

        int eval = Evaluate(board);

        // Quiescence search is in the same function as negamax to save tokens
        if (qsearch)
        {
            bestScore = eval;
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
        }

        // Generate moves, only captures in qsearch
        Move[] moves = board.GetLegalMoves(qsearch);
        int[] scores = new int[moves.Length];

        // Score moves
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            // TT move
            if (move == entry.Move) scores[i] = 1000000;
            // MVV-LVA
            else if (move.IsCapture) scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        Move bestMove = Move.NullMove;
        int origAlpha = alpha;

        // Search moves
        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 30000;

            // Incrementally sort moves
            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            // New best move
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (ply == 0) _bestMoveRoot = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if (alpha >= beta) break;
            }
        }

        // (Check/Stale)mate
        if (!qsearch && moves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;

        // Did we fail high/low or get an exact score?
        int bound = bestScore >= beta ? 2 : bestScore > origAlpha ? 3 : 1;

        // Push to TT
        _tt[key % TtEntriesCount] = new TtEntry(key, depth, bestScore, bound, bestMove);

        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        _bestMoveRoot = Move.NullMove;
        
        // https://www.chessprogramming.org/Iterative_Deepening
        for (int depth = 1; depth <= 50; depth++)
        {
            int score = Search(board, timer, -30000, 30000, depth, 0);

            // Out of time
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }

        return _bestMoveRoot.IsNull ? board.GetLegalMoves()[0] : _bestMoveRoot;
    }
}