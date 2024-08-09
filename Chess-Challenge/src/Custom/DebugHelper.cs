using System;
using System.Linq;
using ChessChallenge.API;

public static class DebugHelper
{
    public static void LogDepth(String botName, Timer timer, int depth, double score, int nodes, int qnodes, Move bestMove)
    {
        // Color codes
        string textColor = "\x1b[37m";
        string boldText = "\x1b[1m";
        string orangeColor = "\x1b[38;2;251;96;27m";
        string cyanColor = "\x1b[36m";
        string purpleColor = "\x1b[35m";
        string blueColor = "\x1b[34m";
        string yellowColor = "\x1b[33m";
        string greenColor = "\x1b[32m";
        string tealColor = "\x1b[38;5;51m";
        string resetFormat = "\x1b[0m";

        // Depth level
        botName = botName.Replace("Bot", "");
        string depthString = $"{boldText}{orangeColor}{botName} - {depth} ply{resetFormat}";
        depthString += string.Concat(Enumerable.Repeat(" ", 46 - depthString.Length));

        // Time
        string timeString =
            $"{textColor}time{orangeColor} {yellowColor}{timer.MillisecondsElapsedThisTurn}ms{resetFormat}";
        timeString += string.Concat(Enumerable.Repeat(" ", 46 - timeString.Length));

        // Best Eval
        string bestEvalString = $"{textColor}eval{cyanColor} {score:0} ";
        bestEvalString += $"{cyanColor}{textColor}";
        bestEvalString += string.Concat(Enumerable.Repeat(" ", 36 - bestEvalString.Length));

        // Nodes
        string nodesString = $"{textColor}nodes{purpleColor} {greenColor}{nodes}{textColor}";
        nodesString += string.Concat(Enumerable.Repeat(" ", 40 - nodesString.Length));

        // Q Nodes
        string qnodesString = $"{textColor}qnodes{purpleColor} {qnodes}{textColor}";
        qnodesString += string.Concat(Enumerable.Repeat(" ", 40 - qnodesString.Length));

        // Nodes per second
        int nps =  (nodes + qnodes) / Math.Max(timer.MillisecondsElapsedThisTurn, 1);
        string npsString = $"{textColor}nps{blueColor} {nps}{textColor}k";
        npsString += string.Concat(Enumerable.Repeat(" ", 34 - npsString.Length));

        // Best move String 
        string bestMoveString =
            $"{textColor}pv{tealColor} {bestMove.MovePieceType} - {bestMove.StartSquare.Name}{bestMove.TargetSquare.Name}";

        Console.WriteLine(string.Join(" ", depthString, timeString, bestEvalString, nodesString, qnodesString, npsString, bestMoveString));
    }
}