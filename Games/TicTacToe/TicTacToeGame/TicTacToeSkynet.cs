﻿using AlphaSharp;
using AlphaSharp.Interfaces;
using TorchSharp;
using static AlphaSharp.AlphaSharpTrainer;

namespace TicTacToeGame
{
    public class TicTacToeSkynet : ISkynet
    {
        private readonly IGame _game;
        private readonly int _oneHotEncodedInputSize;
        private readonly TicTacToeSkynetModel _model;
        private readonly TicTacToeParameters _param;

        public TicTacToeSkynet(IGame game, TicTacToeParameters param)
        {
            _game = game;
            _param = param;

            const int NumberOfPieces = 2; // X and O
            _oneHotEncodedInputSize = _game.W * _game.H * NumberOfPieces;
            _model = new TicTacToeSkynetModel(_oneHotEncodedInputSize, _game.ActionCount);
        }

        public void LoadModel(string modelPath)
        {
            _model.load(modelPath);
        }

        public void SaveModel(string modelPath)
        {
            _model.save(modelPath);
        }

        public void Suggest(byte[] state, int playerTurn, float[] dstActionsProbs, out float v)
        {
            _model.eval();
            using var x = torch.no_grad();

            var oneHotEncoded = OneHotEncode(state);
            var oneHotTensor = torch.from_array(oneHotEncoded).reshape(1, oneHotEncoded.Length);
            var (logProbs, vt) = _model.forward(oneHotTensor);

            v = vt.ToSingle();

            var probs = torch.exp(logProbs);

            var data = probs.flatten().data<float>();
            data.CopyTo(dstActionsProbs);
        }

        private float[] OneHotEncode(byte[] state)
        {
            var oneHotEncoded = new float[_oneHotEncodedInputSize];

            for (int i = 0; i < _game.StateSize; i++)
            {
                if (state[i] > 0)
                {
                    int idxInLayer = i;
                    int pieceLayer = state[i] == TicTacToe.PieceX ? 0 : 1;
                    oneHotEncoded[pieceLayer * _game.StateSize + idxInLayer] = 1;
                }
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
            var optimizer = torch.optim.Adam(_model.parameters(), lr: _param.TrainingLearningRate);

            for (int epoch = 0; epoch < _param.TrainingEpochs; ++epoch)
            {
                _model.train();

                int batchCount = trainingData.Count / _param.TrainingBatchSize;

                float latestLossV = 0;
                float latestLossProbs = 0;

                //var meanSquaredError = torch.nn.MSELoss();
                //var crossEntropy = torch.nn.CrossEntropyLoss();

                for (int b = 0; b < batchCount; ++b)
                {
                    var batchIndices = torch.randint(trainingData.Count, _param.TrainingBatchSize).data<long>().ToList();
                    var batch = batchIndices.Select(i => trainingData[(int)i]);

                    var oneHotArray = batch.Select(td => OneHotEncode(td.State)).ToArray();
                    var desiredProbsArray = batch.Select(td => td.ActionProbs).ToArray();
                    var desiredVsArray = batch.Select(td => td.ValueForPlayer1).ToArray();

                    using var oneHotBatchTensor = torch.stack(oneHotArray.Select(a => torch.from_array(a))).reshape(_param.TrainingBatchSize, -1);
                    using var desiredProbsBatchTensor = torch.stack(desiredProbsArray.Select(p => torch.from_array(p))).reshape(_param.TrainingBatchSize, -1);
                    using var desiredVsBatchTensor = torch.from_array(desiredVsArray);

                    var (logProbs, vt) = _model.forward(oneHotBatchTensor);

                    var lossV = LossV(desiredVsBatchTensor, vt);
                    var lossProbs = LossProbs(desiredProbsBatchTensor, logProbs);
                    //var lossV = meanSquaredError.forward(vt.view(-1), desiredVsBatchTensor);
                    //var lossProbs = crossEntropy.forward(logProbs, desiredProbsBatchTensor);
                    var totalLoss = lossV + lossProbs;

                    optimizer.zero_grad();
                    totalLoss.backward();
                    optimizer.step();

                    latestLossV = lossV.ToSingle();
                    latestLossProbs = lossProbs.ToSingle();
                }

                progressCallback(epoch + 1, _param.TrainingEpochs, $"lossV: {latestLossV}, lossProbs: {latestLossProbs}");
            }
        }
    }
}