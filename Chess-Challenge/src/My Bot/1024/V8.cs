// #define DEBUG

using ChessChallenge.API;
using System;
using System.Linq;
using static System.Math;

public class V8 : IChessBot
{
    private readonly int[] _moveScores = new int[218],
        
    // This is how we access:
    // UnpackedPestoTables[piece * 64 + square + (384 if eg else 0)]
    // Massive props to Gonumen (discord: Gonumen#9433) for his psts compression
    // He multiplies a matrix of weights (bytes packed into decimals) with binary matricies (ulong bitboards) to approximate the psts
    _unpackedPsts = Enumerable.Range(0, 768).Select(i =>
        new [] {
            74878480285766975821042337339m,
            8700648716869973255219861779m,
            4656772980811869114983847931m,
            75183115554167900517826224848m,
            73952414959976139581166969602m,
            2180906920336172951100710675m,
            2500072854013919266670321142m,
            1523218125012308048420213777m,
            77992630937507186377960916227m,
            77382123212914747844380914953m,
            614153261965563190204825608m,
            77382123324173646619562804980m,
            76437956888462316255108465652m,
            1841194004755399023613773316m,
            315529546755515743324209420m,
            76134530787686861148734357764m,
            10649697330710992953859974410m
        }.Select((w, j) =>
            (sbyte)Buffer.GetByte(decimal.GetBits(w), i / 64) * (j == 16
                ? 9
                : (int)((new [] {
                    0x3d0000000000ffbfUL,
                    0x28fe7e10UL,
                    0x3d9dbfffff00fc30UL,
                    0xcf9f8381a1c7c0f1UL,
                    0xf7c101020b021f03UL,
                    0x51682187c4f7babUL,
                    0x5600001ad762bdfeUL,
                    0xbff7c30020662100UL,
                    0xa83ac099b1000019UL,
                    0x1c1e1c381b020f3UL,
                    0x4100c01bc5bf0f3cUL,
                    0xb111c14028889c1dUL,
                    0xf787861b4c0a7c9aUL,
                    0x6dfc12110be3ac70UL,
                    0xc4820080f3502a26UL,
                    0x1f0202c1001e001fUL
                }[j] >> i % 64) % 2))
        ).Sum()).ToArray();

    // 0x400000 -> ~# of entries to fill 256mb
    // Hash, Move, Score, Depth, Flag
    private readonly (ulong, Move, int, int, int)[] _tt = new (ulong, Move, int, int, int)[0x400000];

    private readonly Move[] _killers = new Move[2048];

    private Move _bestRootMove;

#if DEBUG
    long nodes;
#endif

    public Move Think(Board board, Timer timer)
    {
#if DEBUG
        Console.WriteLine();
        nodes = 0;
#endif
        // Reset history for new search
        var hh = new int[4096, 7];

        int searchMaxTime = timer.MillisecondsRemaining / 13,
            depth = 2, alpha = -999999, beta = 999999, eval;

        // Iterative deepening
        for (;;)
        {
            // delta += delta / 2;
            eval = Pvs(depth, alpha, beta, 0, true);

            // Out of time -> soft bound exceeded
            if (timer.MillisecondsElapsedThisTurn > searchMaxTime / 3)
                return _bestRootMove;

            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            else
            {
                // Set up new aspiration window
                alpha = eval - 17;
                beta = eval + 17;
                depth++;
            }
        }
        
        
        int Pvs(int depth, int alpha, int beta, int plyFromRoot, bool allowNull)
        {
#if DEBUG
            nodes++;
#endif
            bool inCheck = board.IsInCheck(),
                canFPrune = false,
                notRoot = plyFromRoot++ > 0,
                notPv = beta - alpha == 1;

            // Draw detection
            if (notRoot && board.IsRepeatedPosition())
                return 0;

            ulong zobristKey = board.ZobristKey;
            var (entryKey, entryMove, entryScore, entryDepth, entryFlag) = _tt[zobristKey & 0x3FFFFF];

            // Define best eval all the way up here to generate the standing pattern for QSearch
            int bestEval = -9999999,
                newTtFlag = 1,
                eval;
            
            if (entryKey == zobristKey && notRoot && entryDepth >= depth && Abs(entryScore) < 50000
                && entryFlag != 2 | entryScore >= beta
                && entryFlag != 1 | entryScore <= alpha)
                return entryScore;

            // entryScore -> movesTried, entryDepth -> movesScored
            entryScore = entryDepth = 0; // Lil token save
            
            // Local method to save tokens (credit to Tyrant? idk who thought of this but thank god they did)
            int Search(int newAlpha, int R = 1, bool canNull = true) => eval = -Pvs(depth - R, -newAlpha, -alpha, plyFromRoot, canNull);

            // Check extensions
            if (inCheck)
                depth++;

            
            // Declare qsearch status here to prevent dropping into qsearch while in check
            bool inQSearch = depth <= 0;
            if (inQSearch && !inCheck)
            {
                bestEval = Evaluate();
                if (bestEval >= beta)
                    return bestEval;
                alpha = Max(alpha, bestEval);
            }
            // No pruning in qsearch
            // If this node is NOT part of the PV and we're not in check
            else if (notPv && !inCheck)
            {
                // Reverse futility pruning
                int staticEval = Evaluate();

                // Fuse RFP and Extended FP because they both kinda got the same depth thing goin on
                if (depth <= 7)
                {
                    // Give ourselves a margin of 74 cp * depth.
                    // If we're up by more than that margin in material, there's no point in
                    // searching any further since our position is so good
                    if (staticEval - 74 * depth >= beta)
                        return staticEval;

                    // Extended futility pruning
                    // Can only prune when at lower depth and behind in evaluation by a large margin
                    canFPrune = staticEval + depth * 141 <= alpha;
                }

                // NULL move pruning
                if (depth >= 2 && staticEval >= beta && allowNull)
                {
                    board.ForceSkipTurn();
                    Search(beta, 3 + depth / 4 + Min(6, (staticEval - beta) / 175), false);
                    board.UndoSkipTurn();

                    // Failed high on the null move
                    if (eval >= beta)
                        return eval;
                }

                // Reduce depth if we're terribly behind in evaluation
                // TODO might add back it actually does pretty good
                if (staticEval < -1500)
                    depth--;
            }

            // Generate appropriate moves depending on whether we're in QSearch
            Span<Move> moveSpan = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moveSpan, inQSearch && !inCheck);
            // Order moves in reverse order -> negative values are ordered higher hence the flipped values
            foreach (Move move in moveSpan)
                _moveScores[entryDepth++] = -(
                    // Hash move
                    move == entryMove ? 9_000_000 :
                    // MVV-LVA
                    // TODO try this super janky see thing eh?
                    move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 50_000 + depth : 0) :
                    // Killers
                    _killers[plyFromRoot] == move ? 900_000 :
                    // History
                    hh[move.RawValue & 4095, (int)move.MovePieceType]); // TODO i think we can do more with history

            _moveScores.AsSpan(0, moveSpan.Length).Sort(moveSpan);
            
            // Move bestMove = default;
            foreach (Move move in moveSpan)
            {
                // Out of time -> hard bound exceeded
                // -> Return checkmate so that this move is ignored
                // but better than the worst eval so a move is still picked if no moves are looked at
                // -> Depth check is to disallow timeouts before the bot has finished one round of ID
                if (depth > 2 && timer.MillisecondsElapsedThisTurn > searchMaxTime)
                    return 99999;
                
                // TODO retest if only counting queen promotions as noisy actually gains
                bool notNoisy = !move.IsCapture && (int)move.PromotionPieceType <= 4;

                // Futility pruning
                if (canFPrune && entryScore != 0 && notNoisy)
                    continue;
                
                // Late move pruning
                // TODO try continue instead of break
                // if (notPV && entryScore > depth * 14 && depth < 5 && notNoisy) break;

                board.MakeMove(move);
                
                // 3fold LMR
                // first LMR depth reduction with a null window
                // if that's above alpha, then normal reduction with a null window
                // if that's above alpha, then normal search
                if (entryScore++ == 0 || inQSearch ||

                    // Otherwise, skip reduced search if conditions are not met
                    (entryScore < 6 || depth < 2 ||

                        // If reduction is applicable do a reduced search with a null window
                        // TODO try notNoisy ? 3 : 2
                        Search(alpha + 1, (notPv ? notNoisy ? 3 : 2 : 1) + entryScore / 13 + depth / 9) > alpha) &&

                        // If alpha was above threshold after reduced search, or didn't match reduction conditions,
                        // update eval with a search with a null window
                        alpha < Search(alpha + 1))

                    // We either raised alpha on the null window search, or haven't searched yet,
                    // -> research with no null window
                    Search(beta);

                board.UndoMove(move);

                // History penalties, if eval < alpha then give a lil penalty for quiet moves
                if (eval < alpha && !move.IsCapture)
                    hh[move.RawValue & 4095, (int)move.MovePieceType] -= depth * depth;

                if (eval > bestEval)
                {
                    bestEval = eval;
                    if (eval > alpha)
                    {
                        alpha = eval;
                        entryMove = move;
                        newTtFlag = 3;
                        // newTTFlag = 1;
                        
                        // new best move hip hip hooray
                        if (!notRoot)
                            _bestRootMove = move;
                    }

                    // AB cutoff
                    if (alpha >= beta)
                    {
                        // Update quiet move ordering
                        if (!move.IsCapture)
                        { 
                            hh[move.RawValue & 4095, (int)move.MovePieceType] += depth * depth; 
                            _killers[plyFromRoot] = move;
                        }
                        newTtFlag--;
                        break;
                    }
                }
            }

            // Gamestate, checkmate and draws
            // no moves were looked at and eval was unchanged
            // must not be in qsearch and have had no legal moves
            // a faster way of detecting checkmate and stalemate
            if (bestEval == -9999999)
                return inCheck ? plyFromRoot - 99999 : 0;

            // TT entry update
            _tt[zobristKey & 0x3FFFFF] = (
                zobristKey,
                entryMove,
                bestEval,
                depth,
                newTtFlag);

            return bestEval;
        }

        // TODO retune bc we got the new pawn bonus money emoji WE STILL NEED TO DO THIS AUHHJGHH
        // Why is tyrants eval so immaculate
        int Evaluate()
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
                for (piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {
                        // Gamephase, middlegame -> endgame
                        // Multiply, then shift, then mask out 4 bits for value (0-16)
                        gamephase += 0x00042110 >> piece * 4 & 0x0F;

                        // Material and square evaluation
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;

                        // middlegame += UnpackedPestoTables[square * 16 + piece];
                        // endgame += UnpackedPestoTables[square * 16 + piece + 6];
                        middlegame += _unpackedPsts[piece * 64 + square]; // Also credit to Gonumen for this
                        endgame += _unpackedPsts[piece * 64 + square + 384]; // And this!

                        // // Bishop pair bonus (+14.1 elo alone)
                        // if (piece == 2 && mask != 0)
                        // {
                        //     middlegame += 23;
                        //     endgame += 62;
                        // }
                        //
                        // // Doubled pawn penalty (TODO get elo bonus from this)
                        // if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                        // {
                        //     middlegame -= 15; // RETUNE THIS!
                        //     endgame -= 15;
                        // }

                        // Cheap mobility bonus
                        // TODO this seems to actually be losing elo so we need to test this out later
                        // middlegame += BitboardHelper.GetNumberOfSetBits(
                        //     BitboardHelper.GetPieceAttacks((PieceType)piece + 1, new Square(square), board,
                        //         sideToMove > 0)) / 2; 
                        // The / 2 will filter out most pawn moves and anyways we don't want to overvalue mobility
                        

                        // if (piece == 3 && (0x101010101010101UL << (square & 7) &
                        //                    board.GetPieceBitboard(PieceType.Pawn, sideToMove > 0)) == 0)
                        // {
                        //     middlegame += 13;
                        //     endgame += 10;
                        // }

                        // int bonus = BitboardHelper.GetNumberOfSetBits(
                        //     BitboardHelper.GetSliderAttacks((PieceType)piece + 1,
                        //         new Square(square ^ 56 * sideToMove), sideToMove > 0 ? board.BlackPiecesBitboard : board.WhitePiecesBitboard));
                        // middlegame += bonus;
                        // endgame += bonus * 2;
                    }
            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24)
            // Tempo bonus to help with aspiration windows
                + 16;
        }
    }
}