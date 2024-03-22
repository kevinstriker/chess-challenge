using ChessChallenge.API;
using System;
using System.Linq;

public class ErwanF2 : IChessBot
{
  Move bestRootMove;

  Move[] TT = new Move[0x800000];

  public Move Think(Board board, Timer timer)
  {
    int searchDepth = 0;

    int Search(int depth, int alpha, int beta, int material)
    {

      // Quiescence & eval
      if (depth <= 0)
        alpha = Math.Max(alpha, material * 200 + board.GetLegalMoves().Length);  //eval = material + mobility
      // no beta cutoff check here, it will be done latter

      ulong key = board.ZobristKey & 0x7FFFFF;

      foreach (Move move in board.GetLegalMoves(depth <= 0)
                   .OrderByDescending(move => (move == TT[key], move.CapturePieceType, 0 - move.MovePieceType)))  // TODO: replace 0 by move.PromotionPieceType ?? (+2 tokens, worth the elo?)
      {
        if (alpha >= beta)
          break;

        board.MakeMove(move);

        int score =
            board.IsDraw() ? 0 :
            board.IsInCheckmate() ? 30000 :
            -Search(depth - 1, -beta, -alpha, -material - move.CapturePieceType - move.PromotionPieceType);

        if (score > alpha)
        {
          alpha = score;
          TT[key] = move;
          if (depth == searchDepth)
            bestRootMove = move;
        }

        Convert.ToUInt32(timer.MillisecondsRemaining - 30 * timer.MillisecondsElapsedThisTurn);  // raises an exception when the value is negative

        board.UndoMove(move);
      }

      return alpha;
    }

    try
    {
      for (; ; )
        Search(++searchDepth, -30000, 30000, 0);
    }
    catch { }

    return bestRootMove;
  }
}