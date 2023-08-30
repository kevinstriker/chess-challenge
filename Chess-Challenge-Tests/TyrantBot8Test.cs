using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class TyrantBot8Test
{
    
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
    
    private void IterativePvs(int startDepth = 1, int maxDepth = 16)
    {
        // Iterative Deepening
        for (int depth = startDepth, alpha = -999999, beta = 999999, eval; depth <= maxDepth;)
        {
            eval = _bot.PVS(depth, 0, alpha, beta, true);

            // Gradual widening
            // Fell outside window, retry with wider window search
            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            else
            {
                // Fell inside window
                DebugHelper.LogDepth(_bot.Timer, depth, eval, _bot);

                // Set up window for next search
                alpha = eval - 17;
                beta = eval + 17;
                depth++;
            }
        }
    }
    
    [Test]
    public void TestStartingPosition()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        _bot.Board = _testBoard;

        IterativePvs();

        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(23000));
    }
    
    [Test]
    public void TestMoveBishopSafe()
    {
        _testBoard = Board.CreateBoardFromFEN("r1b2rk1/pp2ppbp/1q4B1/2n5/8/2P1QNN1/PP3PPP/R3K2R w KQ - 1 14");
        _bot.Board = _testBoard;

        IterativePvs();

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Bishop));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("g6"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("c2"));
    }
}