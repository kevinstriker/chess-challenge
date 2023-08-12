using System;
using System.Linq;
using ChessChallenge.API;

public static class DebugHelper
{
    public static void LogDepth(Timer timer, int depth, int score, MyBot myBot)
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
        string depthString = $"{boldText}{orangeColor}depth {depth} ply{resetFormat}";
        depthString += string.Concat(Enumerable.Repeat(" ", 40 - depthString.Length));
        
        // Time
        string timeString =
            $"{textColor}time{orangeColor} {yellowColor}{timer.MillisecondsElapsedThisTurn}ms{resetFormat}";
        timeString += string.Concat(Enumerable.Repeat(" ", 46 - timeString.Length));
        
        // Best Eval
        string bestEvalString = $"{textColor}eval{cyanColor} {score:0} ";
        bestEvalString += $"{cyanColor}{textColor}";
        bestEvalString += string.Concat(Enumerable.Repeat(" ", 38 - bestEvalString.Length));

        // Nodes
        string nodesString = $"{textColor}nodes{purpleColor} {greenColor}{myBot.Nodes}{textColor}";
        nodesString += string.Concat(Enumerable.Repeat(" ", 40 - nodesString.Length));

        // Q Nodes
        string qnodesString = $"{textColor}qnodes{purpleColor} {myBot.QNodes}{textColor}";
        qnodesString += string.Concat(Enumerable.Repeat(" ", 40 - qnodesString.Length));

        // Nodes per second
        long nps = 1000 * (myBot.Nodes + myBot.QNodes) / (timer.MillisecondsElapsedThisTurn + 1);
        string npsString = $"{textColor}nps{blueColor} {nps}{textColor}";
        npsString += string.Concat(Enumerable.Repeat(" ", 40 - npsString.Length));

        // Best move String 
        string bestMoveString = $"{textColor}pv{tealColor} {myBot.BestMove.MovePieceType} - {myBot.BestMove.StartSquare.Name}{myBot.BestMove.TargetSquare.Name}";

        Console.WriteLine(string.Join(" ",
            new string[]
            {
                depthString, timeString, bestEvalString, nodesString, qnodesString, npsString, bestMoveString
            }));
    }
}