using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class LiteBlueBotTest
{
    private LiteBlueBot7 _bot;
    private Board _testBoard;
    private Stopwatch _stopwatch;
    
    [SetUp]
    public void Setup()
    {
        _bot = new LiteBlueBot7
        {
            Timer = new Timer(60000000, 60000000, 0),
            HistoryTable = new int[2, 7, 64],
            TimeLimit = 100000000
        };
        _stopwatch = Stopwatch.StartNew();
    }
    
    
    [Test]
    public void TestStartingPosition()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 15; depth++)
        {
            int score = _bot.Negamax(depth, 0, -100000, 100000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(20000));
    }
    
    [Test]
    public void TestGiveUpQueenToWinItBack()
    {
        _testBoard = Board.CreateBoardFromFEN("r2q1rk1/1pp1bppp/4bn2/pP6/n2Bp3/P3P1NP/2PN1PP1/2RQKB1R b K - 2 13");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 15; depth++)
        {
            int score = _bot.Negamax(depth, 0, -100000, 100000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("d4"));
    }

}