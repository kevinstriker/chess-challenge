#define LOG

using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
#if LOG
    private Int64 _nodes;
    private Int64 _qnodes;
#endif

    // Evaluate variables: Useful when determining which move to play
    private Move _bestMove;

    // PeSTO evaluation thanks to JW
    private int[] _pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
    private int[] _piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    
    private ulong[] _psts =
    {
        657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569,
        366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421,
        366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430,
        402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514,
        329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759,
        291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181,
        402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804,
        347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047,
        347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538,
        384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492,
        347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100,
        366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863,
        419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932,
        329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691,
        383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375,
        329978099633296596, 67159620133902
    };

    // Transposition Table; A lookup table of previous calculated positions and it's "best move"
    public record struct TtEntry(ulong Key, int Depth, int Flag, int Score, Move Move);

    public const ulong TtEntryCount = 0x3FFFFF;
    private TtEntry[] _tt = new TtEntry[TtEntryCount];

    private int GetPstVal(int psq)
    {
        return (int)(((_psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    private int Evaluate(Board board)
    {
        int mg = 0, eg = 0, phase = 0;

        foreach (bool stm in new[] { true, false })
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, stm);
                while (mask != 0)
                {
                    phase += _piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                    mg += GetPstVal(ind) + _pieceVal[piece];
                    eg += GetPstVal(ind + 64) + _pieceVal[piece];
                }
            }

            // Flip score; example white 2300 eval becomes -2300, after we calculate black 2000 it becomes -300 and 300 after last flip
            mg = -mg;
            eg = -eg;
        }

        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }


    // Search our best next move by NegaMax, Q Search, TT and move ordering
    private int Search(Board board, Timer timer, int timeLimit, int alpha, int beta, int depth, int ply)
    {
        ulong key = board.ZobristKey;
        bool notRoot = ply > 0;
        bool qSearch = depth <= 0;
        int bestScore = -30000;

        // Try to prevent a 3 fold repetition by slightly offsetting this position score
        if (notRoot && board.IsRepeatedPosition()) return -10;

        TtEntry entry = _tt[key % TtEntryCount];

        // Try to find a board position that was already evaluated and re-use it
        if (notRoot && entry.Key == key && entry.Depth >= depth
            && (entry.Flag == 3
                || entry.Flag == 2 && entry.Score >= beta
                || entry.Flag == 1 && entry.Score <= alpha
            )) return entry.Score;

        int eval = Evaluate(board);

        // Quiescence search here to save tokens
        if (qSearch)
        {
            bestScore = eval;
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
        }

        // Generate moves, only captures when in q search
        Move[] moves = board.GetLegalMoves(qSearch);
        int[] scores = new int[moves.Length];

        // Sorting moves makes alpha beta pruning more effective, good moves are calculated first
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            // TT move
            if (move == entry.Move) scores[i] = 1000000;
            // https://www.chessprogramming.org/MVV-LVA
            else if (move.IsCapture) scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        Move bestMove = Move.NullMove;
        int startAlpha = alpha;

        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timeLimit) return 0;

            // Incrementally sort moves
            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

#if LOG
            if (ply < 0) _qnodes++;
            else _nodes++;
#endif

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Search(board, timer, timeLimit, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            // New best move
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;

                // When in root depth we're using this move as our next move
                if (ply == 0) _bestMove = move;
                // Improve alpha
                alpha = Math.Max(alpha, score);
                // Beta fail soft
                if (alpha >= beta) break;
            }
        }

        // (Check/Stale)mate
        if (!qSearch && moves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;

        // Determine type of node cutoff
        int flag = bestScore >= beta ? 2 : bestScore > startAlpha ? 3 : 1;

        // Save position and best move to transposition table
        _tt[key % TtEntryCount] = new TtEntry(key, depth, flag, bestScore, bestMove);

        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        _bestMove = Move.NullMove;

        int timeLimit = timer.MillisecondsRemaining / 30;

        // Iterative deepening
        for (int depth = 1; depth <= 50; depth++)
        {
            // Negamax algorithm with Alpha Beta, Q Search and Transposition Tables
            int score = Search(board, timer, timeLimit, -30000, 30000, depth, 0);

#if LOG
            DebugHelper.LogDepth(board, timer, _tt, depth, score, _nodes, _qnodes, 1000 * _nodes / (timer.MillisecondsElapsedThisTurn + 1));
#endif
            
            // Out of time
            if (timer.MillisecondsElapsedThisTurn >= timeLimit) break;
        }

#if LOG
        Console.WriteLine();
#endif
        
        return _bestMove.IsNull ? board.GetLegalMoves()[0] : _bestMove;
    }
}