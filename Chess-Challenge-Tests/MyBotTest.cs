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
        _bot = new MyBot
        {
            Timer = new Timer(60000000, 60000000, 0),
            TimeLimit = 1000000000,
            Hh = new int[2, 7, 64],
        };
        _stopwatch = Stopwatch.StartNew();
    }

    private void IterativePvs(int startDepth = 1, int maxDepth = 16)
    {
        // Iterative Deepening
        for (int depth = startDepth, alpha = -999999, beta = 999999, eval; depth <= maxDepth;)
        {
            eval = _bot.Pvs(depth, 0, alpha, beta, true);

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
    
    #region Mistakes in games

    [Test]
    // TODO: FIND OUT HOW TO FIX THIS EVAL
    public void TestAttackKingWithRook()
    {
        _testBoard = Board.CreateBoardFromFEN("rn3bk1/pp5p/2p5/3p3N/3P2p1/2P5/PP4PP/4RNK1 w - - 1 24");
        _bot.Board = _testBoard;

        IterativePvs();
        
        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("e1"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("e8"));
    }

    [Test]
    public void TestMoveBishopSafe()
    {
        _testBoard = Board.CreateBoardFromFEN("r1b2rk1/pp2ppbp/1q4B1/2n5/8/2P1QNN1/PP3PPP/R3K2R w KQ - 1 14");
        _bot.Board = _testBoard;

        IterativePvs(1, 16);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Bishop));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("g6"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("b1"));
    }

    #endregion


    #region Higher Depth

    [Test]
    public void TestGiveUpQueenToWinItBack()
    {
        _testBoard = Board.CreateBoardFromFEN("r2q1rk1/1pp1bppp/4bn2/pP6/n2Bp3/P3P1NP/2PN1PP1/2RQKB1R b K - 2 13");
        _bot.Board = _testBoard;

        IterativePvs(1, 16);
        
        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("d4"));
    }

    [Test]
    public void TestKingSafetyDoNotTakePawn()
    {
        // Do not take pawn with king since it will lose the queen much later
        _testBoard = Board.CreateBoardFromFEN("5rk1/2p2qp1/3b3p/8/2PQ4/3P3P/Pr1B1Pp1/3R1RK1 w - - 0 23");
        _bot.Board = _testBoard;
        
        IterativePvs(1, 16);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("f1"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("e1"));
    }

    [Test]
    public void TestTrapQueenWithTwoRooks()
    {
        _testBoard = Board.CreateBoardFromFEN("3r2r1/Q1pk1p2/2p2nqp/2Pp4/8/4PP1P/PP1B1RP1/R5K1 b - - 0 1");
        _bot.Board = _testBoard;

        IterativePvs(1, 16);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Rook));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("d8"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("a8"));
    }

    [Test]
    public void TestWinRookAndPawnForBishop()
    {
        _testBoard = Board.CreateBoardFromFEN("rn3r1k/pp3p1p/4bN2/q3Q3/1b1p3P/5N2/PPP2PP1/2KR1B1R b - - 2 14");
        _bot.Board = _testBoard;

        IterativePvs(1, 16);

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("a5"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("a2"));
    }

    [Test]
    public void TestPressureWithPawnForKnightForRook()
    {
        _testBoard = Board.CreateBoardFromFEN("8/5kp1/1N3p2/1p4p1/1P6/P5P1/R2n1PKP/3r4 b - - 14 39");
        _bot.Board = _testBoard;

        IterativePvs(1, 16);

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
        _bot.Board = _testBoard;

        IterativePvs(1, 16);

        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(20000));
    }

    #endregion

    #region Move ordering

    [Test]
    public void TestMoveOrdering()
    {
        _testBoard = Board.CreateBoardFromFEN("2b1kb1r/P1pppppp/4nn2/1N4q1/2B2B2/4PN1P/1PPPQPP1/R3K2R w KQk - 0 1");
        _bot.Board = _testBoard;

        int score = _bot.Pvs(1, 0, -100000, 100000, true);

        DebugHelper.LogDepth(_bot.Timer, 0, score, _bot);
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
            int score = _bot.Pvs(depth, 0, -100000, 100000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Queen));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("b6"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("b2"));
    }

    [Test]
    public void TestCheckMateIn2()
    {
        _testBoard = Board.CreateBoardFromFEN("rnbq4/pp1pkB2/3p2P1/6b1/3PP2Q/2N5/PPP1K1P1/r7 w - - 2 18");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 8; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("c3"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("d5"));
    }

    [Test]
    public void TestCheckMateByQueenSacrifice()
    {
        // Checkmate by queen sacrifice
        _testBoard = Board.CreateBoardFromFEN("2r2bk1/p5p1/1p1p2Qp/2PNp3/PR1nNr1q/3P4/5PPP/5RK1 b - - 0 1");
        _bot.Board = _testBoard;

        for (int depth = 1; depth <= 15; depth++)
        {
            int score = _bot.Pvs(depth, 0, -100000, 100000, true);
            DebugHelper.LogDepth(_bot.Timer, depth, score, _bot);
        }

        Assert.That(_bot.RootMove.MovePieceType, Is.EqualTo(PieceType.Knight));
        Assert.That(_bot.RootMove.StartSquare.Name, Is.EqualTo("d4"));
        Assert.That(_bot.RootMove.TargetSquare.Name, Is.EqualTo("e2"));
    }

    #endregion

    #region BitBoard tests

    [Test]
    public void TestEvalBitboardDoubledPawns()
    {
        // Checkmate by queen sacrifice
        _testBoard = Board.CreateBoardFromFEN("rnbqkbnr/ppppp1p1/4p2p/5P2/1P1P3P/PP1P2P1/8/RNBQKBNR b KQkq - 0 1");
        _bot.Board = _testBoard;


        int eval = 0, colors = 2;

        int[] materialValues = { 100, 300, 300, 500, 900, 0 };

        // Loop black and white colors of the game
        // Flip score for optimised token count (always white perspective due to double flip)
        // Eg. White eval = 2300 -> flip -> -2300 -> black eval = 2000 -> -300 -> flip -> 300 k)
        for (; --colors >= 0; eval = -eval)
        {
            var pawnsPerFile = new int[8];

            for (int piece = -1; ++piece < 6;)
            for (ulong mask = _testBoard.GetPieceBitboard((PieceType)piece + 1, colors > 0); mask != 0;)
            {
                // A number between 0 to 63 that indicates which square the piece is on, flip for black
                int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * colors;

                // Piece (material) values are baked into the PST (!)
                eval += materialValues[piece];

                // Keep track of doubled pawns
                if (piece == 0)
                    pawnsPerFile[squareIndex % 8]++;
            }

            // Doubles pawns penalty
            int count = pawnsPerFile.Count(c => c > 1);
            eval -= count * 50;
        }

        int score = eval * (_testBoard.IsWhiteToMove ? 1 : -1);


        Console.WriteLine(score);
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