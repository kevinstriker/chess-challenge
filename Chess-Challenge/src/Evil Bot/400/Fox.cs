using ChessChallenge.API;
using System;
using System.Linq;

public class Fox : IChessBot
{
  Move[] TT = new Move[8388608];
  Move bestRootMove;
  public Move Think(Board board, Timer timer)
  {
    var killers = new Move[128];

    int search(int depth, int alpha, int beta, int ply)
    {
      var (value, key, inQSearch, bestMove, score, pieces, evalValues) = (-20002, board.ZobristKey % 8388608, depth <= 0, Move.NullMove, 0, board.AllPiecesBitboard, new[] { 0ul, 943240312410411277ul, 4197714699149851955ul, 4848484849616963136ul, 6658122805863343458ul, 17289018720893200097ul, 508351539015584769ul, 2313471533096915729ul, 4777002364955480891ul, 5717702758025484112ul, 9909758167411563417ul, 17073413321325017080ul, 1447370843669012753ul, });
      if (board.IsInCheck()) depth++;
      while (pieces != 0)
      {
        int index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces);
        Piece piece = board.GetPiece(new Square(index));
        int piecetype = (int)piece.PieceType,
            c = piece.IsWhite ? 1 : -1;
        score -= ((byte)(evalValues[piecetype] >> (index ^= 28 + 28 * c) % 8 * 8) + (byte)(evalValues[piecetype + 6] >> (index & 0b111000))) * c;
      }
      if (board.IsWhiteToMove) score = -score;
      if (inQSearch) alpha = Math.Max(alpha, score);
      if (depth < 6 && score - 31.8 * Math.Max(depth, 0) >= beta) return score; //rfp and in qsearch stand pat to reduce tokens

      foreach (Move move in board.GetLegalMoves(inQSearch).OrderByDescending(move => (move == TT[key], move.IsCapture ? (int)move.CapturePieceType * 10 - (int)move.MovePieceType : move == killers[ply] ? 1 : 0)))
      {
        board.MakeMove(move);
        value = board.IsDraw() ? 0
            : board.IsInCheckmate() ? 20000 - ply
            : -search(depth - 1, -beta, -alpha, ply + 1);
        board.UndoMove(move);
        if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 13) return 42;
        if (value > alpha)
        {
          alpha = value;
          bestMove = move;
        }
        if (alpha >= beta)
        {
          if (!move.IsCapture) killers[ply] = move;
          break;
        }
      }
      if (ply == 0) bestRootMove = bestMove;
      TT[key] = bestMove;
      return alpha;
    }

    int i = 0;
    while (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 26) search(++i, -20001, 20001, 0);
    return bestRootMove;
  }
}