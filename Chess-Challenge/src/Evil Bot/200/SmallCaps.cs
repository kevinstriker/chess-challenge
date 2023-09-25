using System.Linq;
using ChessChallenge.API;

// Sᴍᴀʟʟ Cᴀᴘꜱ, a 200 token bot

public class SmallCaps : IChessBot
{
    Move rootMove;

    public Move Think(Board board, Timer _)
    {

        negamax(5, 32_000, -32_000);
        return rootMove;

        long negamax(int remainingDepth, long alpha, long beta)
        {
            if (board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return 30_000;

            long score = -board.GetLegalMoves().Count();
            foreach (var pieceList in board.GetAllPieceLists())
                score += pieceList.Count * (1013860005146900480 >> 10 * (int)pieceList.TypeOfPieceInList & 0x3ff)
                                         * (pieceList.IsWhitePieceList == board.IsWhiteToMove ? -1 : 1);
            
            if (remainingDepth <= 0)
            {
                if (score < alpha) alpha = score;
                if (score <= beta) return beta;
            }

            foreach (Move move in board.GetLegalMoves(remainingDepth <= 0).OrderByDescending(move => move.CapturePieceType))
            {
                board.MakeMove(move);
                score = -negamax(remainingDepth - 1, -beta, -alpha);
                board.UndoMove(move);
                if (score < alpha)
                {
                    if (remainingDepth == 5)
                        rootMove = move;
                    alpha = score;
                    if (score <= beta) break;
                }
            }

            return alpha;
        }
    }
}