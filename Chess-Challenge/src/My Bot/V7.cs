#define X

using ChessChallenge.API;
using System;
using System.Linq;

/*
 * So here we go, 8 august, let's start from scratch, let's name this project NebulaAI
 * V1: NegaMax, Q Search, Move ordering, Piece Square Tables and Transposition Tables
 * V2: Null move pruning, History heuristics
 * V3: Reversed futility pruning, futility pruning and from NegaMax to PVS
 * V4: Killer moves, check extensions, razoring, time management
 * V5: Late move reduction, token savings
 * V6: Span<Move> for moves collection, allowNull to prevent double pruning, two killers,
 *     more effective alpha beta update and beta cutoff, token optimising
 * V7: Improvements to pruning, new time management (soft/hard limit)
 * V8: Token improvements, Doubled Pawns, Tuned PST
 */
public class V7 : IChessBot
{
#if X
    public int Nodes;
    public int QNodes;
#endif

    // The next move (at root depth) to play based on our search
    public Move RootMove;

    // Transposition Table: keep track of positions that were already calculated and possibly re-use information
    // Token optimised: Key, Score, Depth, Flag, Move
    private readonly (ulong, Move, int, int, int)[] _tt = new (ulong, Move, int, int, int)[0x400000];
    
    // Killer moves: keep track on great moves that caused a cutoff to retry them based on a lookup by depth
    private readonly Move[] _killers = new Move[2048];

    // Piece values for tapered eval
    private static readonly int[] PieceValues =
        {
            77, 302, 310, 434, 890, 0,
            109, 331, 335, 594, 1116, 0
        },
        // Score each legal move for move ordering
        MoveScores = new int[218],

    // The unpacked piece square lookup table with the piece values baked in
    Pst = new[] {
            59445390105436474986072674560m, 70290677894333901267150682880m, 71539517137735599738519086336m, 78957476706409475571971323392m, 76477941479143404670656189696m, 78020492916263816717520067072m, 77059410983631195892660944640m, 61307098105356489251813834752m,
            77373759864583735626648317994m, 3437103645554060776222818613m, 5013542988189698109836108074m, 2865258213628105516468149820m, 5661498819074815745865228343m, 8414185094009835055136457260m, 7780689186187929908113377023m, 2486769613674807657298071274m,
            934589548775805732457284597m, 4354645360213341838043912961m, 8408178448912173986754536726m, 9647317858599793704577609753m, 9972476475626052485400971547m, 9023455558428990305557695533m, 9302688995903440861301845277m, 4030554014361651745759368192m,
            78006037809249804099646260205m, 5608292212701744542498884606m, 9021118043939758059554412800m, 11825811962956083217393723906m, 11837863313235587677091076880m, 11207998775238414808093699594m, 9337766883211775102593666830m, 4676129865778184699670239740m,
            75532551896838498151443462373m, 3131203134016898079077499641m, 8090231125077317934436125943m, 11205623443703685966919568899m, 11509049675918088175762150403m, 9025911301112313205746176509m, 6534267870125294841726636036m, 3120251651824756925472439792m,
            74280085839011331528989207781m, 324048954150360030097570806m, 4681017700776466875968718582m, 7150867317927305549636569078m, 7155688890998399537110584833m, 5600986637454890754120354040m, 1563108101768245091211217423m, 78303310575846526174794479097m,
            70256775951642154667751105509m, 76139418398446961904222530552m, 78919952506429230065925355250m, 2485617727604605227028709358m, 3105768375617668305352130555m, 1225874429600076432248013062m, 76410151742261424234463229975m, 72367527118297610444645922550m,
            64062225663112462441888793856m, 67159522168020586196575185664m, 71185268483909686702087266048m, 75814236297773358797609495296m, 69944882517184684696171572480m, 74895414840161820695659345152m, 69305332238573146615004392448m, 63422661310571918454614119936m,
        }.SelectMany(packedTable =>
        decimal.GetBits(packedTable).SelectMany(BitConverter.GetBytes)
                    .Select((square, index) => (int)((sbyte)square * 1.461) + PieceValues[index % 12])
                .ToArray()
        ).ToArray();
    
    public Move Think(Board board, Timer timer)
    {
#if X
        Nodes = 0;
        QNodes = 0;
#endif
        // History Heuristics: keep track on great moves that caused a cutoff to retry them
        // Based on a lookup by color, piece type and target square
        var hh = new int[2, 7, 64];

        // 1/13th of our remaining time, split among all of the moves
        int searchMaxTime = timer.MillisecondsRemaining / 13,
            // Progressively increase search depth, starting from 1
            depth = 1, alpha = -1_000_000, beta = 1_000_000;

        // Iterative Deepening 
        for (;;)
        {
            int eval = Pvs(depth, alpha, beta, 0, true);

#if X
            DebugHelper.LogDepth(GetType().ToString(), timer, depth, eval, Nodes, QNodes, RootMove);
#endif

            // Out of time -> soft bound exceeded
            if (timer.MillisecondsElapsedThisTurn > searchMaxTime / 3)
                return RootMove;

            // Outside AW -> widen the bounds we missed
            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            // Inside AW, go to next depth
            else
            {
                alpha = eval - 17;
                beta = eval + 17;
                depth++;
            }
        }

        int Pvs(int depth, int alpha, int beta, int plyFromRoot, bool allowNull)
        {
            // Declare search variables
            bool inCheck = board.IsInCheck(),
                canFPrune = false,
                isRoot = plyFromRoot++ == 0,
                isNotPv = beta - alpha == 1;

            // Check for repetition since TT doesn't know that and we don't want draws when we can win
            if (!isRoot && board.IsRepeatedPosition()) return 0;

            // Try to find the board position in the tt
            ulong zobristKey = board.ZobristKey;
            var (entryKey, entryMove, entryScore, entryDepth, entryFlag) = _tt[zobristKey & 0x3FFFFF];

            // Define best eval all the way up here to generate the standing pattern for QSearch
            int bestEval = -100_000,
                ttFlag = 2,
                movesTried = 0,
                movesScored = 0,
                pvsEval;

            // Using local method to simplify multiple similar calls to the Pvs (to combine with Late move reduction)
            int Search(int newAlpha, int reduction = 1, bool canNull = true) =>
                pvsEval = -Pvs(depth - reduction, -newAlpha, -alpha, plyFromRoot, canNull);
            
            // Transposition table lookup, exception for shallower depths and or checkmates (not accurate ply level)
            if (entryKey == zobristKey && !isRoot && entryDepth >= depth && Math.Abs(entryScore) < 50000 && (
                    entryFlag == 1 ||
                    entryFlag == 2 && entryScore <= alpha ||
                    entryFlag == 3 && entryScore >= beta))
                return entryScore;
            
            // Check extensions
            if (inCheck)
                depth++;
            
            // Search quiescence position to prevent horizon effect
            bool inQSearch = depth <= 0;
            if (inQSearch)
            {
                // Determine if quiescence search should be continued
                bestEval = Evaluate();
                if (bestEval >= beta) return bestEval;
                alpha = Math.Max(alpha, bestEval);
            }
            // No pruning in QSearch, not when PV node and not when there is a check (unstable situation)
            else if (isNotPv && !inCheck)
            {
                // Reverse futility pruning
                int staticEval = Evaluate();

                // Reverse futility pruning: if our position is much better than beta, even if we start losing material every depth
                // we'd still be above beta, so cutoff since unlikely opponent will allow us this path
                if (depth <= 7 && staticEval - 74 * depth >= beta)
                    return staticEval;

                // Null move pruning on pre-frontier nodes and up
                if (depth >= 2 && staticEval >= beta && allowNull)
                {
                    board.ForceSkipTurn();
                    // depth - (1 + Reduction), using the classic 2 for reduction
                    Search(beta, 3 + depth / 4 + Math.Min(6, (staticEval - beta) / 175), false);
                    board.UndoSkipTurn();

                    // Prune branch when the side who got a free move can't even improve
                    if (beta <= pvsEval) return pvsEval;
                }

                // Futility pruning: if our position is so bad that even if we improve a lot
                // and we can't improve alpha, so we'll give up on this branch
                canFPrune = depth <= 8 && staticEval + depth * 141 <= alpha;
            }

            // Generate appropriate moves only capture moves in q search unless in check
            Span<Move> moveSpan = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moveSpan, inQSearch && !inCheck);

            // Order moves in reverse order -> negative values are ordered higher hence the flipped values
            foreach (Move move in moveSpan)
                MoveScores[movesScored++] = -(
                    move == entryMove ? 9_000_000 :
                    move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    _killers[plyFromRoot] == move ? 900_000 :
                    hh[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

            MoveScores.AsSpan(0, moveSpan.Length).Sort(moveSpan);

            Move bestMove = entryMove;
            foreach (Move move in moveSpan)
            {
#if X
                if (depth > 0) Nodes++;
                else QNodes++;
#endif
                // Out of time -> hard bound exceeded
                if (depth > 2 && timer.MillisecondsElapsedThisTurn > searchMaxTime)
                    return 99_999;

                bool tactical = move.IsCapture || move.IsPromotion;
                
                // Futility pruning on non tactical nodes
                if (canFPrune && movesTried != 0 && !tactical)
                    continue;

                board.MakeMove(move);

                // PVS + LMR

                // Full search in Q search or on first move
                if (movesTried++ == 0 || inQSearch)
                    Search(beta);
                else
                {
                    // Late move reduction search 
                    if (movesTried >= 6 && depth > 1)
                        Search(alpha + 1, 2 + (isNotPv ? 1 : 0));
                    else
                        pvsEval = alpha + 1;

                    // Zero window search when reduced search improved alpha (or alpha hacked see above)
                    if (pvsEval > alpha && Search(alpha + 1) > alpha)
                        // Full search when lmr search and zw search are likely to improve alpha 
                        Search(beta);
                }

                board.UndoMove(move);

                // When the eval is better than our best so far, we'll update our best with the current eval
                if (pvsEval > bestEval)
                {
                    bestEval = pvsEval;

                    // If this moves even improve the alpha (best branch so far), we'll improve alpha and the best move 
                    if (pvsEval > alpha)
                    {
                        alpha = pvsEval;
                        bestMove = move;
                        ttFlag = 1;

                        // Update the root move
                        if (isRoot) RootMove = move;
                    }

                    // Beta cutoff, move is too good, opposing player has a better option (beta) and won't play this subtree
                    if (beta <= alpha)
                    {
                        // Update heuristics tables for scoring moves when this move was not a capture move
                        if (!move.IsCapture)
                        {
                            hh[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                            _killers[plyFromRoot] = move;
                        }

                        ttFlag = 3;
                        break;
                    }
                }
            }

            // Performant way to check for stalemate and checkmate
            // Basically when the score hasn't changed we know there was no move since any move is better than -100_000
            if (bestEval == -100_000) return inCheck ? plyFromRoot - 100_000 : 0;

            // Save position to transposition table
            _tt[zobristKey & 0x3FFFFF] = (
                zobristKey,
                bestMove,
                bestEval,
                depth,
                ttFlag);

            return bestEval;
        }

       int Evaluate()
        {
            // Variables re-used during evaluate
            int mg = 0, eg = 0, gamePhase = 0, sideToMove = 2, piece, square;
            
            // Loop the two sides that have to move (white and black)
            // Flip score for optimised token count (always white perspective due to double flip)
            // Eg. White eval = 2300 -> flip -> -2300 -> black eval = 2000 -> -300 -> flip -> 300 k)
            for (; --sideToMove >= 0; mg = -mg, eg = -eg)
                for (piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {
                        // The less pieces, the more we bend towards our endgame strategy
                        // Values for the pieces are 0 for pawn, 1 for bishop and knight, 2 for rook and 4 for queen
                        gamePhase += 0x00042110 >> piece * 4 & 0x0F;
                        
                        // A number between 0 to 63 that indicates which square the piece is on, flip for black
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                        
                        mg += Pst[square * 16 + piece];
                        eg += Pst[square * 16 + piece + 6];

                        // Bishop pair bonus
                        if (piece == 2 && mask != 0)
                        {
                            mg += 23;
                            eg += 62;
                        }

                        // Doubled pawns penalty
                        if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                        {
                            mg -= 15;
                            eg -= 15;
                        }

                        // Semi-open file bonus for rooks (+14.6 elo alone)
                        /*
                        if (piece == 3 && (0x101010101010101UL << (square & 7) & board.GetPieceBitboard(PieceType.Pawn, sideToMove > 0)) == 0)
                        {
                            middlegame += 13;
                            endgame += 10;
                        }
                        */

                        // Mobility bonus (+15 elo alone)
                        /*
                        if (piece >= 2 && piece <= 4)
                        {
                            int bonus = BitboardHelper.GetNumberOfSetBits(
                                BitboardHelper.GetPieceAttacks((PieceType)piece + 1, new Square(square ^ 56 * sideToMove), board, sideToMove > 0));
                            middlegame += bonus;
                            endgame += bonus * 2;
                        }
                        */
                    }
            return (mg * gamePhase + eg * (24 - gamePhase)) / (board.IsWhiteToMove ? 24 : -24)
            // Tempo bonus to help with aspiration windows
                + 16;
        }
    }
    
}