using ChessChallenge.API;
using System;
using System.Linq;

public class TinyV1 : IChessBot
{
  //private int qnodes, nodes;
  private Move bestMoveRoot;
  
  public Move Think(Board board, Timer timer)
  {
    int searchDepth = 0;

    try
    {
      for (;;)
      {
        Search(++searchDepth, -1_000_000, 1_000_000, 0);
        //DebugHelper.LogDepth(GetType().ToString(), timer, searchDepth, score, nodes, qnodes, bestMoveRoot);
      }

    } catch
    { }

    return bestMoveRoot;

    double Search(int depth, double alpha, double beta, double curEval)
    {
      // Stand pat update when in Q Search
      if (depth <= 0 && alpha < curEval)
        alpha = curEval;
      
      // No cutoff fail-hard here in search to combine search and Q Search

      // Search moves (either normal search or q search)
      // Ordering by best previous move makes iterative deepening much better since we'll always start with best move of last depth
      // So when exception is thrown early in search depth, it's still a "good move" since it was best move of last depth
      foreach (Move move in board.GetLegalMoves(depth <= 0)
                   .OrderByDescending(m => (m == bestMoveRoot, m.CapturePieceType, m.MovePieceType)))
      {
        // if (depth <= 0)
        //  qnodes++;
        //else nodes++;

        // Fail hard beta cutoff here since -Search already contains current eval (before move)
        if (alpha >= beta)
          return beta;

        board.MakeMove(move);
        
        // Calculate eval for the other player since "we" just made our move ^
        // Does not need checkmate, Math.log makes it inf negative when no moves found (0 length)
        double score =
            board.IsDraw()
                ? 0
                : -Search(depth - 1, -beta, -alpha,
                          // Mobility
                          Math.Log(board.GetLegalMoves().Length)
                          // Material                K 255    Q 225    R 125    B 80     K 75     P 25     N 0
                          - BitConverter.GetBytes(0b_11111111_11100001_01111101_01010000_01001011_00011001_00000000)[(int)move.CapturePieceType | (int)move.PromotionPieceType]
                          // Take the already calculated eval into account
                          - curEval);

        if (score > alpha)
        {
          alpha = score;
          if (depth == searchDepth)
            bestMoveRoot = move;
        }

        Convert.ToUInt32(timer.MillisecondsRemaining - 30 * timer.MillisecondsElapsedThisTurn);

        board.UndoMove(move);
      }

      return alpha;
    }


  }
}