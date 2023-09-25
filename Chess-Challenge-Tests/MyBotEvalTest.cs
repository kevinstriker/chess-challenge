using System.Diagnostics;
using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

namespace Chess_Challenge_Tests;

public class MyBotEvalTest
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
    
    [Test]
    public void Eval()
    {
        _testBoard = Board.CreateBoardFromFEN("8/8/8/4p3/8/3RP3/8/8 w - - 0 1");

        foreach (var i in Enumerable.Range(0, 5))
        {
            
        }
        
        
        double eval = Enumerable.Range(0, 64)
            .Sum(squareIndex =>
            {
                var piece = _testBoard.GetPiece(new Square(squareIndex));
                return 10 * Math.Pow(2, (int)piece.PieceType) * (piece.IsWhite ? 1 : -1);
            }) - _testBoard.GetLegalMoves().Length;
        
        Console.WriteLine($"Eval: {eval}");
    }
    
}