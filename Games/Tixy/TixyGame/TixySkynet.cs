using AlphaSharp;
using AlphaSharp.Interfaces;
using TorchSharp;
using static AlphaSharp.AlphaSharpTrainer;

namespace TixyGame
{
    public class TixySkynet : ISkynet
    {
        private readonly IGame _game;
        private readonly int _oneHotEncodedInputSize;

        private readonly TixySkynetModel _model;
        private readonly TixyParameters _param;

        public TixySkynet(IGame game, TixyParameters param)
        {
            _game = game;
            _param = param;

            _oneHotEncodedInputSize = _game.W * _game.H * TixyPieces.NumberOfPieces;
            _model = new TixySkynetModel(_oneHotEncodedInputSize, _game.ActionCount);
        }

        public void LoadModel(string modelPath)
        {
            _model.load(modelPath);
        }

        public void SaveModel(string modelPath)
        {
            _model.save(modelPath);
        }

        private float[] OneHotEncode(byte[] state)
        {
            var oneHotEncoded = new float[_oneHotEncodedInputSize];

            int layerSize = _game.StateSize;

            for (int i = 0; i < _game.StateSize; i++)
            {
                if (state[i] > 0)
                {
                    int idxInLayer = i;
                    int pieceLayer = TixyPieces.PieceToPlaneIdx(state[i]);
                    oneHotEncoded[pieceLayer * layerSize + idxInLayer] = 1;
                }
            }

            // verify one-hot encoding
            //int cnt = 0;
            //for (int i = 0; i < oneHotEncoded.Length; i++)
            //{
            //    if (oneHotEncoded[i] != 0)
            //    {
            //        int ii = i % 25;
            //        cnt++;
            //        int x = ii % _game.W;
            //        int y = ii / _game.W;
            //        int p = TixyPieces.PlaneIdxToPiece(i / _game.StateSize);
            //        int l = state[y * _game.W + x];
            //        if (l != p)
            //            Console.WriteLine("wut");
            //    }
            //}

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
            var optimizer = torch.optim.Adam(_model.parameters(), lr: _param.TrainingLearningRate);

            for (int epoch = 0; epoch < _param.TrainingEpochs; ++epoch)
            {
                _model.train();

                int batchCount = trainingData.Count / _param.TrainingBatchSize;

                float latestLossV = 0;
                float latestLossProbs = 0;

                for (int b = 0; b < batchCount; ++b)
                {
                    var batchIndices = Enumerable.Range(b * _param.TrainingBatchSize, _param.TrainingBatchSize).ToList();
                    //var batchIndices = torch.randint(trainingData.Count, _param.TrainingBatchSize).data<long>().ToList();
                    var batch = batchIndices.Select(i => trainingData[(int)i]);

                    var oneHotArray = batch.Select(td => OneHotEncode(td.State)).ToArray();
                    var desiredProbsArray = batch.Select(td => td.ActionProbs).ToArray();
                    var desiredVsArray = batch.Select(td => td.ValueForPlayer1).ToArray();

                    var oneHotBatchTensor = torch.stack(oneHotArray.Select(a => torch.from_array(a))).reshape(_param.TrainingBatchSize, -1);
                    var desiredProbsBatchTensor = torch.stack(desiredProbsArray.Select(p => torch.from_array(p))).reshape(_param.TrainingBatchSize, -1);
                    var desiredVsBatchTensor = torch.from_array(desiredVsArray);

                    var (logProbs, vt) = _model.forward(oneHotBatchTensor);

                    var lossV = LossV(desiredVsBatchTensor, vt);
                    var lossProbs = LossProbs(desiredProbsBatchTensor, logProbs);
                    var totalLoss = lossV + lossProbs;

                    optimizer.zero_grad();
                    totalLoss.backward();
                    optimizer.step();

                    latestLossV += lossV.ToSingle();
                    latestLossProbs += lossProbs.ToSingle();
                }

                progressCallback?.Invoke(epoch + 1, _param.TrainingEpochs, $"lossV: {latestLossV / batchCount}, lossProbs: {latestLossProbs / batchCount}");
            }
        }

        public void Suggest(byte[] state, float[] dstActionsProbs, out float v)
        {
            _model.eval();
            var x = torch.no_grad();

            var oneHotEncoded = OneHotEncode(state);
            var oneHotTensor = torch.from_array(oneHotEncoded).reshape(1, oneHotEncoded.Length);
            var (logProbs, vt) = _model.forward(oneHotTensor);

            v = vt.ToSingle();

            var probs = torch.exp(logProbs);

            var data = probs.flatten().data<float>();
            data.CopyTo(dstActionsProbs);
        }
    }
}
