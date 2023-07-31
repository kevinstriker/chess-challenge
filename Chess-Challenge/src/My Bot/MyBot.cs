#define DEBUGX

using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Constants
    private const int Checkmate = 100000;

    // Globals to save tokens
    private Board _board;
    private Timer _timer;
    private Int64 _nodes;
    private Int64 _qnodes;
    private int _timeLimit;

    // Evaluate
    private Move _bestMove;
    private Move _depthMove;
    private int[] _pieceValue = { 0, 100, 300, 300, 500, 900, 0 };

    // Transposition table variables
    const ulong TT_ENTRIES = 0x8FFFFF;
    int ALPHA_FLAG = 0, EXACT_FLAG = 1, BETA_FLAG = 2;

    // One entry of the TT
    private struct Entry
    {
        public ulong key;
        public int score, depth, flag;
        public Move move;

        public Entry(ulong _key, int _score, int _depth, int _flag, Move _move)
        {
            key = _key;
            score = _score;
            depth = _depth;
            flag = _flag;
            move = _move;
        }
    }

    // TT Definition
    Entry[] _tt;

    // Constructor to initialise 
    public MyBot()
    {
        _tt = new Entry[TT_ENTRIES];
    }

    public Move Think(Board boardInput, Timer timerInput)
    {
        // Globalise so we're able to save tokens (parameter passing takes more space)
        _board = boardInput;
        _timer = timerInput;

        _timeLimit = 1000;

        IterativeDeepening();

        return _bestMove.IsNull ? _board.GetLegalMoves()[0] : _bestMove;
    }

    private void IterativeDeepening()
    {
        _bestMove = Move.NullMove;

        for (int depth = 1; depth < 100; depth++)
        {
            _nodes = 0;
            _qnodes = 0;

            // Search "best line of play" with Negamax alpha beta algorithm
            int score = Search(depth, 0, -Checkmate, Checkmate);

            // If we're breaking out of our search do not use this depth since it was incomplete 
            if (_timer.MillisecondsElapsedThisTurn > _timeLimit)
                break;

            // We reached here so our current depth move is our best / most informed move
            _bestMove = _depthMove;

            #if DEBUGX
            LogDepth(depth, score, _nodes, _qnodes, _timer);
            #endif
        }

        Console.WriteLine();
    }

    private int Search(int depth, int ply, int alpha, int beta)
    {
        // Define search variables
        bool root = ply == 0;
        ulong key = _board.ZobristKey;
        int bestScore = -Checkmate;
        int startAlpha = alpha;
        Move ttMove = Move.NullMove;

        // Decisive returns
        if (_timer.MillisecondsElapsedThisTurn > _timeLimit) return 0;
        if (_board.IsRepeatedPosition()) return -10;
        if (_board.IsDraw()) return 0;
        if (_board.IsInCheckmate()) return -Checkmate + ply;

        // TT Pruning
        Entry ttEntry = _tt[key % TT_ENTRIES];
        if (ttEntry.key == key)
        {
            ttMove = ttEntry.move;
            if (!root && ttEntry.depth >= depth && ((ttEntry.flag == ALPHA_FLAG && ttEntry.score <= alpha)
                                                    || ttEntry.flag == EXACT_FLAG
                                                    || (ttEntry.flag == BETA_FLAG && ttEntry.score >= beta)))
                return ttEntry.score;
        }

        if (depth <= 0) return Eval();

        Move[] moves = _board.GetLegalMoves();

        // Sorting moves will help making alpha beta pruning more effective since good/best moves are calculated first
        List<Tuple<Move, int>> scoredMoves = new();
        foreach (Move move in moves) scoredMoves.Add(new Tuple<Move, int>(move, MoveScore(move, ttMove)));
        scoredMoves.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        foreach (var (move, _) in scoredMoves)
        {
            _nodes++;

            _board.MakeMove(move);
            int newScore = -Search(depth - 1, ply + 1, -beta, -alpha);
            _board.UndoMove(move);

            if (newScore > bestScore)
            {
                bestScore = newScore;
                ttMove = move;

                // Update "depth move" which is our best move this iteration
                if (root) _depthMove = move;
                // Improve Alpha
                alpha = Math.Max(bestScore, alpha);
                // Beta fail soft
                if (alpha >= beta) break;
            }
        }

        // Determine type of node cutoff
        int flag = bestScore >= beta ? BETA_FLAG : bestScore > startAlpha ? EXACT_FLAG : ALPHA_FLAG;
        // Save position to transposition table
        _tt[key % TT_ENTRIES] = new Entry(key, bestScore, depth, flag, ttMove);

        return bestScore;
    }

    // Score moves using TT and MVV-LVA
    private int MoveScore(Move move, Move ttMove)
    {
        // TT-Move
        if (move == ttMove)
            return 10000;

        // MVV-LVA
        if (move.IsCapture)
            return 10 * ((int)move.CapturePieceType) - (int)move.MovePieceType;
        return 0;
    }

    private int Eval()
    {
        // First score is for black, second for white
        int[] score = { 0, 0 };
        // Loop both colors of the game to calculate their score
        for (int colorIndex = 0; colorIndex < 2; colorIndex++)
        {
            // Loop and calculate pieces position and value of the current color
            for (int pieceType = 1; pieceType <= 6; pieceType++)
            {
                ulong bb = _board.GetPieceBitboard((PieceType)pieceType, colorIndex != 0);
                while (bb > 0)
                {
                    Square sq = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (colorIndex != 0 ? 0 : 56));
                    score[colorIndex] += _pieceValue[pieceType];
                }
            }
        }

        // Calculate the score for the "maximising" player and comparing it to the opponent perspective ^ 1 (XOR)
        int perspective = Convert.ToInt32(_board.IsWhiteToMove);
        return score[perspective] - score[perspective ^ 1];
    }

#if DEBUGX
    private string GetPv(int depth)
    {
        ulong key = _board.ZobristKey;
        Entry ttEntry = _tt[key % TT_ENTRIES];

        if (ttEntry.key == key && depth > 0)
        {
            _board.MakeMove(ttEntry.move);
            string subLine = GetPv(depth - 1);
            _board.UndoMove(ttEntry.move);
            return $"{ttEntry.move} {subLine}";
        }

        return "";
    }
    
    private void LogDepth(int depth, int score, long nodes, long qnodes, Timer timer)
    {
        string timeString = "\x1b[37mtime\u001b[38;5;214m " + timer.MillisecondsElapsedThisTurn + "ms\x1b[37m\x1b[0m";
        timeString += string.Concat(Enumerable.Repeat(" ", 38 - timeString.Length));

        string depthString = "\x1b[1m\u001b[38;2;251;96;27mdepth " + (depth) + " ply\x1b[0m";
        depthString += string.Concat(Enumerable.Repeat(" ", 38 - depthString.Length));
        
        string bestEvalString = string.Format("\x1b[37meval\x1b[36m {0:0} \x1b[37m", score);
        bestEvalString += string.Concat(Enumerable.Repeat(" ", 27 - bestEvalString.Length));

        string nodesString = "\x1b[37mnodes\x1b[35m " + nodes + "\x1b[37m";
        nodesString += string.Concat(Enumerable.Repeat(" ", 29 - nodesString.Length));
        
        string qnodesString = "\x1b[37mqnodes\x1b[34m " + qnodes + "\x1b[37m";
        qnodesString += string.Concat(Enumerable.Repeat(" ", 32 - qnodesString.Length));

        string pvString = "\x1b[37mpv\x1b[33m " + GetPv(depth);

        Console.WriteLine(string.Join(" ",
            new string[]
            {
                depthString, timeString, bestEvalString, nodesString, qnodesString, pvString
            }));
    }
#endif
}