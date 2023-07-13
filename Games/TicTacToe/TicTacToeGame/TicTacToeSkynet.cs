using AlphaSharp;
using AlphaSharp.Interfaces;
using TorchSharp;
using static AlphaSharp.AlphaSharpTrainer;

namespace TixyGame
{
    public class TicTacToeSkynet : ISkynet
    {
        private readonly IGame _game;
        private readonly int _oneHotEncodedInputSize;
        private readonly bool _forceCpu = false;

        private readonly GenericSkynetModel _model;
        private readonly GenericSkynetParam _param;

        private readonly IReadOnlyDictionary<byte, int> _pieceToLayerLut;

        public TicTacToeSkynet(IGame game, GenericSkynetParam param)
        {
            _game = game;
            _param = param;
            _pieceToLayerLut = new Dictionary<byte, int> { [1] = 0, [2] = 1, };
            _oneHotEncodedInputSize = _game.W * _game.H * _param.NumberOfPieces;
            _model = new GenericSkynetModel(_game, _oneHotEncodedInputSize, forceCpu: false);
        }

        public void LoadModel(string modelPath)
        {
            _model.SetDevice(DeviceType.CPU);
            _model.load(modelPath);
            _model.SetDeviceAuto();
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
                    int pieceLayer = _pieceToLayerLut[state[i]];
                    oneHotEncoded[pieceLayer * layerSize + idxInLayer] = 1;
                }
            }

            return oneHotEncoded;
        }

        private static torch.Tensor LossProbs(torch.Tensor targets, torch.Tensor outputs)
        {
            return -(targets * outputs).sum() / targets.shape[0];
        }

        private static torch.Tensor LossV(torch.Tensor targets, torch.Tensor outputs)
        {
            return (targets - outputs.view(-1)).pow(2).sum() / targets.shape[0];
        }

        public void Train(List<TrainingData> trainingData, TrainingProgressCallback progressCallback)
        {
            for (int i = 0; i < trainingData.Count; i++)
            {
                var td = trainingData[i];
                td.ValueForPlayer1 = (td.ValueForPlayer1 + 1) * 0.5f;
            }

            var optimizer = torch.optim.Adam(_model.parameters(), lr: _param.TrainingLearningRate);

            torch.set_num_threads(_param.TrainingMaxWorkerThreads);
            _model.SetDeviceAuto();

            for (int epoch = 0; epoch < _param.TrainingEpochs; ++epoch)
            {
                using var disposeScope = torch.NewDisposeScope();
                _model.train();

                AlphaUtil.Shuffle(trainingData);

                int batchSize = _param.TrainingBatchSize;
                int batchCount = Math.Min(trainingData.Count / _param.TrainingBatchSize, _param.TrainingBatchesPerEpoch);

                if (batchCount == 0)
                {
                    batchCount = 1;
                    batchSize = trainingData.Count;
                }

                float batchLossV = 0;
                float batchLossProbs = 0;

                for (int b = 0; b < batchCount; ++b)
                {
                    // reduce overfitting by not training on all data every epoch, but instead randomly selecting a subset
                    var batchIndices = Enumerable.Range(b * batchSize, batchSize).ToList();
                    var batch = batchIndices.Select(i => trainingData[i]);

                    var oneHotArray = batch.Select(td => OneHotEncode(td.State)).ToArray();
                    var desiredProbsArray = batch.Select(td => td.ActionProbs).ToArray();
                    var desiredVsArray = batch.Select(td => td.ValueForPlayer1).ToArray();

                    var oneHotBatchTensor = torch.stack(oneHotArray.Select(a => torch.from_array(a))).reshape(batchSize, -1);
                    var desiredProbsBatchTensor = torch.stack(desiredProbsArray.Select(p => torch.from_array(p))).reshape(batchSize, -1).to(_model.Device);
                    var desiredVsBatchTensor = torch.from_array(desiredVsArray).to(_model.Device);

                    var (logProbs, vt) = _model.forward(oneHotBatchTensor);

                    var lossV = LossV(desiredVsBatchTensor, vt);
                    var lossProbs = LossProbs(desiredProbsBatchTensor, logProbs);
                    var totalLoss = lossV + lossProbs;

                    optimizer.zero_grad();
                    totalLoss.backward();
                    optimizer.step();

                    batchLossV += lossV.ToSingle();
                    batchLossProbs += lossProbs.ToSingle();
                }

                progressCallback?.Invoke(epoch + 1, _param.TrainingEpochs, $"lossV: {batchLossV / batchCount}, lossProbs: {batchLossProbs / batchCount}");
            }

            torch.set_num_threads(1);

            if (_forceCpu)
                _model.SetDevice(DeviceType.CPU);
            else
                _model.SetDeviceAuto();
        }

        public void Suggest(byte[] state, float[] dstActionsProbs, out float v)
        {
            _model.eval();

            using var disposeScope = torch.NewDisposeScope();
            using var _ = torch.no_grad();

            var oneHotEncoded = OneHotEncode(state);
            var oneHotTensor = torch.from_array(oneHotEncoded).reshape(1, oneHotEncoded.Length);
            var (logProbs, vt) = _model.forward(oneHotTensor);

            v = vt.ToSingle();
            v = v * 2 - 1;

            var probs = torch.exp(logProbs);

            var data = probs.flatten().data<float>();
            data.CopyTo(dstActionsProbs);
        }
    }
}
