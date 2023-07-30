using System;
using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Debug
    private PrincipleLine[] _principleLines = new PrincipleLine[128];

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
            key = _key; score = _score; depth = _depth; flag = _flag; move = _move;
        }
    }
    // TT Definition
    Entry[] _tt;
    
    // Constructor to initialise some required stuff
    public MyBot()
    {
        _tt = new Entry[TT_ENTRIES];
    }
    
    public Move Think(Board boardInput, Timer timerInput)
    {
        // Debug
        for (int i = 0; i < 128; i++) _principleLines[i] = new PrincipleLine();
        
        // Globalise so we're able to save tokens (parameter passing takes more space)
        _board = boardInput;
        _timer = timerInput;
        
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

            _timeLimit = 5000;
            
            // Search "best line of play" with Negamax alpha beta algorithm
            int score = Search(depth, 0, -Checkmate, Checkmate);

            // If we're breaking out of our search do not use this depth since it was incomplete 
            if (_timer.MillisecondsElapsedThisTurn > _timeLimit) 
                break;
            
            // We reached here so our current depth move is our best / most informed move
            _bestMove = _depthMove;
            
            DebugHelper.LogDepth(depth, score, _nodes, 0, _timer, _principleLines, _bestMove);
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
        
        // Prevent QSearch adding to PL
        _principleLines[ply].Length = 0;

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
            if (!root && ttEntry.depth >= depth && (
                    (ttEntry.flag == ALPHA_FLAG && ttEntry.score <= alpha) ||
                    ttEntry.flag == EXACT_FLAG ||
                    (ttEntry.flag == BETA_FLAG && ttEntry.score >= beta)
                )
               )
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
                
                // Debug
                DebugHelper.UpdatePrincipleLine(ref _principleLines, ply, move);
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
        if(move == ttMove)
        {
            return 1000;
        }
        
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
                ulong bb =_board.GetPieceBitboard((PieceType)pieceType, colorIndex != 0);
                while (bb > 0)
                {
                    Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (colorIndex != 0 ? 0 : 56));
                    score[colorIndex] += _pieceValue[pieceType];
                }
            }
        }
        
        // Calculate the score for the "maximising" player and comparing it to the opponent perspective ^ 1 (XOR)
        int perspective = Convert.ToInt32(_board.IsWhiteToMove);
        return score[perspective] - score[perspective ^ 1];
    }
    
}