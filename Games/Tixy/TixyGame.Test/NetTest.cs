using AlphaSharp;
using System.Diagnostics;
using Xunit;

namespace TixyGame.Test
{
    public class NetTest
    {
        public class Mcts
        {
            [Fact]
            public void TestRun()
            {
                var game = new Tixy(5, 5);
                byte[] input = new byte[game.StateSize];
                float[] dst1 = new float[game.ActionCount];
                float[] dst2 = new float[game.ActionCount];

                var skynet = new TixySkynet(game, new TixyParameters());

                var state1 = new byte[game.StateSize];
                state1[3] = TixyPieces.P1.X;

                var state2 = new byte[game.StateSize];
                state2[7] = TixyPieces.P1.Y;

                var probsPl1 = new float[game.ActionCount];
                probsPl1[4] = 1;

                var probsPl2 = new float[game.ActionCount];
                probsPl2[5] = 1;

                var td = new List<TrainingData>();
                for (int i = 0; i < 10000; i++)
                {
                    bool p1 = i % 2 == 0;

                    // just one state, it is good if it is my turn (p1) and bad if is not my turn (p2)

                    // won: mark all state+probs as 1
                    // los: mark all state+probs as -1

                    var d = new TrainingData
                    {
                        State = new List<byte>(p1 ? state1 : state2).ToArray(),
                        ActionProbs = new List<float>(p1 ? probsPl1 : probsPl2).ToArray(),
                        ValueForPlayer1 = p1 ? 1 : -1,
                    };

                    td.Add(d);
                };

                static void ProgressCallback(int currentValue, int numberOfValues, string additionalInfo = null)
                {
                    Trace.WriteLine($"{currentValue} / {numberOfValues} {additionalInfo}");
                }

                // TODO: load existing training data and analyze it. Are there states that are maked as both good and bad? Other things?


                skynet.Train(td, new AlphaSharpTrainer.TrainingProgressCallback(ProgressCallback));
                skynet.Suggest(state1, dst1, out float v1);
                // for p1 want: input state1, 110 highest prob, v = 1

                skynet.Suggest(state2, dst2, out float v2);
                // for p2 want: input state2, NOT 90 highest prob, v = -1
            }
        }
    }
}