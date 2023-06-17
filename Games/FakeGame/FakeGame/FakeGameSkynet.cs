using AlphaSharp;
using AlphaSharp.Interfaces;
using TorchSharp;
using static AlphaSharp.AlphaSharpTrainer;

namespace FakeGame
{
    public class FakeGameSkynet : ISkynet
    {
        private readonly IGame _game;
        private readonly int _oneHotEncodedInputSize;

        private readonly FakeGameSkynetModel _model;

        public FakeGameSkynet(IGame game)
        {
            _game = game;

            _oneHotEncodedInputSize = _game.W * _game.H * 3; // 1 layer for current player
            _model = new FakeGameSkynetModel(_oneHotEncodedInputSize, _game.ActionCount);
        }

        public void LoadModel(string modelPath)
        {
            _model.load(modelPath);
        }

        public void SaveModel(string modelPath)
        {
            _model.save(modelPath);
        }

        private float[] OneHotEncode(byte[] state, int playerTurn)
        {
            var oneHotEncoded = new float[_oneHotEncodedInputSize];
            int layerSize = _game.W * _game.H;

            for (int i = 0; i < _game.StateSize; i++)
            {
                if (state[i] > 0)
                {
                    int idxInLayer = i;
                    int pieceLayer = state[i] == 1 ? 0 : 1;
                    oneHotEncoded[pieceLayer * layerSize + idxInLayer] = 1;
                }
            }

            float playerVal = playerTurn == 1 ? 1 : -1;

            for (int i = 0; i < layerSize; ++i)
            {
                oneHotEncoded[2 * layerSize + i] = playerVal;
            }

            return oneHotEncoded;
        }

        private static torch.Tensor LossProbs(torch.Tensor targets, torch.Tensor outputs)
        {
            // add a tiny amount to targets to avoid multiplying by zero
            return -((targets + 0.00001f) * outputs).sum() / targets.shape[0];
        }

        private static torch.Tensor LossV(torch.Tensor targets, torch.Tensor outputs)
        {
            return (targets - outputs.view(-1)).pow(2).sum() / targets.shape[0];
        }

        public void Train(List<TrainingData> trainingData, TrainingProgressCallback progressCallback)
        {
            var optimizer = torch.optim.Adam(_model.parameters(), lr: 0.001);

            const int Epochs = 10;
            for (int epoch = 0; epoch < Epochs; ++epoch)
            {
                _model.train();

                const int BatchSize = 64;
                int batchCount = trainingData.Count / BatchSize;

                float latestLossV = 0;
                float latestLossProbs = 0;

                for (int b = 0; b < batchCount; ++b)
                {
                    var batchIndices = torch.randint(trainingData.Count, BatchSize).data<long>().ToList();
                    var batch = batchIndices.Select(i => trainingData[(int)i]);

                    var oneHotArray = batch.Select(td => OneHotEncode(td.State, td.PlayerTurn)).ToArray();
                    var desiredProbsArray = batch.Select(td => td.ActionProbs).ToArray();
                    var desiredVsArray = batch.Select(td => td.ValueForPlayer1).ToArray();

                    using var oneHotBatchTensor = torch.stack(oneHotArray.Select(a => torch.from_array(a))).reshape(BatchSize, -1);
                    using var desiredProbsBatchTensor = torch.stack(desiredProbsArray.Select(p => torch.from_array(p))).reshape(BatchSize, -1);
                    using var desiredVsBatchTensor = torch.from_array(desiredVsArray);

                    var (logProbs, vt) = _model.forward(oneHotBatchTensor);

                    var lossV = LossV(desiredVsBatchTensor, vt);
                    var lossProbs = LossProbs(desiredProbsBatchTensor, logProbs);
                    var totalLoss = lossV + lossProbs;

                    optimizer.zero_grad();
                    totalLoss.backward();
                    optimizer.step();

                    latestLossV = lossV.ToSingle();
                    latestLossProbs = lossProbs.ToSingle();
                }

                progressCallback(epoch + 1, Epochs, $"lossV: {latestLossV}, lossProbs: {latestLossProbs}");
            }
        }

        public void Suggest(byte[] state, int playerTurn, float[] dstActionsProbs, out float v)
        {
            _model.eval();
            using var x = torch.no_grad();

            var oneHotEncoded = OneHotEncode(state, playerTurn);
            var oneHotTensor = torch.from_array(oneHotEncoded).reshape(1, oneHotEncoded.Length);
            var (logProbs, vt) = _model.forward(oneHotTensor);

            v = vt.ToSingle();

            var probs = torch.exp(logProbs);

            var data = probs.flatten().data<float>();
            data.CopyTo(dstActionsProbs);
        }
    }
}
