using ChessChallenge.API;
using System;
using System.Linq;

public class DjNano : IChessBot
{
    Move moveToPlay;
    double bestMoveEvaluationSoFar;

    // CHALLANGE CALLED FUNCTION 
    public Move Think(Board board, Timer timer)
    {
        bestMoveEvaluationSoFar = -1000000000;
        // magical constant
        // even good for white, odd good for black
        AlphaBeta(board, board.IsWhiteToMove ? 4 : 3, -1000000000, 1000000000, true);
        return moveToPlay;
    }

    double AlphaBeta(Board board, int depth, double a, double b, bool isRoot = false)
    {
        if (board.IsInCheckmate())
            return -1000000000;

        var moves = board.GetLegalMoves();
        if (depth <= 0) return
            moves.Length +
            Enumerable.Range(0, 12).Sum(i => (i < 6 ? 100 : -100) * Math.Pow(2,i%6)*board.GetAllPieceLists()[i].Count);

        foreach (Move move in moves.OrderBy(move => move.IsCapture))
        {
            board.MakeMove(move);
            double eval = -AlphaBeta(board, depth - 1, -b, -a);
            board.UndoMove(move);

            if (isRoot && eval >= bestMoveEvaluationSoFar)
            {
                bestMoveEvaluationSoFar = eval;
                moveToPlay = move;
            }
            else
            {
                if (eval >= b) return b;
                a = Math.Max(a, eval);
            }
        }
        return a;

    }

}