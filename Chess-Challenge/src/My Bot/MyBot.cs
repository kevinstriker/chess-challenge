using System;
using System.Linq;
using ChessChallenge.API;

/*
 * So here we go, 8 august, let's start from scratch, let's name this project CosmicChessAI
 * The idea is to turtle, run out the timer of the opponent and after strike during the mid-game
 */
public class MyBot : IChessBot
{
    // Debug
    public int Nodes;
    public bool InTest = false;

    // Transposition table
    struct TtEntry
    {
        public ulong Key;
        public int Score, Depth, Flag;
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
    const int TtEntryCount = 1 << 22;
    TtEntry[] _tt = new TtEntry[TtEntryCount];

    // Time management
    private Timer _timer;

    // Evaluate
    //                                P    K    B    R    Q    K
    private int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 0 };
    private const int Checkmate = 30000;

    // functions that attempt to simulate a piece square table
    static int[] _edgeDist = { 0, 1, 2, 3, 3, 2, 1, 0 };
    private static Func<Square, int>[] _psqt =
    {
        sq => 0,
        sq => sq.Rank * 10 - 10 + (sq.Rank == 1 && _edgeDist[sq.File] != 3 ? 40 : 0) +
              (_edgeDist[sq.Rank] == 3 && _edgeDist[sq.File] == 3 ? 10 : 0),
        sq => (_edgeDist[sq.Rank] + _edgeDist[sq.File]) * 10,
        sq => _psqt[2](sq),
        sq => sq.Rank == 6 ? 10 : 0 + ((sq.Rank == 0 && _edgeDist[sq.File] == 3) ? 10 : 0),
        sq => (_edgeDist[sq.Rank] + _edgeDist[sq.File]) * 5,
        sq => (3 - _edgeDist[sq.Rank] + 3 - _edgeDist[sq.File]) * 10 - 5 - (sq.Rank > 1 ? 50 : 0)
    };

    public Move BestMove;

    private int Evaluate(Board board)
    {
        int score = 0;

        // Loop both players, first white (true) and after black
        foreach (bool color in new[] { true, false })
        {
            for (PieceType pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
            {
                int p = (int)pieceType;
                ulong hash = board.GetPieceBitboard(pieceType, color);
                while (hash != 0)
                {
                    Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref hash));
                    score += _pieceValues[p] + _psqt[p](square);
                }
            }

            // Flip score for optimised token count (always white perspective due to double flip)
            // Eg. White eval = 2300 -> flip -> -2300 -> black eval = 2000 -> -300 -> flip -> 300 
            score = -score;
        }

        // Since we evaluate after making the move, the isWhiteToMove is the other player, so ? 1 : -1
        return score * (board.IsWhiteToMove ? 1 : -1);
    }

    public int NegaMax(Board board, int depth, int ply, int alpha, int beta)
    {
        // Debug: keep track of the number of nodes searched
        Nodes++;

        // Keep track of the original alpha in order to determine the type of bounds cutoff
        int alphaStart = alpha;

        // Transposition table does not know about three fold draw so check this clause first
        if (board.IsDraw()) return 0;

        // Try to find the board position in the tt
        ulong key = board.ZobristKey;
        TtEntry ttEntry = _tt[key % TtEntryCount];

        // Ensure we found the position in the tt
        if (ttEntry.Key == key && ttEntry.Depth >= depth)
        {
            // 1 = lower bound; 2 = exact; 3 = upper bound
            if (ttEntry.Flag == 2) return ttEntry.Score;
            if (ttEntry.Flag == 1) alpha = Math.Max(alpha, ttEntry.Score);
            if (ttEntry.Flag == 3) beta = Math.Min(beta, ttEntry.Score);

            // Beta cutoff when there is an established better branch that resulted in the alpha score
            if (beta <= alpha) return ttEntry.Score;
        }

        bool root = ply == 0;
        int bestScore = -30000;

        // Leaf node conditions
        if (board.IsInCheckmate()) return bestScore;
        if (depth == 0) return Evaluate(board);

        Move[] moves = board.GetLegalMoves();
        Move ttMove = Move.NullMove;

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
            if (!InTest && _timer.MillisecondsElapsedThisTurn > _timer.MillisecondsRemaining / 40) return 30000;

            board.MakeMove(move);
            int score = -NegaMax(board, depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);

            if (score > bestScore)
            {
                if (root) BestMove = move;
                ttMove = move;

                bestScore = score;
                alpha = Math.Max(alpha, bestScore);

                // Beta cutoff when there is an established better branch that resulted in the alpha score
                if (beta <= alpha) break;
            }
        }

        // Decide the current search bounds so we're able to properly check if we're allowed to cutoff later
        int flag = bestScore <= alphaStart ? 3 : bestScore >= beta ? 1 : 2;

        // Store the position and it's eval to the transposition table for fast lookup when same position is found twice
        _tt[key % TtEntryCount] = new TtEntry(key, bestScore, depth, flag, ttMove);

        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        Nodes = 0;

        // Timer used in NegaMax
        _timer = timer;

        // Reset the depth move to prevent lingering moves 
        BestMove = Move.NullMove;

        // Iterative deepening
        for (int depth = 1; depth < 50; depth++)
        {
            int score = NegaMax(board, depth, 0, -Checkmate, Checkmate);

            DebugHelper.LogDepth(board, timer, depth, score, Nodes, 0);

            if (!InTest && (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 40
                            || score > 15000)) break;
        }

        Console.WriteLine();

        return BestMove.IsNull ? board.GetLegalMoves()[0] : BestMove;
    }
}