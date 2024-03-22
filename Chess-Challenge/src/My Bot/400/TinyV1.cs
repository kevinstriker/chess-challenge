using ChessChallenge.API;
using System;
using System.Linq;

/**
 * ErwanF,             // Place 1
 * ErwanF2,            // Place 1 +100 elo
 * SmallCaps,          // Place 2
 * MrX,                // Place 3
 * Clairvoyance,       // Place 7
 * DjNano,             // Place 10
 */
public class TinyV1 : IChessBot
{
  public Move Think(Board board, Timer timer)
  {
    Move[] allMoves = board.GetLegalMoves();
    return allMoves[0];
  }

}
