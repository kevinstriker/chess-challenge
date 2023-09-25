using ChessChallenge.API;

public class Clairvoyance : IChessBot
{
    Board board;
    int[] pieceValues = {0,100,300,300,500,900,90000};
    int universalDepth = 5;
    Move bestmove;
    public Move Think(Board _board, Timer _timer)
    {
        board = _board;
        NegaMax(universalDepth,-1000000,1000000);
        return bestmove;
    }
    int NegaMax(int depth, int alpha, int beta) {
        int score = 0;
        if (board.IsInCheckmate()) return -999999;
        if (board.IsDraw()) return -10;
        if (depth == 0) return Evaluate();
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            score = -NegaMax(depth - 1,-beta,-alpha);
            board.UndoMove(move);
            if (score >= beta) return beta;
            if (score > alpha) {
                alpha = score;
                if (depth == universalDepth) bestmove = move;
            }
        }
        return alpha;
    }

    int Evaluate() {
        int colorMult, score = 0;
        foreach (PieceList piecelist in board.GetAllPieceLists())
        {
            colorMult = (piecelist.IsWhitePieceList == board.IsWhiteToMove) ? 1 : -1;
            score += piecelist.Count * colorMult * pieceValues[(int)(piecelist.TypeOfPieceInList)];
        }
        return score;
    }
}