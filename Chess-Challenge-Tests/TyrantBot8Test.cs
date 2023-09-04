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

    #region General
    
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
    
    #endregion
    
    [Test]
    public void TestStartingPosition()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        _bot.Board = _testBoard;

        IterativePvs();

        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(23000));
    }
    
    [Test]
    // TODO try very agressive pruning
    public void TestMoveQueenNotTrade()
    {
        _testBoard = Board.CreateBoardFromFEN("6k1/1p3pp1/1p1p2r1/pP1P3p/P2RPp2/q1r1NP1P/2Q3PK/1R6 w - - 0 39");
        _bot.Board = _testBoard;

        IterativePvs(1, 18);

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Queen));
    }

    [Test]
    public void TestKingSafetyDoNotTakePawn()
    {
        // Do not take pawn with king since it will lose the queen much later
        _testBoard = Board.CreateBoardFromFEN("5rk1/2p2qp1/3b3p/8/2PQ4/3P3P/Pr1B1Pp1/3R1RK1 w - - 0 23");
        _bot.Board = _testBoard;
        
        IterativePvs(1, 18);

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("f1"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("e1"));
    }
    */
}