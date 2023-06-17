using AlphaSharp;
using AlphaSharp.Interfaces;

namespace FakeGame
{
    public class FakeGame : IGame
    {
        public int W => 4;
        public int H => 1;

        public string Name => "FakeGame";
        public int ActionCount => 1;
        public int StateSize => W * H;

        public void GetStartingState(byte[] dstState)
        {
            var s = new byte[] { 1, 0, 0, 2 };
            Array.Copy(s, dstState, s.Length);
        }

        public GameOver.Status GetGameEnded(byte[] state, int movesMade, bool isSimulation)
        {
            if (state.Sum(x => x) == 1)
                return GameOver.Status.Player1Won;
            else if (state.Sum(x => x) == 2)
                return GameOver.Status.Player2Won;
            else
                return GameOver.Status.GameIsNotOver;
        }

        public void GetValidActions(byte[] state, byte[] dstValidActions)
        {
            Array.Clear(dstValidActions);
            dstValidActions[0] = 1;
        }

        public void ExecutePlayerAction(byte[] state, int action)
        {
            if (action != 0)
                throw new ArgumentException($"Invalid action: {action}");

            int pos = 0;
            for (int i = 0; i < W; i++)
            {
                if (state[i] == 1)
                {
                    pos = i;
                    state[i] = 0;
                    break;
                }
                state[i] = 0;
            }

            state[pos + 1] = 1;
        }

        public void FlipStateToNextPlayer(byte[] state)
        {
            Array.Reverse(state);
            for (int i = 0; i < state.Length; ++i)
            {
                if (state[i] == 1)
                    state[i] = 2;
                else if (state[i] == 2)
                    state[i] = 1;
            }
        }


        public void PrintState(byte[] state, Action<string> print)
        {
            print("none");
        }

        public void PrintDisplayTextForAction(int action, Action<string> print)
        {
            print("none");
        }

        public List<(byte[], float[])> GetStateSymmetries(byte[] state, float[] probs)
        {
            throw new NotImplementedException();
        }
    }
}