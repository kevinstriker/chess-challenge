using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class MyBotTest
{
    private MyBot _bot;
    private Board _testBoard;
    private Stopwatch _stopwatch;

    [SetUp]
    public void Setup()
    {
        _bot = new MyBot
        {
            Timer = new Timer(60000000, 60000000, 0),
            TimeLimit = 1000000000,
            HistoryHeuristics = new int[2, 7, 64],
        };
        _stopwatch = Stopwatch.StartNew();
    }

    #region Higher Depth

    [Test]
    public void TestGiveUpQueenToWinItBack()
    {
        // Give up queen to win one later
        _testBoard = Board.CreateBoardFromFEN("r2q1rk1/1pp1bppp/4bn2/pP6/n2Bp3/P3P1NP/2PN1PP1/2RQKB1R b K - 2 13");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 11; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
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

        for (int depth = 1; depth <= 13; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("f1"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("e1"));
    }

    

    #endregion
    
    #region Lower Depth

    [Test]
    public void TestDoNotGoForPawnAdvantage()
    {
        _testBoard = Board.CreateBoardFromFEN("r1b1k2r/pp2nppp/2pp1q2/5P2/2BbP3/2N1B3/PP3QPP/R4RK1 b kq - 1 18");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 11; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Bishop));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("d4"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("e3"));
    }

    
    [Test]
    public void TestWinRookByCheckMateThreat()
    {
        _testBoard = Board.CreateBoardFromFEN("4rq1k/3r1p1p/3p1p2/1p1P2Q1/p1n2P2/P1P4R/1P3RPK/8 w - - 0 1");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("g5"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("f5"));
    }

    [Test]
    public void TestTrapQueenWithTwoRooks()
    {
        _testBoard = Board.CreateBoardFromFEN("3r2r1/Q1pk1p2/2p2nqp/2Pp4/8/4PP1P/PP1B1RP1/R5K1 b - - 0 1");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("a8"));
    }

    [Test]
    public void TestWinRookAndPawnForBishop()
    {
        _testBoard = Board.CreateBoardFromFEN("rn3r1k/pp3p1p/4bN2/q3Q3/1b1p3P/5N2/PPP2PP1/2KR1B1R b - - 2 14");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("a5"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("a2"));
    }

    [Test]
    public void TestPressureWithPawnForKnightForRook()
    {
        // Put pressure with moving a small piece (pawn) and win a rook for a knight
        _testBoard = Board.CreateBoardFromFEN("8/5kp1/1N3p2/1p4p1/1P6/P5P1/R2n1PKP/3r4 b - - 14 39");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 10; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Pawn));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("g5"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("g4"));
    }

    [Test]
    public void TestMoveRookSimpleDamageControl()
    {
        _testBoard = Board.CreateBoardFromFEN("r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("a1"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("b1"));
    }


    [Test]
    public void TestChangeBishopForKnightToAfterPinQueen()
    {
        // Change bishop for knight to pin queen to king after with other bishop 
        _testBoard = Board.CreateBoardFromFEN("2kr2r1/pppq1p2/2npbp1p/2bNp3/2P1P1P1/3P1N1P/PP1QBP2/R3K2R b KQ - 1 1");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 9; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Bishop));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("e6"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("d5"));
    }
    
    #endregion

    #region Starting position

    [Test]
    /*
     * Info: depth:  2 || eval:     12 || nodes:       183 || nps:    16636 || time:    10ms || best move: g1f3
     * Info: depth:  3 || eval:     22 || nodes:       323 || nps:    26916 || time:    11ms || best move: g1f3
     * Info: depth:  4 || eval:     12 || nodes:       612 || nps:    51000 || time:    11ms || best move: g1f3
     * Info: depth:  5 || eval:     19 || nodes:      1557 || nps:   111214 || time:    13ms || best move: g1f3
     * Info: depth:  6 || eval:     12 || nodes:      3850 || nps:   213888 || time:    17ms || best move: g1f3
     * Info: depth:  7 || eval:     16 || nodes:      9257 || nps:   342851 || time:    26ms || best move: g1f3
     * Info: depth:  8 || eval:     12 || nodes:     19395 || nps:   440795 || time:    43ms || best move: g1f3
     * Info: depth:  9 || eval:     20 || nodes:     72068 || nps:   637769 || time:   112ms || best move: b1c3
     * Info: depth: 10 || eval:     12 || nodes:    150731 || nps:   731703 || time:   205ms || best move: b1c3
     * Info: depth: 11 || eval:     18 || nodes:    398854 || nps:  1030630 || time:   386ms || best move: g1f3
     * Info: depth: 12 || eval:     12 || nodes:    837791 || nps:  1362261 || time:   614ms || best move: d2d4
     * Info: depth: 13 || eval:     15 || nodes:   1574613 || nps:  1462036 || time:  1076ms || best move: d2d4
     * Info: depth: 14 || eval:     13 || nodes:   2612874 || nps:  1611890 || time:  1620ms || best move: d2d4
     * Info: depth: 15 || eval:     19 || nodes:   5558057 || nps:  1762224 || time:  3153ms || best move: d2d4
     * Info: depth: 16 || eval:     13 || nodes:  13872494 || nps:  1853125 || time:  7485ms || best move: d2d4
     * Info: depth: 17 || eval:     13 || nodes:  29057297 || nps:  1885123 || time: 15413ms || best move: d2d4
     * Info: depth: 18 || eval:      9 || nodes:  80326989 || nps:  1873603 || time: 42872ms || best move: d2d4
     **/
    public void TestStartingPosition()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 11; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(20000));
    }

    #endregion

    #region Move ordering

    [Test]
    public void TestMoveOrdering()
    {
        _testBoard = Board.CreateBoardFromFEN("2b1kb1r/P1pppppp/4nn2/1N4q1/2B2B2/4PN1P/1PPPQPP1/R3K2R w KQk - 0 1");
        _bot.Board = _testBoard;

        int score = _bot.Pvs(1, 0, -100000, 100000);

        LogAll(1, score);
    }

    #endregion

    #region Checkmate

    [Test]
    public void TestCheckMateIn1()
    {
        _testBoard = Board.CreateBoardFromFEN("r1b1r1k1/pp3ppp/1qpp4/8/2PbP3/P3p1P1/1PN1Q2P/2KR1B1R b - - 1 19");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 6; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("b6"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("b2"));
    }

    [Test]
    public void TestCheckMateIn2()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbq4/pp1pkB2/3p2P1/6b1/3PP2Q/2N5/PPP1K1P1/r7 w - - 2 18");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("c3"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("d5"));
    }

    [Test]
    public void TestCheckMateByQueenSacrafice()
    {
        // Checkmate by queen sacrifice
        _testBoard = Board.CreateBoardFromFEN("2r2bk1/p5p1/1p1p2Qp/2PNp3/PR1nNr1q/3P4/5PPP/5RK1 b - - 0 1");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 6; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000);
            LogAll(depth, score);
        }

        Assert.That(_bot.BestMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.BestMove.StartSquare.Name, Is.EqualTo("d4"));
        Assert.That(_bot.BestMove.TargetSquare.Name, Is.EqualTo("e2"));
    }

    #endregion

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

    #region Hardware

    // https://github.com/SebLague/Chess-Challenge/issues/381
    [Test]
    public void Takes5SecondOnTournamentTest()
    {
        string fen = "r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        SearchFullForHardwareCheck(5);

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

    #endregion
}