using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    public class Coach
    {
        // hotspots for perf:
        //  #1: MCTS searching for every move in the iteration games.
        //  #2: MCTS searching for every move in the self play games.
        //    storage for visitcounts, action probs, value, Q, etc. Tons of lookups with state or state+a as key.

        public void Run(IGame game, ISkynet skynet)
        {
            // episodes()
            // train()
            // selfPlay()

            // add output training examples from all episodes to global training set.
            // train skynet on global training set.
            // run self play to accept or reject new model.
        }

        private void Episodes()
        {
            // run x episodes with each their own MCTS. In parallel!

            // game loop until win/loss/draw. See OneVsOne.
            //   ask mcts for action probs.
            //   store state, action probs, playerId for each move, including symmetries for [state, action probs].

            // return episodes result (state, action probs, result) where playerId is converted to 1 or -1 for win/loss.
        }

        private void Train()
        {
            // train skynet on global training set.
        }

        private void SelfPlay()
        {
            // play x time against previous model. In parallel!
            // return result (win/loss/draw).
        }
    }
}
