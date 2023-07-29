using System.Linq;
using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Debug
    private PrincipleLine[] _principleLines = new PrincipleLine[128];

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
        _board = boardInput;
        _timer = timerInput;

        _bestMove = Move.NullMove;
        
        for (int i = 0; i < 128; i++) _principleLines[i] = new PrincipleLine();
        
        for (int depth = 1; depth < 100; depth++)
        {
            _nodes = 0;
            _qnodes = 0;
            int score = Negamax(depth, 0, -Checkmate, Checkmate);

            if (_timer.MillisecondsElapsedThisTurn > TimeLimit) 
                break;

            _bestMove = _depthMove;
            
            DebugHelper.LogDepth(depth, score, _nodes, 0, _timer, _principleLines, _bestMove);
        }
        Console.WriteLine();
        
        return _bestMove.IsNull ? _board.GetLegalMoves()[0] : _bestMove;
    }


    private int Negamax(int depth, int ply, int alpha, int beta)
    {
        _principleLines[ply].Length = 0; // To prevent q search adding more depth
        
        if (_timer.MillisecondsElapsedThisTurn > TimeLimit) return -Checkmate;
        if (_board.IsInCheckmate()) return -Checkmate + ply;
        if (_board.IsDraw()) return 0;
        if (depth <= 0) return Eval();

        Span<Move> moves = stackalloc Move[256];
        _board.GetLegalMovesNonAlloc(ref moves);
        
        foreach (Move move in moves)
        {
            _nodes++;
            
            _board.MakeMove(move);
            int evaluation = -Negamax(depth - 1, ply + 1, -beta, -alpha);
            _board.UndoMove(move);

            if (evaluation >= beta) return beta;

            if (evaluation > alpha)
            {
                if (ply == 0) _depthMove = move;
                alpha = evaluation;
                DebugHelper.UpdatePrincipleLine(ref _principleLines, ply, move);
            }
        }
        
        return alpha;
    }

    private int Eval()
    {
        int color = Convert.ToInt32(_board.IsWhiteToMove);
        int[] score = { 0, 0 };

        for (int pieceType = 1; pieceType <= 6; pieceType++)
        {
            ulong whiteBb = _board.GetPieceBitboard((PieceType)pieceType, true);
            ulong blackBb = _board.GetPieceBitboard((PieceType)pieceType, false);

            while (whiteBb > 0)
            {
                Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref whiteBb));
                score[1] += _pieceValue[pieceType];
            }

            while (blackBb > 0)
            {
                Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref blackBb) ^ 56);
                score[0] += _pieceValue[pieceType];
            }
        }

        // Calculate the current player score  comparing it to the opponent color ^ 1 (XOR)
        return score[color] - score[color ^ 1];
    }
    
}