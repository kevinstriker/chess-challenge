using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class TyrantBot8Test
{
    /*
    private TyrantBot8 _bot;
    private Board _testBoard;
    private Stopwatch _stopwatch;
    
    [SetUp]
    public void Setup()
    {
        _bot = new TyrantBot8()
        {
            Timer = new Timer(60000000, 60000000, 0), 
            HistoryHeuristics = new int[2, 7, 64],
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
            int score = _bot.PVS(depth, 0, -100_000, 100_000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(23000));
    }
    
    [Test]
    public void TestGiveUpQueenToWinItBack()
    {
        _testBoard = Board.CreateBoardFromFEN("r2q1rk1/1pp1bppp/4bn2/pP6/n2Bp3/P3P1NP/2PN1PP1/2RQKB1R b K - 2 13");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 15; depth++)
        {
            int score = _bot.PVS(depth, 0, -100_000, 100_000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("d4"));
    }
    
    [Test]
    public void TestKingSafetyDoNotTakePawn()
    {
        // Do not take pawn with king since it will lose the queen much later
        _testBoard = Board.CreateBoardFromFEN("5rk1/2p2qp1/3b3p/8/2PQ4/3P3P/Pr1B1Pp1/3R1RK1 w - - 0 23");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 18; depth++)
        {
            int score = _bot.PVS(depth, 0, -100000, 100000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("f1"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("e1"));
    }
    */
}