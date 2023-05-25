namespace AlphaSharp
{
    // use iteration instead of recursion
    internal class Mcts : IMcts
    {
        // Episode:
        // simulate game from scratch.


        // each episode gets its own mcts tree.
        // one episode:
        //  repeat X times: start from current game state, scan tree, update stats
        //  when a move is chosen KEEP the tree from the target state and discard the rest (mem usage I guess)

        // Parallel episodes: higher mem usage... easy.
        // Parallel scans in same tree: share MCTS, start from same root.. doesn't really make sense, does it? values will not be updated as they do now.

        // conclusion: parallel episodes. each episode gets its own tree and returns
    }
}
