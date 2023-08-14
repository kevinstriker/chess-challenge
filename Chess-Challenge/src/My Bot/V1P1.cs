using System;
using System.Linq;
using ChessChallenge.API;

/*
 * So here we go, 8 august, let's start from scratch, let's name this project NebulaAI
 * First implementation of: NegaMax, Q Search, Move ordering, Piece Square Tables and Transposition Tables
 */
public class V1P1 : IChessBot
{
    public int Nodes;
    public int QNodes;

    // Transposition table
    public struct TtEntry
    {
        public readonly ulong Key;
        public readonly int Score, Depth, Flag;
        public Move Move;

        public TtEntry(ulong key, int score, int depth, int flag, Move move)
        {
            Key = key;
            Score = score;
            Depth = depth;
            Flag = flag;
            Move = move;
        }
    }

    public const int TtEntryCount = 1 << 22;
    public TtEntry[] Tt = new TtEntry[TtEntryCount];
    
    // Time management
    public Timer Timer;

    // Evaluate
    //                                P    K    B    R    Q     K
    private int[] _pieceValues = { 0, 100, 310, 330, 500, 1000, 10000 };
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

    public Move BestMove;

    // A getter method thanks to example bot JW 
    private int GetPstVal(int psq)
    {
        return (int)(((_psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    // Tapered evaluation that takes mid-game and end-game priorities into account 
    private int Evaluate(Board board)
    {
        int mg = 0, eg = 0, phase = 0;

        // Loop both players, first white (true) and after black
        foreach (bool color in new[] { true, false })
        {
            for (PieceType pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
            {
                int p = (int)pieceType;
                ulong hash = board.GetPieceBitboard(pieceType, color);
                while (hash != 0)
                {
                    phase += _piecePhase[p];
                    int ind = 128 * (p - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref hash) ^ (color ? 56 : 0);
                    mg += _pieceValues[p] + GetPstVal(ind);
                    eg += _pieceValues[p] + GetPstVal(ind + 64);
                }
            }

            // Flip score for optimised token count (always white perspective due to double flip)
            // Eg. White eval = 2300 -> flip -> -2300 -> black eval = 2000 -> -300 -> flip -> 300 
            mg = -mg;
            eg = -eg;
        }

        // Since we evaluate after making the move, the isWhiteToMove is the other player, so ? 1 : -1
        // Use tapered eval to smooth into endgame strategy when required
        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    // Quiescence search which means we're searching for a "quiet" position before return the evaluation
    private int QSearch(Board board, int alpha, int beta)
    {
        // Debug
        QNodes++;

        // We're not forced to capture so also evaluate the current position which might be a great situation
        int standPat = Evaluate(board);
            
        // Beta cutoff when there is an established better option for the other player
        // We are not making a move now (no capture) but we're not forced to capture, hence "stand pat"
        if (beta <= standPat)
            return beta;

        // Improve alpha
        alpha = Math.Max(alpha, standPat);
        
        // Only consider captures since we want to find a "quiescence" position asap
        Move[] moves = board.GetLegalMoves(true);

        // Loop the captures and check them
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -QSearch(board, -beta, -alpha);
            board.UndoMove(move);
            
            // Beta cutoff when there is an established better option for the other player
            if (beta <= score)
                return beta;
            
            // Improve alpha if this move is a better move (eg. capture queen with pawn etc)
            alpha = Math.Max(alpha, score);
        }

        return alpha;
    }

    public int NegaMax(Board board, int depth, int ply, int alpha, int beta)
    {
        // Debug keep track on nodes and qnodes searching
        Nodes++;

        // Keep track of the original alpha in order to determine the type of bounds cutoff
        int alphaStart = alpha;
        bool root = ply == 0;

        // Transposition table does not know about three fold draw so check this clause first
        if (board.IsDraw()) return 0;

        // Try to find the board position in the tt
        ulong key = board.ZobristKey;
        TtEntry ttEntry = Tt[key % TtEntryCount];

        // When we find the transposition check if we can use it to narrow our alpha beta bounds
        if (!root && ttEntry.Key == key && ttEntry.Depth >= depth)
        {
            // 1 = lower bound; 2 = exact; 3 = upper bound
            if (ttEntry.Flag == 2) return ttEntry.Score;
            if (ttEntry.Flag == 1) alpha = Math.Max(alpha, ttEntry.Score);
            if (ttEntry.Flag == 3) beta = Math.Min(beta, ttEntry.Score);

            // Beta cutoff when there is an established better branch that resulted in the alpha score
            if (beta <= alpha) return ttEntry.Score;
        }

        // Go into Quiescence search at depth 0
        if (depth == 0) 
            return QSearch(board, alpha, beta);
        
        int bestScore = -100000;

        Move[] moves = board.GetLegalMoves();
        Move bestMove = Move.NullMove;

        // Score moves so alpha beta pruning will be more effective (more to prune if we check likely good moves first)
        int[] movesScore = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            movesScore[i] =
                // tt moves were stored for a reason so let's evaluate them first
                move == ttEntry.Move ? 100000 :
                // Capturing moves are often a good idea if you capture a piece more valuable than yours
                move.IsCapture ? 1000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                // Pawn promotions are often best (who wouldn't like a new queen)
                move.IsPromotion ? 100 : 0;
        }

        // Sort based on ascending so flip with reverse in the loop to sort them based on descending
        Array.Sort(movesScore, moves);

        foreach (Move move in moves.Reverse())
        {
            if (Timer.MillisecondsElapsedThisTurn > Timer.MillisecondsRemaining / 40) return 100000;

            board.MakeMove(move);
            int score = -NegaMax(board, depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);

            if (score > bestScore)
            {
                if (ply == 0) BestMove = move;
                bestMove = move;
                bestScore = score;
                alpha = Math.Max(alpha, bestScore);

                // Beta cutoff when there is an established better branch that resulted in the alpha score
                if (beta <= alpha) break;
            }
        }

        // Decide the current search bounds so we're able to properly check if we're allowed to cutoff later
        int flag = bestScore <= alphaStart ? 3 : bestScore >= beta ? 1 : 2;

        // Store the position and it's eval to the transposition table for fast lookup when same position is found twice
        Tt[key % TtEntryCount] = new TtEntry(key, bestScore, depth, flag, bestMove);

        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        Nodes = 0;
        QNodes = 0;

        // Assign to be globally used
        Timer = timer;

        // Reset to prevent lingering previous moves
        BestMove = Move.NullMove;

        // Iterative deepening
        for (int depth = 1; depth < 50; depth++)
        {
            int score = NegaMax(board, depth, 0, -100000, 100000);

            DebugHelper.LogDepth(timer, depth, score, this);

            if (Timer.MillisecondsElapsedThisTurn > Timer.MillisecondsRemaining / 40) break;
        }
        
        Console.WriteLine();
        
        return BestMove.IsNull ? board.GetLegalMoves()[0] : BestMove;
    }
}