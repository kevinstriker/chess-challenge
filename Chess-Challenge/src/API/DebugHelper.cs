using System;
using System.Linq;
using ChessChallenge.API;

public static class DebugHelper
{
    private static string GetPv(Board board, MyBot.TtEntry[] tt, ulong ttEntries, int depth)
    {
        ulong key = board.ZobristKey;
        MyBot.TtEntry ttEntry = tt[key % ttEntries];

        if (ttEntry.Key == key && depth > 0)
        {
            board.MakeMove(ttEntry.Move);
            string subLine = GetPv(board, tt, ttEntries, depth - 1);
            board.UndoMove(ttEntry.Move);
            return $"{ttEntry.Move.MovePieceType} {ttEntry.Move.StartSquare.Name}{ttEntry.Move.TargetSquare.Name} {subLine}";
        }

        return "";
    }

    public static void LogDepth(Board board, Timer timer, MyBot.TtEntry[] tt, int depth, int score, long nodes, long qNodes)
    {
        string timeString = "\x1b[37mtime\u001b[38;5;214m " + timer.MillisecondsElapsedThisTurn + "ms\x1b[37m\x1b[0m";
        timeString += string.Concat(Enumerable.Repeat(" ", 38 - timeString.Length));

        string depthString = "\x1b[1m\u001b[38;2;251;96;27mdepth " + (depth) + " ply\x1b[0m";
        depthString += string.Concat(Enumerable.Repeat(" ", 38 - depthString.Length));

        string bestEvalString = string.Format("\x1b[37meval\x1b[36m {0:0} \x1b[37m", score);
        bestEvalString += string.Concat(Enumerable.Repeat(" ", 27 - bestEvalString.Length));

        string nodesString = "\x1b[37mnodes\x1b[35m " + nodes + "\x1b[37m";
        nodesString += string.Concat(Enumerable.Repeat(" ", 29 - nodesString.Length));

        string qnodesString = "\x1b[37mqnodes\x1b[34m " + qNodes + "\x1b[37m";
        qnodesString += string.Concat(Enumerable.Repeat(" ", 32 - qnodesString.Length));

        string pvString = "\x1b[37mpv\x1b[33m " + GetPv(board, tt, MyBot.TtEntryCount, depth);

        Console.WriteLine(string.Join(" ",
            new string[]
            {
                depthString, timeString, bestEvalString, nodesString, qnodesString, pvString
            }));
    }
}