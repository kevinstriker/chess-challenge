using System;
using System.Linq;
using ChessChallenge.API;

public class NanoV1 : IChessBot
{
    private Move _bestRootMove;

    private static int GetPieceValue(PieceType pieceType) => new[] { 0, 100, 300, 310, 500, 900, 10000 }[(int)pieceType];
    
    public Move Think(Board board, Timer timer)
    {
        int searchDepth = 0;
        
        int Search(int depth, int alpha, int beta, int material)
        {
            // Evaluate this current position
            int eval = material + board.GetLegalMoves().Length;
            
            // Re-use alpha as our best found value when in q search
            if (depth <= 0)
                alpha = Math.Max(alpha, eval);
            
            // Reverse futility pruning: if our position is much better than beta, even if we start losing material every depth
            // we'd still be above beta, so cutoff since unlikely opponent will allow us this path
            else if (beta - alpha == 1 && depth <= 3 && beta <= eval - 100 * depth)
                return eval;
            
            // no beta cutoff check here, it will be done latter
            foreach (Move move in board.GetLegalMoves(depth <= 0)
                         .OrderByDescending(move => (move == _bestRootMove ? 1 : 0, move.CapturePieceType, 0 - move.MovePieceType)))
            {
                if (alpha >= beta)
                    break;

                board.MakeMove(move);

                int score =
                    board.IsDraw() ? 0 :
                    board.IsInCheckmate() ? 30_000 :
                    -Search(depth - 1, -beta, -alpha, -material - GetPieceValue(move.CapturePieceType) - GetPieceValue(move.PromotionPieceType));
                
                board.UndoMove(move);
                
                if (beta <= score)
                    return beta;
                
                if (score > alpha)
                {
                    alpha = score;
                    if (depth == searchDepth)
                        _bestRootMove = move;
                }

                // Check timer now: after updating best root move (so no illegal move), but before UndoMove (which takes some time)
                if (timer.MillisecondsElapsedThisTurn * 30 >= timer.MillisecondsRemaining)
                    depth /= 0;

            }

            return alpha;
        }

        try
        {
            for (;;)
                Search(++searchDepth, -40000, 40000, 0);
        }
        catch { }

        return _bestRootMove;
    }
}