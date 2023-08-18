using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class LteBlueBotTest
{
    private LiteBlueBot6 _bot;
    private Board _testBoard;
    private Stopwatch _stopwatch;
    
    [SetUp]
    public void Setup()
    {
        _bot = new LiteBlueBot6
        {
            timer = new Timer(60000000, 60000000, 0),
            history_table = new int[2, 7, 64],
            time_limit = 100000000
        };
        _stopwatch = Stopwatch.StartNew();
    }

    #region Puzzles

    [Test]
    public void TestPuzzle1()
    {
        // Checkmate by queen sacrifice
        _testBoard = Board.CreateBoardFromFEN("2r2bk1/p5p1/1p1p2Qp/2PNp3/PR1nNr1q/3P4/5PPP/5RK1 b - - 0 1");
        
        for (int depth = 1; depth <= 6; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("d4"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("e2"));
    }

    [Test]
    public void TestPuzzle2()
    {
        // Move queen out of the way to threat mate and win rook if not mate
        _testBoard = Board.CreateBoardFromFEN("4rq1k/3r1p1p/3p1p2/1p1P2Q1/p1n2P2/P1P4R/1P3RPK/8 w - - 0 1");

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("g5"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("f5"));
    }

    [Test]
    public void TestPuzzle3()
    {
        // Trap a queen with 2 rooks
        _testBoard = Board.CreateBoardFromFEN("3r2r1/Q1pk1p2/2p2nqp/2Pp4/8/4PP1P/PP1B1RP1/R5K1 b - - 0 1");

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("a8"));
    }

    [Test]
    public void TestPuzzle4()
    {
        // Win a Rook and a Pawn for a bishop
        _testBoard = Board.CreateBoardFromFEN("rn3r1k/pp3p1p/4bN2/q3Q3/1b1p3P/5N2/PPP2PP1/2KR1B1R b - - 2 14");

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("a5"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("a2"));
    }

    [Test]
    public void TestPuzzle5()
    {
        // Put pressure with moving a small piece (pawn) and win a rook for a knight
        _testBoard = Board.CreateBoardFromFEN("8/5kp1/1N3p2/1p4p1/1P6/P5P1/R2n1PKP/3r4 b - - 14 39");

        for (int depth = 1; depth <= 10; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Pawn));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("g5"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("g4"));
    }

    [Test]
    public void TestPuzzle6()
    {
        // Simple "damage control" puzzle to prevent promotion there is only 1 reasonable move
        _testBoard = Board.CreateBoardFromFEN("r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16");

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("a1"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("b1"));
    }

    [Test]
    public void TestPuzzle7()
    {
        // Trap queen with pawn attack
        _testBoard = Board.CreateBoardFromFEN("3q1k2/5ppp/r1p1p3/p1np4/3Qn3/P1P1P3/3N1PPP/R3K2R b KQ - 3 17");

        for (int depth = 1; depth <= 6; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Pawn));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("e6"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("e5"));
    }

    [Test]
    public void TestPuzzle8()
    {
        // Change bishop for knight to pin queen to king after with other bishop 
        _testBoard = Board.CreateBoardFromFEN("2kr2r1/pppq1p2/2npbp1p/2bNp3/2P1P1P1/3P1N1P/PP1QBP2/R3K2R b KQ - 1 1");

        for (int depth = 1; depth <= 9; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Bishop));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("e6"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("d5"));
    }

    [Test]
    public void TestPuzzle9()
    {
        // Bishop shouldn't go for pawn advantage since it means you get wayyy behind
        _testBoard = Board.CreateBoardFromFEN("r1b1k2r/pp2nppp/2pp1q2/5P2/2BbP3/2N1B3/PP3QPP/R4RK1 b kq - 1 18");

        for (int depth = 1; depth <= 9; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Bishop));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("d4"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("e3"));
    }

    [Test]
    public void TestPuzzle10()
    {
        // Give up queen to win one later
        _testBoard = Board.CreateBoardFromFEN("r2q1rk1/1pp1bppp/4bn2/pP6/n2Bp3/P3P1NP/2PN1PP1/2RQKB1R b K - 2 13");

        for (int depth = 1; depth <= 10; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("d4"));
    }

    [Test]
    public void TestPuzzle11()
    {
        // Give up queen to win one later
        _testBoard = Board.CreateBoardFromFEN("5rk1/2p2qp1/3b3p/8/2PQ4/3P3P/Pr1B1Pp1/3R1RK1 w - - 0 23");

        for (int depth = 1; depth <= 12; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("f1"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("e1"));
    }
    
    #endregion

    #region Move ordering

    [Test]
    public void TestMoveOrdering()
    {
        _testBoard = Board.CreateBoardFromFEN("2b1kb1r/P1pppppp/4nn2/1N4q1/2B2B2/4PN1P/1PPPQPP1/R3K2R w KQk - 0 1");
        
        int score = _bot.Search(_testBoard, 1, 0, -100000, 100000, true);
        
        LogAll(score);
    }

    #endregion

    #region Checkmate

    [Test]
    public void TestCheckMateIn2()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbq4/pp1pkB2/3p2P1/6b1/3PP2Q/2N5/PPP1K1P1/r7 w - - 2 18");
        
        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Search(_testBoard, depth, 0, -100000, 100000, true);
            LogAll(score);
        }

        Assert.That(_bot.best_move_root.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.best_move_root.StartSquare.Name, Is.EqualTo("c3"));
        Assert.That(_bot.best_move_root.TargetSquare.Name, Is.EqualTo("d5"));
    }

    #endregion

    #region Logging

    private void LogAll(int score)
    {
        Console.WriteLine("score {0}, time {1}ms, nodes {2} qNodes {3}, nps {4}, DepthMove {5}-{6}{7}",
            score,
            _stopwatch.ElapsedMilliseconds,
            _bot.Nodes,
            _bot.QNodes,
            (_bot.Nodes + _bot.QNodes) / (_stopwatch.ElapsedMilliseconds + 1) * 1000,
            _bot.best_move_root.MovePieceType.ToString(),
            _bot.best_move_root.StartSquare.Name,
            _bot.best_move_root.TargetSquare.Name);
    }

    #endregion

    #region Hardware

    /*
    // https://github.com/SebLague/Chess-Challenge/issues/381
    [Test]
    public void Takes5SecondOnTournamentTest()
    {
        string fen = "r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        SearchFullForHardwareCheck(5);
        ;

        _stopwatch.Stop();

        Console.WriteLine(_stopwatch.ElapsedMilliseconds + " ms.");
    }

    private void SearchFullForHardwareCheck(int depthRemaining)
    {
        if (depthRemaining == 0) return;
        Span<Move> moves = stackalloc Move[218];
        _testBoard.GetLegalMovesNonAlloc(ref moves);
        foreach (var m in moves)
        {
            _testBoard.MakeMove(m);
            SearchFullForHardwareCheck(depthRemaining - 1);
            _testBoard.UndoMove(m);
        }
    }
    */

    #endregion
}