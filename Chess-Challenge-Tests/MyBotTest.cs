using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class MyBotTest
{
    
    private MyBot _bot;
    private Board _testBoard;
    private Stopwatch _stopwatch;

    #region General

    [SetUp]
    public void Setup()
    {
        _bot = new MyBot();
        _stopwatch = Stopwatch.StartNew();
    }

    private void ExecuteThink(Board board, int timeTotalMs = 2000)
    {
        Move move = _bot.Think(board, new Timer(timeTotalMs * 30, timeTotalMs * 30, 0));
    }

    #endregion

    /*
    #region Puzzles
    
    [Test]
    public void Puzzle1()
    {
        _testBoard = Board.CreateBoardFromFEN("8/1p3qbQ/3p4/P2P2k1/4Pp2/3B1Pp1/4K1P1/8 w - - 0 1");

        ExecuteThink(_testBoard, 2500);
        
        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Pawn));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("e4"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("e5"));
    }
    
    [Test]
    public void Puzzle2()
    {
        _testBoard = Board.CreateBoardFromFEN("6k1/3q2b1/1nNp4/3Pp1pp/4Pp2/5P2/6PP/2RQ2K1 w - - 0 1");

        ExecuteThink(_testBoard, 2500);
        
        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("d1"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("b3"));
    }

    #endregion

    #region Mistakes in games

    
    [Test]
    public void TestMoveBishopSafe()
    {
        _testBoard = Board.CreateBoardFromFEN("r1b2rk1/pp2ppbp/1q4B1/2n5/8/2P1QNN1/PP3PPP/R3K2R w KQ - 1 14");
        
        ExecuteThink(_testBoard, 4000);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Bishop));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("g6"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("b1"));
    }

    [Test]
    public void TestMateFind()
    {
        _testBoard = Board.CreateBoardFromFEN("8/8/6r1/3k4/1K6/8/8/8 b - - 77 94");
        
        ExecuteThink(_testBoard);
        
        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("g6"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("g3"));
    }

    [Test]
    // TODO try very agressive pruning
    public void TestMoveQueenNotTrade()
    {
        _testBoard = Board.CreateBoardFromFEN("6k1/1p3pp1/1p1p2r1/pP1P3p/P2RPp2/q1r1NP1P/2Q3PK/1R6 w - - 0 39");
        
        ExecuteThink(_testBoard);


        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
    }

    #endregion

    #region Puzzles

    [Test]
    public void TestGiveUpQueenToWinItBack()
    {
        _testBoard = Board.CreateBoardFromFEN("r2q1rk1/1pp1bppp/4bn2/pP6/n2Bp3/P3P1NP/2PN1PP1/2RQKB1R b K - 2 13");
    
        ExecuteThink(_testBoard);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("d4"));
    }

    [Test]
    public void TestKingSafetyDoNotTakePawn()
    {
        _testBoard = Board.CreateBoardFromFEN("5rk1/2p2qp1/3b3p/8/2PQ4/3P3P/Pr1B1Pp1/3R1RK1 w - - 0 23");
        
        ExecuteThink(_testBoard);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("f1"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("e1"));
    }

    [Test]
    public void TestTrapQueenWithTwoRooks()
    {
        _testBoard = Board.CreateBoardFromFEN("3r2r1/Q1pk1p2/2p2nqp/2Pp4/8/4PP1P/PP1B1RP1/R5K1 b - - 0 1");
        
        ExecuteThink(_testBoard);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("a8"));
    }

    [Test]
    public void TestWinRookAndPawnForBishop()
    {
        _testBoard = Board.CreateBoardFromFEN("rn3r1k/pp3p1p/4bN2/q3Q3/1b1p3P/5N2/PPP2PP1/2KR1B1R b - - 2 14");
        
        ExecuteThink(_testBoard);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("a5"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("a2"));
    }

    [Test]
    public void TestPressureWithPawnForKnightForRook()
    {
        _testBoard = Board.CreateBoardFromFEN("8/5kp1/1N3p2/1p4p1/1P6/P5P1/R2n1PKP/3r4 b - - 14 39");
        
        ExecuteThink(_testBoard);


        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Pawn));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("g5"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("g4"));
    }

    #endregion

    #region Starting position

    [Test]
    public void TestStartingPosition()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        

        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(20000));
    }

    #endregion

    #region Move ordering

    [Test]
    public void TestMoveOrdering()
    {
        _testBoard = Board.CreateBoardFromFEN("2b1kb1r/P1pppppp/4nn2/1N4q1/2B2B2/4PN1P/1PPPQPP1/R3K2R w KQk - 0 1");
        
        ExecuteThink(_testBoard);
    }

    #endregion

    #region Checkmate

    [Test]
    public void TestCheckMateIn1()
    {
        _testBoard = Board.CreateBoardFromFEN("r1b1r1k1/pp3ppp/1qpp4/8/2PbP3/P3p1P1/1PN1Q2P/2KR1B1R b - - 1 19");
        
        ExecuteThink(_testBoard);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("b6"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("b2"));
    }

    [Test]
    public void TestCheckMateIn2()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbq4/pp1pkB2/3p2P1/6b1/3PP2Q/2N5/PPP1K1P1/r7 w - - 2 18");
        
        ExecuteThink(_testBoard);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("c3"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("d5"));
    }

    [Test]
    public void TestCheckMateByQueenSacrifice()
    {
        _testBoard = Board.CreateBoardFromFEN("2r2bk1/p5p1/1p1p2Qp/2PNp3/PR1nNr1q/3P4/5PPP/5RK1 b - - 0 1");
        
        ExecuteThink(_testBoard);
        
        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("d4"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("e2"));
    }

    #endregion

    #region BitBoard tests

    [Test]
    public void TestEvalPawnStructure()
    {
        _testBoard = Board.CreateBoardFromFEN("2b1k1r1/4p2p/2p1pp1p/8/P1PP4/2P5/5P2/1RB1K3 w - - 0 1");
        
        int eval = 0, color = 2;
        
        int[] materialValues = { 100, 300, 300, 500, 900, 0 };

        var pawnsPerFile = new int[2, 8];

        // Loop black and white colors of the game
        // Flip score for optimised token count (always white perspective due to double flip)
        // Eg. White eval = 2300 -> flip -> -2300 -> black eval = 2000 -> -300 -> flip -> 300 k)
        for (; --color >= 0; eval = -eval)
        {
            for (int piece = -1; ++piece < 6;)
            for (ulong mask = _testBoard.GetPieceBitboard((PieceType)piece + 1, color > 0); mask != 0;)
            {
                // A number between 0 to 63 that indicates which square the piece is on, flip for black
                int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * color;

                // Piece (material) values are baked into the PST (!)
                eval += materialValues[piece];

                if (piece == 0)
                    pawnsPerFile[color, squareIndex % 8]++;
            }
            
            // double pawns
            for (int i = 0; i < 8; i++) 
                if (pawnsPerFile[color, i] > 1)
                    eval -= 50;
        }

        for (int i = 2; --i > 0;)
        for (int f = 0; ++f < 8;)
            if (pawnsPerFile[i, f] - pawnsPerFile[i ^ 1, f] > 0)
                eval++;
        

       // int score = eval * (_testBoard.IsWhiteToMove ? 1 : -1);

       // Console.WriteLine($"Eval: {score}");
    }

    #endregion

    #region Hardware

    private int _hardwareNodes = 0;

    // https://github.com/SebLague/Chess-Challenge/issues/381
    [Test]
    public void Takes5SecondOnTournamentTest()
    {
        string fen = "r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16";
        _testBoard = Board.CreateBoardFromFEN(fen);

        _hardwareNodes = 0;
        
        _stopwatch.Restart();

        SearchFullForHardwareCheck(5);

        _stopwatch.Stop();
        
        Console.WriteLine(_hardwareNodes);

        Console.WriteLine(_stopwatch.ElapsedMilliseconds + " ms.");
    }

    private void SearchFullForHardwareCheck(int depthRemaining)
    {
        if (depthRemaining == 0) return;
        Span<Move> moves = stackalloc Move[218];
        _testBoard.GetLegalMovesNonAlloc(ref moves);
        foreach (var m in moves)
        {
            _hardwareNodes++;
            _testBoard.MakeMove(m);
            SearchFullForHardwareCheck(depthRemaining - 1);
            _testBoard.UndoMove(m);
        }
    }

    #endregion
    */
}