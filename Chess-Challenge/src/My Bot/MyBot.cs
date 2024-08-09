using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
  private Move bestRootMove;

  // Search
  private Move[] tt = new Move[0x800000];

  // Eval
  private ulong[] packed = {
    17216961133457930103ul, 8613882468645976456ul, 8603975868592265079ul, 8608480567748810887ul, 
    2699757695739835168ul, 4006402063654819955ul, 4001897364516870515ul, 158526625048655698ul, 
    7455559058274543205ul, 7455858125723699062ul, 7532420487621150838ul, 6225776120599902070ul, 
    8685341996790282103ul, 8608480567731124087ul, 8608480567731124087ul, 8608480567731124087ul, 
    7455559058274612837ul, 8608480567462688630ul, 7455559058829309815ul, 6226075187762657142ul, 
    3611889251339207203ul, 3611889251339207203ul, 7301836200102470453ul, 11207058936273794969ul,
  };

  public Move Think(Board board, Timer timer)
  {
    // DEBUG
    // int Nodes = 0;
    // int QNodes = 0;
    // double eval = 0;

    // ITERATIVE DEEPENING

    int searchDepth = 0;
    try
    {
      for (;;)
      {
        Search(++searchDepth, -1_000_000, 1_000_000);
        //DebugHelper.LogDepth(GetType().ToString(), timer, searchDepth, eval, Nodes, QNodes, bestRootMove);
      }
    } catch
    {
      //DebugHelper.LogDepth(GetType().ToString(), timer, searchDepth, eval, Nodes, QNodes, bestRootMove);
    }

    return bestRootMove;

    // FUNCTIONS 

    double Search(int depth, double alpha, double beta)
    {
      if (depth <= 0)
        alpha = Math.Max(alpha, Evaluate());
      
      // No beta cutoff here but done in the move loop so q search and search have same code

      ulong key = board.ZobristKey & 0x7FFFFF;

      foreach (Move move in board.GetLegalMoves(depth <= 0)
                   .OrderByDescending(move => (move == tt[key], move.CapturePieceType, move.PromotionPieceType - move.MovePieceType)))
      {
        // if (depth > 0) Nodes++;
        // else QNodes++;

        // Beta cutoff check
        if (alpha >= beta)
          return beta;

        board.MakeMove(move);

        // Check for draw. No need to check for mate, it will be taken care of by the log returning -inf in the mobility
        double score =
            board.IsDraw() ? 0 :
            board.IsInCheckmate() ? 100_000 - board.PlyCount :
            -Search(depth - 1, -beta, -alpha);

        // Improve best move when we found a path better than our current (alpha) path
        if (score > alpha)
        {
          alpha = score;
          tt[key] = move;

          // We're improving alpha and this is our root move (first move to do)
          if (depth == searchDepth)
            bestRootMove = move;
        }

        board.UndoMove(move);

        // Exit search when the time is out (raises an exception when the value is negative)
        Convert.ToUInt32(timer.MillisecondsRemaining - 30 * timer.MillisecondsElapsedThisTurn);
      }

      return alpha;
    }
    
    double Evaluate()
    {
      // Starting score is based on the valid number of moves the player to move has available
      double score = Math.Log(board.GetLegalMoves().Length);

      // Loop both sides in perspective of the player to move, so calculate other player and after flip score
      foreach (bool isWhite in new[] { !board.IsWhiteToMove, board.IsWhiteToMove })
      {
        score = -score;

        //       None (skipped)                  King
        for (var pieceIndex = 0; ++pieceIndex <= 6;)
        {
          // Instead of looping each square, we can skip empty squares by looking at a bitboard of each piece,
          var bitboard = board.GetPieceBitboard((PieceType)pieceIndex, isWhite);

          while (bitboard != 0)
          {
            // Material                       K 255    Q 225    R 125    B 80     N 75     P 25     N 0
            score += BitConverter.GetBytes(0b_11111111_11100001_01111101_01010000_01001011_00011001_00000000)[pieceIndex];

            // Clear the piece from the bitboard and get the square
            var sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard) ^ (isWhite ? 56 : 0);

            // PST (done with my own packer)
            score += ((int)(packed[sq / 16 + (pieceIndex - 1) * 4] >> sq % 16 * 4 & 15) - 7) * 2;
          }
        }
      }
      return score;
    }

  }
}