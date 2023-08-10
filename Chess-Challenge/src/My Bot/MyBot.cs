using System;
using ChessChallenge.API;

/*
 * So here we go, 8 august, let's start from scratch, let's name this project CosmikBot
 * The idea is to turtle, run out the timer of the opponent and after strike during the mid-game
 */
public class MyBot : IChessBot
{
    // Debug
    public int Nodes;
    public bool InTest = false;

    // Evaluate
    //                                P    K    B    R    Q    K
    private int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private const int Checkmate = 30000;
    private Timer _timer;
    public Move DepthMove;

    private int Evaluate(Board board)
    {
        int score = 0;

        // Loop both players, first white (true) and after black
        foreach (bool color in new[] { true, false })
        {
            for (PieceType pieceType = PieceType.Pawn; pieceType < PieceType.King; pieceType++)
            {
                int p = (int)pieceType;
                ulong hash = board.GetPieceBitboard(pieceType, color);
                while (hash != 0)
                {
                    BitboardHelper.ClearAndGetIndexOfLSB(ref hash);
                    score += _pieceValues[p];
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
        Nodes++;

        int score = -30000;
        bool root = ply == 0;

        if (board.IsInCheckmate()) return score;
        if (board.IsDraw()) return 0;
        if (depth == 0) return Evaluate(board);
        
        // Loop all moves and play them out (when in q search only captures)
        Move[] moves = board.GetLegalMoves();
        foreach (Move move in moves)
        {
            if (!InTest && _timer.MillisecondsElapsedThisTurn > _timer.MillisecondsRemaining / 30) return -30000;
            
            board.MakeMove(move);
            int newScore = -NegaMax(board, depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);
            
            if (newScore > score)
            {
                if (root) DepthMove = move;
                
                score = newScore;
                alpha = Math.Max(alpha, score);

                if (alpha >= beta) break;
            }
        }

        return score;
    }

    public Move Think(Board board, Timer timer)
    {
        _timer = timer;
        Nodes = 0;
        Move bestMove = Move.NullMove;
        
        // Iterative deepening
        for (int depth = 1; depth < 50; depth++)
        {
            int score = NegaMax(board, depth, 0, -Checkmate, Checkmate);

            if (!InTest && _timer.MillisecondsElapsedThisTurn > _timer.MillisecondsRemaining / 30) break;
            
            bestMove = DepthMove;

            DebugHelper.LogDepth(board, timer, depth, score, Nodes, 0);
        }
        Console.WriteLine();
        
        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }
}