using System;
using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Global variables: To save tokens
    private Board _board;
    private Timer _timer;
    private Int64 _nodes;
    private Int64 _qnodes;
    private int _timeLimit;

    // Evaluate variables: Useful when determining which move to play
    private const int Checkmate = 100000;
    private Move _bestMove;
    private Move _depthMove;
    private int[] _pieceValue = { 0, 100, 310, 330, 500, 1000, 10000 };

    // Transposition Table; A lookup table of previous calculated positions and it's "best move"
    public record struct TtEntry(ulong Key, sbyte Depth, byte Flag, int Score, Move Move);
    public const ulong TtEntryCount = 0x8FFFFF;
    private TtEntry[] _tt = new TtEntry[TtEntryCount];
    
    private int Evaluate()
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

    // Ordering of moves
    private int MoveScore(Move move, Move ttMove)
    {
        // TT-Moves first since they were already calculated and considered "best" for that position
        if (move == ttMove)
            return 10000;

        // MVV-LVA capturing pieces with pieces of less value is generally speaking good so consider them first
        if (move.IsCapture)
            return 100 * (int)move.CapturePieceType - (int)move.MovePieceType;

        // Promotion moves are generally good 
        if (move.IsPromotion)
            return 10;

        return 0;
    }
    
    // Search the best move, a combination of Negamax algorithm, Q Search, TT and Move ordering
    private int Search(int depth, int ply, int alpha, int beta)
    {
        ulong key = _board.ZobristKey;
        int bestScore = -Checkmate;
        bool root = ply == 0;
        bool qSearch = depth <= 0;

        if (!root && _board.IsRepeatedPosition()) return 0;
        
        TtEntry ttEntry = _tt[key % TtEntryCount];
        
        if (!root && ttEntry.Key == key && ttEntry.Depth >= depth
            && (ttEntry.Flag == 3 
                || ttEntry.Flag == 2 && ttEntry.Score >= beta
                || ttEntry.Flag == 1 && ttEntry.Score <= alpha
            )) return ttEntry.Score;

        int eval = Evaluate();

        // Quiescence search here to save tokens
        if (qSearch)
        {
            bestScore = eval;
            if (bestScore >= beta) return beta;
            alpha = Math.Max(alpha, bestScore);
        }
        
        Move[] moves = _board.GetLegalMoves(qSearch);

        // Sorting moves makes alpha beta pruning more effective, good moves are calculated first
        List<Tuple<Move, int>> scoredMoves = new();
        foreach (Move move in moves) scoredMoves.Add(new Tuple<Move, int>(move, MoveScore(move, ttEntry.Move)));
        scoredMoves.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        Move bestMove = Move.NullMove;
        int startAlpha = alpha;

        foreach (var (move, _) in scoredMoves)
        {
            if (_timer.MillisecondsElapsedThisTurn > _timeLimit) return 0;

            if (root || !qSearch) _nodes++;
            else _qnodes++;
            
            _board.MakeMove(move);
            int newScore = -Search(depth - 1, ply + 1, -beta, -alpha);
            _board.UndoMove(move);

            if (newScore > bestScore)
            {
                bestScore = newScore;
                bestMove = move;
                // When in root depth we're using this move as our next move
                if (root) _depthMove = move;
                // Improve alpha
                alpha = Math.Max(bestScore, alpha);
                // Beta fail soft
                if (alpha >= beta) break;
            }
        }

        // (Check/Stale)mate
        if (!qSearch && moves.Length == 0) return _board.IsInCheck() ? -Checkmate + ply : 0;

        // Determine type of node cutoff
        int flag = bestScore >= beta ? 2 : bestScore > startAlpha ? 3 : 1;
        // Save position and best move to transposition table
        _tt[key % TtEntryCount] = new TtEntry(key, (sbyte)depth, (byte)flag, bestScore, bestMove);

        return bestScore;
    }

    public Move Think(Board boardInput, Timer timerInput)
    {
        _board = boardInput;
        _timer = timerInput;

        _bestMove = Move.NullMove;

        _timeLimit = Math.Min(1000, _timer.MillisecondsRemaining / 20);

        // Iterative deepening
        for (int depth = 1; depth < 50; depth++)
        {
            _nodes = 0;
            _qnodes = 0;

            // Negamax algorithm with Alpha Beta, Q Search and Transposition Tables
            int score = Search(depth, 0, -Checkmate, Checkmate);

            // If we're breaking out of our search do not use this depth since it was incomplete 
            if (_timer.MillisecondsElapsedThisTurn > _timeLimit)
                break;
            
            // Reached here so depth move is our best calculated move
            _bestMove = _depthMove;

            DebugHelper.LogDepth(_board, _timer, _tt, depth, score, _nodes, _qnodes);
        }

        Console.WriteLine();

        return _bestMove.IsNull ? _board.GetLegalMoves()[0] : _bestMove;
    }
}