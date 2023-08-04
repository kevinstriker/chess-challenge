using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using System;

namespace ChessChallenge.UCI
{
    class UCIBot
    {
        IChessBot _bot;
        ChallengeController.PlayerType _type;
        Chess.Board _board;

        static readonly string DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

		public UCIBot(IChessBot bot, ChallengeController.PlayerType type)
        {
            this._bot = bot;
            this._type = type;
            _board = new Chess.Board();
        }

        void PositionCommand(string[] args)
        {
            int idx = Array.FindIndex(args, x => x == "moves");
            if (idx == -1)
            {
                if (args[1] == "startpos")
                {
                    _board.LoadStartPosition();
                }
                else
                {
                    _board.LoadPosition(String.Join(" ", args.AsSpan(1, args.Length - 1).ToArray()));
                }
            }
            else
            {
                if (args[1] == "startpos")
				{
					_board.LoadStartPosition();
				}
                else
				{
					_board.LoadPosition(String.Join(" ", args.AsSpan(1, idx - 1).ToArray()));
				}

                for (int i = idx + 1; i < args.Length; i++)
                {
                    // this is such a hack
                    API.Move move = new API.Move(args[i], new API.Board(_board));
                    _board.MakeMove(new Chess.Move(move.RawValue), false);
                }
            }

            string fen = FenUtility.CurrentFen(_board);
            Console.WriteLine(fen);
        }

        void GoCommand(string[] args)
        {
            int wtime = 0, btime = 0;
            API.Board apiBoard = new API.Board(_board);
            Console.WriteLine(FenUtility.CurrentFen(_board));
            Console.WriteLine(apiBoard.GetFenString());
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "wtime")
                {
                    wtime = Int32.Parse(args[i + 1]);
                }
                else if (args[i] == "btime")
                {
                    btime = Int32.Parse(args[i + 1]);
                }
            }
            if (!apiBoard.IsWhiteToMove)
            {
                (wtime, btime) = (btime, wtime);
            }
            Timer timer = new Timer(wtime, btime, 0);
            API.Move move = _bot.Think(apiBoard, timer);
            Console.WriteLine($"bestmove {move.ToString().Substring(7, move.ToString().Length - 8)}");
        }

        void ExecCommand(string line)
        {
            // default split by whitespace
            var tokens = line.Split();

            if (tokens.Length == 0)
                return;

            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine("id name Chess Challenge");
                    Console.WriteLine("id author AspectOfTheNoob, Sebastian Lague");
                    Console.WriteLine("uciok");
                    break;
                case "ucinewgame":
                    _bot = ChallengeController.CreateBot(_type);
                    break;
                case "position":
                    PositionCommand(tokens);
                    break;
                case "isready":
                    Console.WriteLine("readyok");
                    break;
                case "go":
                    GoCommand(tokens);
                    break;
            }
        }

        public void Run()
        {
            while (true)
            {
                string line = Console.ReadLine();

                if (line == "quit" || line == "exit")
                    return;
                ExecCommand(line);
            }
        }
    }
}