using AlphaSharp;
using AlphaSharp.Interfaces;
using System.Text;

namespace FakeGame
{
    public class FakeGame : IGame
    {
        public int W => 10;
        public int H => 3;

        public string Name => "FakeGame";
        public int ActionCount => 4;
        public int StateSize => 2;

        private GameOver.Status _gameResult = GameOver.Status.GameIsNotOver;

        public void GetStartingState(byte[] dstState)
        {
            dstState[0] = 0;
            dstState[1] = (byte)(W - 1);
        }

        public GameOver.Status GetGameEnded(byte[] state, int movesMade, bool isSimulation)
            => _gameResult;

        public void GetValidActions(byte[] state, byte[] dstValidActions)
        {
            // N, E, S, W
            dstValidActions[0] = 1;
            dstValidActions[1] = state[0] < W - 1 ? (byte)1 : (byte)0;
            dstValidActions[2] = 1;
            dstValidActions[3] = state[0] > 0 ? (byte)1 : (byte)0;

            Array.Clear(dstValidActions);
            dstValidActions[0] = 1;
        }

        public void ExecutePlayerAction(byte[] state, int action)
        {
            if (action == 0 || action == 2)
                _gameResult = GameOver.Status.Player2Won;
            else if (action == 1)
                state[0]++;
            else if (action == 3)
                state[0]--;
        }

        public void FlipStateToNextPlayer(byte[] state)
        {
            byte tmp = state[0];
            state[0] = state[1];
            state[1] = tmp;
        }

        public void PrintState(byte[] state, Action<string> print)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < H; ++y)
            {
                for (int x = 0; x < W; ++x)
                {
                    int idx = y * W + x;
                    if (x == state[0] && y == 1)
                        sb.Append('1');
                    else if (x == state[1] && y == 1)
                        sb.Append('2');
                    else
                        sb.Append(state[idx] == 255 ? '*' : ' ');
                }
            }
            print(sb.ToString());
        }

        public void PrintDisplayTextForAction(int action, Action<string> print)
        {
            print("(print move not supported)");
        }

        public List<(byte[], float[])> GetStateSymmetries(byte[] state, float[] probs)
        {
            throw new NotImplementedException();
        }
    }
}