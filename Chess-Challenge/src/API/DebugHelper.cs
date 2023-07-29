using System;
using System.Linq;

namespace ChessChallenge.API;

public class PrincipleLine {
    public Move[] Line = new Move[128];
    public int Length = 0;
}

public static class DebugHelper
{
    public static void UpdatePrincipleLine(ref PrincipleLine[] principleLines, int ply, Move move)
    {
        principleLines[ply].Length = 1 + principleLines[ply + 1].Length;
        principleLines[ply].Line[0] = move;
        Array.Copy(principleLines[ply + 1].Line, 0, principleLines[ply].Line, 1, principleLines[ply + 1].Length);
    }
    
    public static void LogDepth(int depth, int score, long nodes, long qnodes, Timer timer, PrincipleLine[] PvTable, Move bestMoveRoot)
    {
        string timeString = "\x1b[37mtime\u001b[38;5;214m " + timer.MillisecondsElapsedThisTurn + "ms\x1b[37m\x1b[0m";
        timeString += string.Concat(Enumerable.Repeat(" ", 38 - timeString.Length));

        string depthString = "\x1b[1m\u001b[38;2;251;96;27mdepth " + (depth) + " ply\x1b[0m";
        depthString += string.Concat(Enumerable.Repeat(" ", 38 - depthString.Length));
        
        string bestEvalString = string.Format("\x1b[37meval\x1b[36m {0:0} \x1b[37m", score);
        bestEvalString += string.Concat(Enumerable.Repeat(" ", 27 - bestEvalString.Length));

        string nodesString = "\x1b[37mnodes\x1b[35m " + nodes + "\x1b[37m";
        nodesString += string.Concat(Enumerable.Repeat(" ", 29 - nodesString.Length));
        
        string qnodesString = "\x1b[37mqnodes\x1b[34m " + qnodes + "\x1b[37m";
        qnodesString += string.Concat(Enumerable.Repeat(" ", 32 - qnodesString.Length));

        string plString = "\x1b[37mpv\x1b[33m " + string.Join(" ",
            PvTable[0].Line.Where(x => !x.Equals(Move.NullMove)).Select(x =>
                x.MovePieceType + " " + x.StartSquare.Name + x.TargetSquare.Name));

        Console.WriteLine(string.Join(" ",
            new string[]
            {
                depthString, timeString, bestEvalString, nodesString, qnodesString, plString
            }));
    }
}