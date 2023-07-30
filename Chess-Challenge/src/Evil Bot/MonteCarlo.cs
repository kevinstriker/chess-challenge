using ChessChallenge.API;
using System.Numerics;
using System;

/*
          .         .                                                     
         ,8.       ,8.           ,o888888o.8888888 8888888888 d888888o.   
        ,888.     ,888.         8888     `88.    8 8888     .`8888:' `88. 
       .`8888.   .`8888.     ,8 8888       `8.   8 8888     8.`8888.   Y8 
      ,8.`8888. ,8.`8888.    88 8888             8 8888     `8.`8888.     
     ,8'8.`8888,8^8.`8888.   88 8888             8 8888      `8.`8888.    
    ,8' `8.`8888' `8.`8888.  88 8888             8 8888       `8.`8888.   
   ,8'   `8.`88'   `8.`8888. 88 8888             8 8888        `8.`8888.  
  ,8'     `8.`'     `8.`8888.`8 8888       .8'   8 8888    8b   `8.`8888. 
 ,8'       `8        `8.`8888.  8888     ,88'    8 8888    `8b.  ;8.`8888 
,8'         `         `8.`8888.  `8888888P'      8 8888     `Y8888P ,88P' 

                        Monte Carlo Tree Search
 
Here is a basic implementation of MCTS coming in just under 500 tokens when 
including my evaluation function. There are ways to shrink this further but 
I've opted to keep it more readable for the didactic purposes of this guide.

To that end this is just the basic implementation of the algorithm I've left 
several footnotes at the end on improving MCTS performance on chess from my 
experience building MCTS based engines, as well as an FAQ.

Just to be clear if you wan't the absolute strongest engine possible for this
challenge look elsewhere. AlphaBeta pruning based engines will easily outperform 
MCTS pound for pound, and they are much easier to write.

Still do not underestimate MCTS when applied well it is no slouch either.

I suggest if you are unfamiliar with MCTS:
 > Wikipedia: https://en.wikipedia.org/wiki/Monte_Carlo_tree_search
 > Interactive Demo: https://vgarciasc.github.io/mcts-viz/
 */
public class MonteCarlo : IChessBot
{
    struct TreeNode
    {
        public float Visits;
        public float Value;
        public TreeNode[] Children;
        public Move[] Moves;
    }

    Board board;

    public Move Think(Board startBoard, Timer timer)
    {
        board = startBoard;

        TreeNode root = new TreeNode { };

        // One benefit of growing a game tree in memory you have access to very granular time control
        while (timer.MillisecondsElapsedThisTurn < 1000) // while(root.Visits < 100000) also works
            iteration(ref root);

        // Following code could be reduced considerably,
        // After the search it plays which move/child averaged the best and returns it.
        float bestAvg = -1;
        Move bestMove = root.Moves[0];

        for (int i = 0; i < root.Moves.Length; i++)
        {
            TreeNode child = root.Children[i];
            float avg = -child.Value / child.Visits;
            if (avg > bestAvg)
            {
                bestAvg = avg;
                bestMove = root.Moves[i];
            }
            // Console.WriteLine("{0}, Score: {1}, Visits: {2}", root.Moves[i], avg, child.Visits);
        }

        //Console.WriteLine(bestMove);
        return bestMove;
    }

    // Iteration() handles all the core steps of the MCTS algorithm
    float iteration(ref TreeNode node)
    {
        // If we have reached a leaf node, we enter the EXPANSION step base case
        if (node.Visits == 0)
        {
            node.Visits = 1;
            node.Value = evaluation();
            return node.Value;
        }

        // Most leaf nodes will not be revisited, so only call expensive movegen on revisit
        if (node.Visits == 1)
        {
            node.Moves = board.GetLegalMoves();
            node.Children =
                new TreeNode[node.Moves
                    .Length]; // You can save some memory by allocating this is as dynamically growing list, but it complicates other parts
        }

        // It can also be a leaf node because it is a terminal node, i.e. checkmate
        if (node.Moves.Length == 0)
            return node.Value / node.Visits;

        // Otherwise proceed to SELECTION step, we compute UCT for all children and select maximizing node
        // For more on UCT formula see [2] for details
        float part = 1.41f * MathF.Log(node.Visits);
        float bestUCT = -1;

        // We store index because we're gonna look up child and its respective move, theres probably some way to save quite a few tokens
        int bestChildIdx = 0;

        for (int i = 0; i < node.Moves.Length; i++)
        {
            TreeNode child = node.Children[i];

            float uct;
            // Avoid division by 0, further important discussion on FPU in footnotes
            if (child.Visits == 0)
                uct = 1000f;
            else
                uct = (-child.Value / child.Visits) + MathF.Sqrt(part / child.Visits);

            if (uct >= bestUCT)
            {
                bestUCT = uct;
                bestChildIdx = i;
            }
        }

        // Move a level down the tree to our chosen node, and BACKPROPOGATE when it returns an evaluation up the tree
        Move exploreMove = node.Moves[bestChildIdx];
        board.MakeMove(exploreMove);
        float eval = -iteration(ref node.Children[bestChildIdx]);
        node.Value += eval;
        node.Visits++;
        board.UndoMove(exploreMove);

        return eval;
        // Thats it. Thats MCTS. https://www.youtube.com/watch?v=T1XgFsitnQw
    }


    // In this version I replace random rollout/simulations with a static eval.
    // Nothing super fancy just material values and punishing pieces on outermost ring of board
    float evaluation()
    {
        if (board.IsInsufficientMaterial()) return 0f;
        if (board.IsInCheckmate()) return -1f;

        var pieceWeights = new int[] { 100, 280, 320, 500, 900 };
        var pieceTypes = new PieceType[]
        {
            PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen
        };
        ulong bordermagic = 18411139144890810879;

        int score = 0;

        for (int i = 0; i < 5; i++)
        {
            ulong whitePieces = board.GetPieceBitboard(pieceTypes[i], true);
            ulong blackPieces = board.GetPieceBitboard(pieceTypes[i], false);
            score += pieceWeights[i] * (BitOperations.PopCount(whitePieces) - BitOperations.PopCount(blackPieces));
            score -= 15 * (BitOperations.PopCount(whitePieces & bordermagic) -
                           BitOperations.PopCount(blackPieces & bordermagic));
        }

        if (!board.IsWhiteToMove)
            score = -score;

        // Compress traditional centipawn eval to (-1,1) probability scale.
        return 0.9f * MathF.Tanh(((float)score) / 250);
    }
}

/*
FOOTNOTES:

Q: Why aren't we using a dictionary to map moves to children?

A: Although I haven't benchmarked C#'s implementation dictionaries tend to be very slow to iterate over, 
which happens frequently during SELECTION steps, it also requires copying the move array into the keys of the dictionary.


Q: What is the 1.41f in float part = 1.41f * MathF.Log(node.Visits);?
   Why isn't this included with the rest of the UCT calculation?

A: It is the exploration constant theoretically equal to sqrt(2) essentially, it controls the exploration vs exploitation 
of the tree search, essentially how deep vs wide you search your tree. We exlcude this because Logarithm is extremely
expensive to compute, so we want it outside the loop, it also saves a multiply with constant.


Q: Why is the -child.Value/child.Visits in UCT formula negative?

A: For simplicity I keep the value at the node from the side to move's perspective. 
Same logic as megamax, whats good for your opponent is bad for you and vice versa. 
You can keep it from the node above side to moves perspective but that just confuses me personally.


Q: I thought MCTS used rollouts (random simulations of games)?

A: Traditionally it does hence the Monte Carlo in the name, but chess has very strong heuristics for the value of a position,
(material sum) and can be assessed more accurately and quickly by a static evaluation function than random playouts. 
Another issue with truly random playouts in chess is they almost always will fail to checkmate and produce a draw. 
You can still use rollouts though you have to be a bit smarter with it, and have a simple ai or policy function guiding it.


Q: What is the meaning of life?

A: 42


Q: What does this in the evaluation function do 0.9f * MathF.Tanh(((float)score) / 250); and why * 0.9f?

A: Essentially we need to convert a traditional centipawn score to something similar to a probability of a win 
just on a scale (-1,1). For this we can use Tanh or similar sigmoid like functions. Graph tanh(x) on desmos
for a visual representation of what it does. The 0.9 is actually very helpful in guiding MCTS to checkmate.
The issue otherwise is it sees its up massive material the evaluation is nearly 1, and it will have no incentive
to checkmate instead of just staying a queen up forever, so we scale material to (-0.9,0.9), only letting it get 
a 1 score from checkmate paths, guiding it towards them.


Q: You're using a scale of (-1,1) on wikipedia and other sites it only shows mcts values from (0,1) what gives?

A: I don't know why this discrepancy exists, every version I've seen for zero sum alternating games, implements it
on a scale of (-1,1) which is clearly better since you don't have to do 1-(node.Value/node.Visits) constantly.
Its far simpler on a scale of (-1,1), you can see the same scheme in the interactive tic-tac-toe demo link.


Q: How can I make it play even better?

A: Here I'm going to list a lot of different things you can implement...

First you have to understand why it is playing poorly. The main reason regular MCTS struggles in chess
is because you often have tactical positions where you have thirty total moves but only a couple are good,
for example recapturing after your opponent takes a piece. The problem is since UCT gives infinite priority towards 
unexplored (Visits=0) leaf nodes so you will end up with one true evaluation drowned out by thirty or so bad ones,
so mcts will likely conclude that the line is losing, before it fully explores it. One solution to this is to tune
"first play urgency"(FPU) essentially what the UCT score of unexplored leaf nodes. You can play around with this by
changing the following portion of UCT to a much lower value.

if (child.Visits == 0)
    uct = 1000f;

By lowering this if it finds a really good move, it may explore it again next time instead of another unexplored child, which
will help it converge to the correct evaluation faster. Lowering this value won't immediately make it play better 
as if it tries the bad moves before the good one, then it will still drown out the correct evaluation. 

Thus it comes time to discuss policy functions, which are critical to strong MCTS play. 

Policy is essentially a secondary modifier of how much you want to explore certain moves/children.Essentially in your Node struct 
you will also store policies corresponding to each move/child. The idea being that you can use these to bias the early search 
and prioritize clearly better moves, to be explored first and more.

A simple policy could be something similar to MVV/LVA which will make it play considerably better in tactical positions.
There are a few ways you can add this to UCT, but you need it to decrease with number of visits, so over time you trust
the searched evaluation of the position more and the policy less. Tuning Policy and FPU are a must for strong tactical play.

Fun fact Leela uses part of its deep neural network for evaluation of position and another part for generating policy for moves, 
this helps give it such strength, that MCTS can rival stockfish.

Another common optimization is adding Quiesence search to evaluations. This leads to less noisy evaluations,
and has the perk that you spend more time evaluating at the leaves instead of bouncing around the tree. Since you
evaluate less positions but more accurately it will lead to a memory savings as well. The main downside is unlike AB
we do not have a pre-existing alpha beta window from the main search to pass in which could lead to slightly more pruning.
Komodo's MCTS version used shallow AB searches essentially qsearch at the leaves.

AMAF/RAVE are essentially a way to bias the tree search using something similar to the history heuristic in alpha-beta engines which can help it
find good moves faster, and supplement policy.

Another small optimization is that since you are keeping a game tree in memory you can actually just recycle 
the subtree of it for the next move that gets played essentially giving you a head start on evaluation.

You can also look into different time management systems, since you can stop on a dime. For example you could give it more time
in tricky positions, and for ones with a clear best move cut them off short.

Implicit minmax backups are another possible way to improve further tactical awareness into MCTS, essentially in backpropogation
you also backup the minmax value of the tree, which can even replace the child.Value/child.Visits in UCT. (Note if you try this
make sure to use quiesence search so you can trust evaluations from the leaves.)

A small performance optimization I decided to omit, is storing a utility score i.e. node.Value/node.Visits in the struct
and recalculating it during backpropogation when it changes. The idea is in UCT most of the nodes we check
will not have their UCT updated since we don't select them, so we are wasting divisions by constantly
recomputing node.Value/node.Visits, when it can be precomputed in exchange for 4 bytes of each structs memory.
There are some similar things like this that you can do at different points.

Theres probably some other minor stuff that I'm forgetting when writing this but anyway hope this helped some of you :D
*/