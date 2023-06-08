using System;
using System.Collections.Generic;

namespace AlphaSharp.Interfaces
{
    public interface IGame
    {
        /// <summary>
        /// Width of the game board
        /// </summary>
        int W { get; }

        /// <summary>
        /// Height of the game board.
        /// </summary>
        int H { get; }

        /// <summary>
        /// How many different actions the game can execute.
        /// </summary>
        int ActionCount { get; }

        /// <summary>
        /// How many bytes are needed to store the game state. NB: state is always stored as bytes.
        /// </summary>
        int StateSize { get; }

        /// <summary>
        /// Writes the game starting state to the given destination state of length StateSize.
        void GetStartingState(byte[] dstState);

        /// <summary>
        /// Writes the valid actions for the given state to the given destination array of length ActionCount. 1 for valid and 0 for not valid.
        /// </summary>
        void GetValidActions(byte[] state, byte[] dstValidActions);

        /// <summary>
        /// Given this state, has the game ended? 0 = no, 1 = player 1 won, -1 = player 2 won
        /// </summary>
        int GetGameEnded(byte[] state);

        /// <summary>
        /// Execute the given action on the given state. The state is modified in-place to the new state.
        /// </summary>
        void ExecutePlayerAction(byte[] state, int action);

        /// <summary>
        /// Flips the state to the next player. The game is always simulated from the perspective of player one.
        /// For tic-tac-toe the piece types could be reverted, o -> x and x -> o (no rotation required).
        /// For chess the board would be rotated 180 degrees and the pieces would be reverted in color.
        /// The state is modified in-place.
        /// </summary>
        /// <param name="state"></param>
        void FlipStateToNextPlayer(byte[] state);

        /// <summary>
        /// For faster training, we can use symmetries to augment the training data. If in doubt, just return the input state and probs.
        /// </summary>
        List<(byte[], float[])> GetStateSymmetries(byte[] state, float[] probs);

        /// <summary>
        /// Prints the game board as text, using the supplied print function. Can print multiple lines.
        /// </summary>
        void PrintState(byte[] state, Action<string> print);

        /// <summary>
        /// Prints a human readable text for the given action as text, using the supplied print function. Can print multiple lines.
        /// </summary>
        void PrintDisplayTextForAction(int action, Action<string> print);
    }
}
