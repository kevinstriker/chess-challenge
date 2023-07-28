using System;
using System.Linq;
using ChessChallenge.API;

public class PvLine {
    public Move[] line = new Move[128];
    public int length = 0;
}

public class MyBot : IChessBot
{
    // Debug
    Timer timer;
    Int64 nodes;
    PvLine[] PvTable = new PvLine[128];

    // Generic
    Board board;

    // Eval
    int CHECKMATE = 100000;
    int[] PieceValue = { 0, 100, 300, 300, 500, 900, 0 };
    Move depthMove;
    
    // Timer related
    int timeLimit;
    DateTime start;

    public Move Think(Board boardInput, Timer timerInput)
    {
        board = boardInput;
        timer = timerInput;
        
        for (int i = 0; i < 128; i++) PvTable[i] = new PvLine();
        return IterativeDeepening();
    }

    private Move IterativeDeepening()
    {
        timeLimit = 1000;
        start = DateTime.Now;
        Move bestMove = Move.NullMove;

        for (int depth = 1; depth < 100; depth++)
        {
            nodes = 0;
            
            int score = Negamax(depth, 0, -CHECKMATE, CHECKMATE);

            if ((DateTime.Now - start).TotalMilliseconds > timeLimit)
                break;

            bestMove = depthMove;
            
            LogDepth(depth, score);
        }
        
        Console.WriteLine();
        return bestMove;
    }

    private int Negamax(int depth, int ply, int alpha, int beta)
    {
        if ((DateTime.Now - start).TotalMilliseconds > timeLimit) return -CHECKMATE;
        if (board.IsInCheckmate()) return -CHECKMATE + ply;
        if (board.IsDraw()) return 0;
        if (depth <= 0) return Eval();

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        
        foreach (Move move in moves)
        {
            nodes++;
            
            board.MakeMove(move);
            int evaluation = -Negamax(depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation >= beta) return beta;

            if (evaluation > alpha)
            {
                if (ply == 0) depthMove = move;
                alpha = evaluation;
                PvTable[ply].length = 1 + PvTable[ply + 1].length;
                PvTable[ply].line[0] = move;
		        Array.Copy(PvTable[ply + 1].line, 0, PvTable[ply].line, 1, PvTable[ply + 1].length);
            }
        }
        
        return alpha;
    }

    private int Eval()
    {
        int color = Convert.ToInt32(board.IsWhiteToMove);
        int[] score = { 0, 0 };

        for (int pieceType = 1; pieceType <= 6; pieceType++)
        {
            ulong whiteBb = board.GetPieceBitboard((PieceType)pieceType, true);
            ulong blackBb = board.GetPieceBitboard((PieceType)pieceType, false);

            while (whiteBb > 0)
            {
                Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref whiteBb));
                score[1] += PieceValue[pieceType];
            }

            while (blackBb > 0)
            {
                Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref blackBb) ^ 56);
                score[0] += PieceValue[pieceType];
            }
        }

        // Calculate the current player score  comparing it to the opponent color ^ 1 (XOR)
        return score[color] - score[color ^ 1];
    }
    
    private void LogDepth(int depth, int score)
    {
        string timeString = "\x1b[37mtime\u001b[38;5;214m " + timer.MillisecondsElapsedThisTurn + "ms\x1b[37m\x1b[0m";
        timeString += string.Concat(Enumerable.Repeat(" ", 38 - timeString.Length));

        string depthString = "\x1b[1m\u001b[38;2;251;96;27mdepth " + (depth) + " ply\x1b[0m";
        depthString += string.Concat(Enumerable.Repeat(" ", 38 - depthString.Length));

        string bestEvalString = string.Format("\x1b[37meval\x1b[36m {0:0} \x1b[37m", score);
        bestEvalString += string.Concat(Enumerable.Repeat(" ", 27 - bestEvalString.Length));

        string nodesString = "\x1b[37mnodes\x1b[35m " + nodes + "\x1b[37m";
        nodesString += string.Concat(Enumerable.Repeat(" ", 29 - nodesString.Length));

        string plString = "\x1b[37mpv\x1b[33m " + string.Join(" ",
            PvTable[0].line.Where(x => !x.Equals(Move.NullMove)).Select(x =>
                x.MovePieceType + " " + x.StartSquare.Name + x.TargetSquare.Name));

        Console.WriteLine(string.Join(" ",
            new string[]
            {
                depthString, timeString, bestEvalString, nodesString, plString
            }));
    }
    
}