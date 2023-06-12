using AlphaSharp;
using AlphaSharp.Interfaces;

namespace TicTacToeGame
{
    public class TicTacToe : IGame
    {
        public string Name => "tic-tac-toe";

        public int W => 3;
        public int H => 3;

        public int ActionCount => 9; // 9 cells to place in
        public int StateSize => 9; // 9 cells, 0 for empty, 1 for X, 2 for O

        public const byte PieceX = 1;
        public const byte PieceO = 2;

        private static readonly int[] d1 = new[] { 2, 4, 6 };
        private static readonly int[] d2 = new[] { 0, 4, 8 };
        private static readonly int[] c1 = new[] { 0, 3, 6 };
        private static readonly int[] c2 = new[] { 1, 4, 7 };
        private static readonly int[] c3 = new[] { 2, 5, 8 };
        private static readonly int[] r1 = new[] { 0, 1, 2 };
        private static readonly int[] r2 = new[] { 3, 4, 5 };
        private static readonly int[] r3 = new[] { 6, 7, 8 };

        private static readonly List<int[]> WinningLines = new() { d1, d2, c1, c2, c3, r1, r2, r3, };

        private static readonly List<string> MoveText = new()
        {
            "top-left",
            "top-mid",
            "top-right",
            "left-mid",
            "center",
            "right-mid",
            "bottom-left",
            "bottom-mid",
            "bottom-right"
        };

        public void ExecutePlayerAction(byte[] state, int action)
        {
            // Game is always played from the perspective of X.
            state[action] = PieceX;
        }

        public void FlipStateToNextPlayer(byte[] state)
        {
            // no need to turn the board, just flip X and O.
            for (int i = 0; i < state.Length; i++)
            {
                if (state[i] != 0)
                    state[i] = state[i] == PieceX ? PieceO : PieceX;
            }
        }

        public GameOver.Status GetGameEnded(byte[] state, int movesMade, bool _)
        {
            // could be optimized to return draw when it is clear no winner can be found
            bool allCellsFilled = movesMade == W * H;
            if (allCellsFilled)
                return GameOver.Status.Draw;

            for (int l = 0; l < WinningLines.Count; l++)
            {
                var line = WinningLines[l];

                bool lineHasThreeInARow = state[line[0]] != 0 && state[line[0]] == state[line[1]] && state[line[1]] == state[line[2]];
                if (lineHasThreeInARow)
                    return state[line[0]] == PieceX ? GameOver.Status.Player1Won : GameOver.Status.Player2Won;
            }

            return GameOver.Status.GameIsNotOver;
        }

        public void GetStartingState(byte[] dstState)
        {
            Array.Clear(dstState, 0, dstState.Length);
        }

        public List<(byte[], float[])> GetStateSymmetries(byte[] state, float[] probs)
        {
            // Don't care about symmetries for now, just return the state as-is.
            return new List<(byte[], float[])>() { ((byte[])state.Clone(), (float[])probs.Clone()) };
        }

        public void GetValidActions(byte[] state, byte[] dstValidActions)
        {
            for (int i = 0; i < state.Length; i++)
                dstValidActions[i] = state[i] == 0 ? (byte)1 : (byte)0;
        }

        public void PrintDisplayTextForAction(int action, Action<string> print)
        {
            print($"Placing at {MoveText[action]}");
        }

        public void PrintState(byte[] state, Action<string> print)
        {
            static char C(byte b)
                => b == PieceX ? 'X' : b == PieceO ? 'O' : '.';

            print($"{C(state[0])}{C(state[1])}{C(state[2])}");
            print($"{C(state[3])}{C(state[4])}{C(state[5])}");
            print($"{C(state[6])}{C(state[7])}{C(state[8])}");
        }
    }
}