using ChessChallenge.API;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
      Move[] allMoves = board.GetLegalMoves();
      return allMoves[0];
    }
    
}