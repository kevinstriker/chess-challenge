using ChessChallenge.API;
using System;
using System.Collections.Generic;


/// <summary>
/// The one version before my current version
/// </summary>
public class MinusOneBot : IChessBot
{
    // Constants
    private const int Checkmate = 100000;
    private const int TimeLimit = 1000;
    
    // Globals to save tokens
    private Board _board;
    private Timer _timer;
    private Int64 _nodes;
    private Int64 _qnodes;
    
    // Eval
    private Move _bestMove;
    private Move _depthMove;
    private int[] _pieceValue = { 0, 100, 300, 300, 500, 900, 0 };

    public Move Think(Board boardInput, Timer timerInput)
    {
        // Globalise so we're able to save tokens (parameter passing takes more space)
        _board = boardInput;
        _timer = timerInput;
        
        _bestMove = Move.NullMove;
        
        for (int depth = 1; depth < 100; depth++)
        {
            _nodes = 0;
            _qnodes = 0;
            
            // Negamax alpha beta algorithm to search the best move available
            int score = Search(depth, 0, -Checkmate, Checkmate);

            // If we're breaking out of our search do not use this depth since it was incomplete 
            if (_timer.MillisecondsElapsedThisTurn > TimeLimit) 
                break;
            
            // We reached here so our current depth move is our best / most informed move
            _bestMove = _depthMove;
        }
        Console.WriteLine();
        
        return _bestMove.IsNull ? _board.GetLegalMoves()[0] : _bestMove;
    }
    
    private int Search(int depth, int ply, int alpha, int beta)
    { 
        if (_board.IsDraw()) return 0;                                  
        if (_board.IsInCheckmate()) return -Checkmate + ply;
        
        if (depth <= 0) return Eval();                                  
        
        Move[] moves = _board.GetLegalMoves();
        
        // Sorting moves will help making alpha beta pruning more effective since good/best moves are calculated first
        List<Tuple<Move, int>> scoredMoves = new();
        foreach (Move move in moves) scoredMoves.Add(new Tuple<Move, int>(move, MoveScore(move)));
        scoredMoves.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        
        foreach (var (move, _) in scoredMoves)
        {
            _nodes++;
            
            _board.MakeMove(move);
            int evaluation = -Search(depth - 1, ply + 1, -beta, -alpha);
            _board.UndoMove(move);
            
            if (evaluation >= beta) return beta;
            
            if (evaluation > alpha)
            {
                if (ply == 0) _depthMove = move;
                alpha = evaluation;
            }
        }
        
        return alpha;
    }
    
    // Score moves using TT and MVV-LVA
    private int MoveScore(Move move)
    {
        // TT-Move TODO: implement TT
        
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