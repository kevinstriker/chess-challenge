using ChessChallenge.API;
using System;
using Stockfish.NET;

namespace ChessChallenge.Example
{
    public class StockFishBot : IChessBot
    {
        IStockfish mStockFish;

        public StockFishBot(int level)
        {
            Stockfish.NET.Models.Settings stockfishSettings = new Stockfish.NET.Models.Settings();
            stockfishSettings.SkillLevel = level;
            mStockFish = new Stockfish.NET.Stockfish(@"resources/stockfish/stockfish-11-64", 2, stockfishSettings);
        }

        public Move Think(Board board, Timer timer)
        {
            string fen = board.GetFenString();
            mStockFish.SetFenPosition(fen);

            string bestMove = mStockFish.GetBestMoveTime(timer.MillisecondsRemaining / 40);

            return new Move(bestMove, board);
        }
        
    }
}