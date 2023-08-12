# Without move ordering

Check mate in 2
> score 30000, time 22ms, nodes 23141, nps 1006130, DepthMove Knight-c3d5

Test dodge
> score -500, time 26ms, nodes 69715, nps 2582037, DepthMove Rook-a1b1

Pawn promo
> score 100, time 20ms, nodes 57110, nps 2719523, DepthMove Pawn-b2b1

Puzzle 1
> score 30000, time 2483ms, nodes 14370210, nps 597950, DepthMove Knight-d4e2

Puzzle 2
> score 100, time 30ms, nodes 198747, nps 6411193, DepthMove Queen-g5f5

Puzzle 3
> score 100, time 388ms, nodes 2393592, nps 4887854, DepthMove Rook-d8a8

# Bubble sort ordering
 
Checkmate in 2
> score 30000, time 14ms, nodes 1976, nps 131733, DepthMove Knight-c3d5

Test dodge pawn
> score -500, time 4ms, nodes 10175, nps 2035000, DepthMove Rook-a1b1
 
Test promo
> score 100, time 2ms, nodes 5471, nps 1823666, DepthMove Pawn-b2b1

Puzzle 1
> score 30000, time 2483ms, nodes 14370210, nps 597950, DepthMove Knight-d4e2

Puzzle 2
> score 100, time 1ms, nodes 3171, nps 1585500, DepthMove Queen-g5f5

Puzzle 3
> score 100, time 119ms, nodes 289130, nps 2409416, DepthMove Rook-d8a8


# With Array Sort 

Checkmate in 2
> score 30000, time 19ms, nodes 1536, nps 76800, DepthMove Knight-c3d5

Test dodge pawn
> score -500, time 11ms, nodes 13391, nps 1115916, DepthMove Rook-a1b1

Test move ordering
> score 1600, time 0ms, nodes 54, nps 54000, DepthMove Knight-f3g5
> 
Test promo
> score 100, time 15ms, nodes 13590, nps 849375, DepthMove Pawn-b2b1

Puzzle 1
> score 30000, time 137ms, nodes 190977, nps 1383891, DepthMove Knight-d4e2

Puzzle 2
> score 100, time 4ms, nodes 2700, nps 540000, DepthMove Queen-g5f5

Puzzle 3
> score 100, time 127ms, nodes 205132, nps 1602593, DepthMove Rook-d8a8

# Added TT 

Checkmate in 2
> score 30000, time 19ms, nodes 1424, nps 71200, DepthMove Knight-c3d5

Test dodge pawn
> score -500, time 11ms, nodes 12261, nps 1021750, DepthMove Rook-a1b1

Test move ordering
> score 1600, time 0ms, nodes 54, nps 54000, DepthMove Knight-f3g5

Test promo
> score 100, time 15ms, nodes 13182, nps 823875, DepthMove Pawn-b2b1

Puzzle 1
> score 30000, time 106ms, nodes 147430, nps 1377850, DepthMove Knight-d4e2

Puzzle 2
> score 100, time 4ms, nodes 2440, nps 488000, DepthMove Queen-g5f5

Puzzle 3
> score 100, time 104ms, nodes 160408, nps 1527695, DepthMove Rook-d8a8


# TT with my cutoff

Puzzle 4
> score -200, time 6204ms, nodes 12319984, nps -91042, DepthMove Queen-a5a2

Puzzle 5
> score -100, time 930ms, nodes 2422102, nps -2011670, DepthMove Pawn-g5g4

# TT with JW cutoff
Puzzle 4
> score -200, time 9014ms, nodes 16945018, nps -26051, DepthMove Queen-a5a2

Puzzle 5
> score -130, time 1018ms, nodes 2609904, nps -1653644, DepthMove Pawn-g5g4

