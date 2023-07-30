//#define DEBUG_TIMER
//#define DEBUG_TREE_SEARCH

using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;

/// <summary>
/// Main bot class that does the thinking.
/// </summary>
public class AugsBot : IChessBot
{
    #region rTypes

    /// <summary>
    /// Transposition table entry. Stores best move and evaluation for a board.
    /// </summary>
    struct TEntry
    {
        public ulong mKey;
        public Move mBestMove;
        public int mDepth, mEval, mEvalType;

        public TEntry(ulong key, Move move, int depth, int eval, int evalType)
        {
            mKey = key;
            mBestMove = move;
            mDepth = depth;
            mEval = eval;
            mEvalType = evalType;
        }
    }

    #endregion rTypes
    
    #region rConstants

    UInt64[] kWhitePTables =
    {
        0x007B16BA4B8F7B00, 0x0021DC89C2B04F00, 0x000D1BCDF7871B00, 0xFF1BCE1560B0B5FF, 0xFFEB0304667E43FF,
        0xFF9DB385E7BF9EFF, 0xFFA28385E7BF9FFF, 0x007F7C7A18406000,
        0xCC717E504B0D3582, 0xA40B0F3DE35BDFC4, 0x5CE4794E75B97CB5, 0xC6D6C0A813C8C457, 0x2E4AB519820A1A00,
        0xF2E45A21838BDDFB, 0x4CC32101838BDFFE, 0x103CFEFE7C742000,
        0x4F6DDEF1FEEEB3B8, 0xB3BCC6B4C8BE2336, 0x5CDBADCF41C03476, 0x6E1C214AC8A199FC, 0xEE80FEFBD181D942,
        0xD72D81C3C18199BF, 0xFB8D81C3C18199FF, 0x00727E3C3E7E6600,
        0x9D4B043DAE0090E6, 0xE7570EC9BD121C03, 0xFF0AD305DF4ED2FA, 0x6DE3249C553D0767, 0xAA90296679715964,
        0x303C41C7FEFE5EA7, 0x200001C7FFFFDFE7, 0xDFFFFE3800002018,
        0x5CA995D33A2AA91B, 0x50814950D549736F, 0x45D71F7FB29951B9, 0xA6DA5F8C40A3991A, 0xE28E4CD0FDBEDB8D,
        0x133DAFDFFFBFDA7F, 0x031F0FDFFFBFDBFF, 0xFCE0F02000402400,
        0xEBEB060C97C8134D, 0x209E11F6922BCC92, 0x2C6328ED1FCBE231, 0x6B45FEC72857FB51, 0x707A2580BB18D71A,
        0x48BEBD7F46E7E731, 0x79FEBDFFFFFFFF39, 0x86014200000000C6,
        0x00EAC437994BD900, 0x00F46031FEC18E00, 0x00E2A5D986660E00, 0xFFCD292B421B6EFF, 0xFFABE538FEFFEEFF,
        0xFFD7DD38FEFFEEFF, 0xFFFF0238FEFFEEFF, 0x00FFFFC701001100,
        0xE6C81580C11D542F, 0x55FFFFC2C97F73A7, 0x2F6D1D413637584C, 0x4700CEDFC9D0A3B8, 0x829ABC4242369D42,
        0xBC7F7FC3C3F77E3D, 0x7FFFFFC3C3F7FFFF, 0x0000003C3C080000,
        0x926E99A434D9DE05, 0xB9B99A7BEA6F3F17, 0xC1FE382B798FC9ED, 0x43ACE5EA32AD53A0, 0x3C57FFEBF36E3C5A,
        0xFFFFFFEBF3EFFFFF, 0xFFFFFFEBF3EFFFFF, 0x000000140C100000,
        0x3F0617FD0839086A, 0x4AF0B0434D78B0ED, 0xC650EF4036AD8338, 0xC2E01FBB0FA20CE6, 0xC2F0FFFBFF5FFF5F,
        0xC2F0FFFBFFFFFFFF, 0xC2F0FFFBFFFFFFFF, 0x3D0F000400000000,
        0x58042874A146A228, 0xF97EDD811D5466C0, 0xE04FC62056FB7A12, 0x5EE59F0F9BD959CC, 0x4188BEA968D84098,
        0x4191875101DBBF77, 0x4181870101DBFFFF, 0xBE7E78FEFE240000,
        0x6F618D5C3F582DC6, 0x6F2519D9558B3463, 0x821B1300466AC172, 0xCE4165DE7951A4A6, 0x522061A1C2C2668B,
        0xDC010181C3C3E77C, 0xDF010181C3C3E7FF, 0x20FEFE7E3C3C1800
    };

    UInt64[] kBlackPTables;

    //                     .  P    K    B    R    Q    K
    int[] kPieceValues = { 0, 100, 300, 310, 500, 900, 10000 };
    int kMassiveNum = 99999999;
    const int kTTSize = 8333329;

    #endregion rConstants
    
    #region rDebug

#if DEBUG_TIMER
	int dNumMovesMade = 0;
	int dTotalMsElapsed = 0;
#endif
#if DEBUG_TREE_SEARCH
	int dNumPositionsEvaluated;
#endif

    #endregion rDebug
    
    #region rMembers

    int mDepth;
    Board mBoard;
    Move mBestMove;
    TEntry[] mTranspositionTable = new TEntry[kTTSize];

    #endregion rMembers
    
    #region rInitialise

    /// <summary>
    /// Create bot
    /// </summary>
    public AugsBot()
    {
        kBlackPTables = new UInt64[kWhitePTables.Length];
        for (int i = 0; i < kWhitePTables.Length; ++i)
            kBlackPTables[i] = BitConverter.ToUInt64(BitConverter.GetBytes(kWhitePTables[i]).Reverse().ToArray());
    }

    #endregion rInitialise
    
    #region rThinking

    /// <summary>
    /// Top level thinking function.
    /// </summary>
    public Move Think(Board board, Timer timer)
    {
        mDepth = 1;
        mBoard = board;

#if DEBUG_TREE_SEARCH
		dNumPositionsEvaluated = 0;
#endif
        int msRemain = timer.MillisecondsRemaining;
        if (msRemain < 200)
            return mBoard.GetLegalMoves()[0];
        while (timer.MillisecondsElapsedThisTurn < (msRemain / 200))
            EvaluateBoardNegaMax(++mDepth, -kMassiveNum, kMassiveNum, mBoard.IsWhiteToMove ? 1 : -1);


#if DEBUG_TIMER
		dNumMovesMade++;
		dTotalMsElapsed += timer.MillisecondsElapsedThisTurn;
		Console.WriteLine("My bot time average: {0}", (float)dTotalMsElapsed / dNumMovesMade);
#endif
#if DEBUG_TREE_SEARCH
		int msElapsed = timer.MillisecondsElapsedThisTurn;
		Console.WriteLine("Num positions evaluated {0} in {1}ms | Depth {2}", dNumPositionsEvaluated, msElapsed, mDepth);
#endif
        return mBestMove;
    }


    /// <summary>
    /// Recursive search of given board position.
    /// </summary>
    int EvaluateBoardNegaMax(int depth, int alpha, int beta, int color)
    {
        ulong boardKey = mBoard.ZobristKey;
        Move[] legalMoves = mBoard.GetLegalMoves();
        float alphaOrig = alpha;
        Move move, bestMove = Move.NullMove;
        int recordEval = int.MinValue;

        // Check for definite evaluations.
        if (mBoard.IsRepeatedPosition() || mBoard.IsInsufficientMaterial() || mBoard.FiftyMoveCounter >= 100)
            return 0;

        if (legalMoves.Length == 0)
            return mBoard.IsInCheck() ? -depth - 9999999 : 0;

        // Search transposition table for this board.
        TEntry entry = mTranspositionTable[boardKey % kTTSize];
        if (entry.mKey == boardKey && entry.mDepth >= depth)
        {
            if (entry.mEvalType == 0) return entry.mEval; // Exact
            else if (entry.mEvalType == 1) alpha = Math.Max(alpha, entry.mEval); // Lower bound
            else if (entry.mEvalType == 2) beta = Math.Min(beta, entry.mEval); // Upper bound
            if (alpha >= beta) return entry.mEval;
        }

        // Heuristic evaluation
        if (depth <= 0)
        {
#if DEBUG_TREE_SEARCH
			dNumPositionsEvaluated++;
#endif
            recordEval = color * (EvalColor(true) - EvalColor(false));
            if (recordEval >= beta || depth <= -4) return recordEval;
            alpha = Math.Max(alpha, recordEval);
        }

        // Sort Moves
        int[] moveScores = new int[legalMoves.Length];
        int[] moveIndices = new int[legalMoves.Length];
        for (int i = 0; i < legalMoves.Length; ++i)
        {
            move = legalMoves[i];
            moveIndices[i] = i;
            moveScores[i] = move == entry.mBestMove ? 1000000 :
                move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType :
                move.IsPromotion ? (int)move.PromotionPieceType : 0;
        }

        Array.Sort(moveIndices, (x, y) => { return moveScores[y] - moveScores[x]; });

        // Tree search
        for (int i = 0; i < legalMoves.Length; ++i)
        {
            move = legalMoves[moveIndices[i]];
            if (depth <= 0 && !move.IsCapture) continue; // Only search captures in qsearch
            mBoard.MakeMove(move);
            int evaluation = -EvaluateBoardNegaMax(depth - 1, -beta, -alpha, -color);
            mBoard.UndoMove(move);

            if (recordEval < evaluation)
            {
                recordEval = evaluation;
                bestMove = move;
                if (depth == mDepth)
                    mBestMove = move;
            }

            alpha = Math.Max(alpha, recordEval);
            if (alpha >= beta) break;
        }

        // Store in transposition table
        int ttEntryType = recordEval <= alphaOrig ? 2 :
            recordEval >= beta ? 1 : 0;
        mTranspositionTable[boardKey % kTTSize] = new TEntry(boardKey, bestMove, depth, recordEval,
            recordEval <= alphaOrig ? 2 :
            recordEval >= beta ? 1 :
            0);

        return recordEval;
    }


    /// <summary>
    /// Evaluate the board for a given color.
    /// </summary>
    int EvalColor(bool isWhite)
    {
        int phase = mBoard.PlyCount > 20 ? 48 : 0;
        UInt64[] PTable = isWhite ? kWhitePTables : kBlackPTables;
        int sum = 0;
        for (int i = 1; i < 7; ++i)
        {
            ulong pieceBitBoard = mBoard.GetPieceBitboard((PieceType)i, isWhite);
            sum += (kPieceValues[i] - 121) * BitOperations.PopCount(pieceBitBoard);
            for (int b = 0; b < 8; ++b)
                sum += BitOperations.PopCount(pieceBitBoard & PTable[(i - 1) * 8 + b + phase]) * (1 << b);
        }

        return sum;
    }

    #endregion rThinking
}