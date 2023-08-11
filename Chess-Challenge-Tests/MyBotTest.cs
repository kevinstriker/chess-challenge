using System.Diagnostics;
using ChessChallenge.API;

namespace Chess_Challenge_Tests;

public class MyBotTest
{
    private MyBot _myBot;
    private Board _testBoard;
    private Stopwatch _stopwatch;

    [SetUp]
    public void Setup()
    {
        _myBot = new MyBot
        {
            InTest = true
        };
        _stopwatch = Stopwatch.StartNew();
    }

    #region Puzzles

    [Test]
    public void TestPuzzle1()
    {
        // Checkmate by queen sacrifice
        const string fen = "2r2bk1/p5p1/1p1p2Qp/2PNp3/PR1nNr1q/3P4/5PPP/5RK1 b - - 0 1";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        int score = _myBot.NegaMax(_testBoard, 6, 0, -30000, 30000);

        _stopwatch.Stop();

        Assert.That(score, Is.EqualTo(30000));
        Assert.That(_myBot.DepthMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_myBot.DepthMove.StartSquare.Name, Is.EqualTo("d4"));
        Assert.That(_myBot.DepthMove.TargetSquare.Name, Is.EqualTo("e2"));

        LogAll(score);
    }

    [Test]
    public void TestPuzzle2()
    {
        // Move queen out of the way to threat mate and win rook if not mate
        const string fen = "4rq1k/3r1p1p/3p1p2/1p1P2Q1/p1n2P2/P1P4R/1P3RPK/8 w - - 0 1";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        int score = _myBot.NegaMax(_testBoard, 4, 0, -30000, 30000);

        _stopwatch.Stop();

        Assert.That(_myBot.DepthMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_myBot.DepthMove.StartSquare.Name, Is.EqualTo("g5"));
        Assert.That(_myBot.DepthMove.TargetSquare.Name, Is.EqualTo("f5"));

        LogAll(score);
    }

    [Test]
    public void TestPuzzle3()
    {
        // Trap a queen with 2 rooks
        const string fen = "3r2r1/Q1pk1p2/2p2nqp/2Pp4/8/4PP1P/PP1B1RP1/R5K1 b - - 0 1";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        int score = _myBot.NegaMax(_testBoard, 6, 0, -30000, 30000);

        _stopwatch.Stop();

        Assert.That(_myBot.DepthMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_myBot.DepthMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_myBot.DepthMove.TargetSquare.Name, Is.EqualTo("a8"));

        LogAll(score);
    }

    #endregion

    #region Move ordering

    [Test]
    public void TestMoveOrdering()
    {
        const string fen = "2b1kb1r/P1pppppp/4nn2/1N4q1/2B2B2/4PN1P/1PPPQPP1/R3K2R w KQk - 0 1";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        int score = _myBot.NegaMax(_testBoard, 1, 0, -30000, 30000);

        _stopwatch.Stop();
        

        LogAll(score);
    }
    
    #endregion

    #region Defensive

    [Test]
    public void TestDodgePawnCaptureAndPromotion()
    {
        const string fen = "r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        int score = _myBot.NegaMax(_testBoard, 4, 0, -30000, 30000);

        _stopwatch.Stop();

        Assert.That(_myBot.DepthMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_myBot.DepthMove.StartSquare.Name, Is.EqualTo("a1"));
        Assert.That(_myBot.DepthMove.TargetSquare.Name, Is.EqualTo("b1"));

        LogAll(score);
    }

    #endregion

    #region Checkmate

    [Test]
    public void TestCheckMateIn2()
    {
        const string fen = "rnbq4/pp1pkB2/3p2P1/6b1/3PP2Q/2N5/PPP1K1P1/r7 w - - 2 18";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        int score = _myBot.NegaMax(_testBoard, 4, 0, -30000, 30000);

        _stopwatch.Stop();

        Assert.That(_myBot.DepthMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_myBot.DepthMove.StartSquare.Name, Is.EqualTo("c3"));
        Assert.That(_myBot.DepthMove.TargetSquare.Name, Is.EqualTo("d5"));

        LogAll(score);
    }

    #endregion

    #region Pawns

    [Test]
    public void TestPromotePawnToQueen()
    {
        const string fen = "2r3k1/5p2/6pB/1Q6/3bR2P/7K/Pp2P1B1/6q1 b - - 2 35";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();

        int score = _myBot.NegaMax(_testBoard, 4, 0, -30000, 30000);

        _stopwatch.Stop();

        Assert.That(_myBot.DepthMove.MovePieceType, Is.EqualTo(PieceType.Pawn));
        Assert.That(_myBot.DepthMove.StartSquare.Name, Is.EqualTo("b2"));
        Assert.That(_myBot.DepthMove.TargetSquare.Name, Is.EqualTo("b1"));

        LogAll(score);
    }

    #endregion

    #region Logging

    private void LogAll(int score)
    {
        Console.WriteLine("score {0}, time {1}ms, nodes {2}, nps {3}, DepthMove {4}-{5}{6}",
            score,
            _stopwatch.ElapsedMilliseconds,
            _myBot.Nodes,
            1000 * _myBot.Nodes / (_stopwatch.ElapsedMilliseconds + 1),
            _myBot.DepthMove.MovePieceType.ToString(),
            _myBot.DepthMove.StartSquare.Name,
            _myBot.DepthMove.TargetSquare.Name);
    }

    #endregion


    #region Hardware

    // https://github.com/SebLague/Chess-Challenge/issues/381
    /*
    [Test]
    public void Takes5SecondOnTournamentTest()
    {
        string fen = "r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _stopwatch.Restart();
        
        SearchFullForHardwareCheck(5);;
        
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