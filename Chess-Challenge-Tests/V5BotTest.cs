using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class V5BotTest
{
    private V5 _bot;
    private Board _testBoard;
    private Stopwatch _stopwatch;

    [SetUp]
    public void Setup()
    {
        _bot = new V5
        {
            Timer = new Timer(60000000, 60000000, 0),
            TimeLimit = 1000000000,
            HistoryHeuristics = new int[2, 7, 64],
        };
        _stopwatch = Stopwatch.StartNew();
    }
    
    
    
    

    #region Logging

    private void LogAll(int depth, int score)
    {
        Console.WriteLine("depth {0} score {1}, time {2}ms, nodes {3} qNodes {4}, nps {5}, DepthMove {6}-{7}{8}",
            depth,
            score,
            _stopwatch.ElapsedMilliseconds,
            _bot.Nodes,
            _bot.QNodes,
            (_bot.Nodes + _bot.QNodes) / (_stopwatch.ElapsedMilliseconds + 1) * 1000,
            _bot.BestMove.MovePieceType.ToString(),
            _bot.BestMove.StartSquare.Name,
            _bot.BestMove.TargetSquare.Name);
    }
    
    #endregion
    
}