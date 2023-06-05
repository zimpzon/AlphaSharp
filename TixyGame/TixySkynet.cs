using AlphaSharp;
using AlphaSharp.Interfaces;
using System.Text;
using System.Text.Json;
using TorchSharp;

namespace TixyGame
{
    public class TixySkynet : ISkynet
    {
        private readonly IGame _game;
        private readonly int _oneHotEncodedInputSize;

        private readonly TixySkynetModel _model;

        public TixySkynet(IGame game, Args args)
        {
            _game = game;

            _oneHotEncodedInputSize = _game.W * _game.H * TixyPieces.NumberOfPieces;
            _model = new TixySkynetModel(_oneHotEncodedInputSize, _game.ActionCount);

            if (args.ResumeFromCheckpoint)
            {
                Console.WriteLine("Loading existing model...");
                if (File.Exists("c:\\temp\\zerosharp\\tixy-model-best.pt"))
                    _model.load("c:\\temp\\zerosharp\\tixy-model-best.pt");
                else
                    Console.WriteLine("No existing model found");
            }
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

            for (int i = 0; i < _game.StateSize; i++)
            {
                if (state[i] > 0)
                {
                    int idxInLayer = i;
                    int pieceLayer = TixyPieces.PieceToPlaneIdx(state[i]);
                    oneHotEncoded[pieceLayer * _game.StateSize + idxInLayer] = 1;
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
            return -(targets * outputs).sum() / targets.shape[0];
        }

        private static torch.Tensor LossV(torch.Tensor targets, torch.Tensor outputs)
        {
            return (targets - outputs.view(-1)).pow(2).sum() / targets.shape[0];
        }

        public void Train(List<TrainingData> trainingData, Args args, int iteration)
        {
            string td = JsonSerializer.Serialize(trainingData);
            File.WriteAllText($"c:\\temp\\zerosharp\\tixy-training-data-{iteration}.json", td);
            File.WriteAllText($"c:\\temp\\zerosharp\\tixy-training-data-latest.json", td);

            _model.save($"c:\\temp\\zerosharp\\tixy-model-pre-train-{iteration}.pt");
            _model.save("c:\\temp\\zerosharp\\tixy-model-pre-train-latest.pt");

            var optimizer = torch.optim.Adam(_model.parameters(), lr: args.TrainingLearningRate);

            for (int epoch = 0; epoch < args.TrainingEpochs; ++epoch)
            {
                _model.train();

                int batchCount = trainingData.Count / args.TrainingBatchSize;

                float totalLossV = 0;
                float totalLossProbs = 0;

                for (int b = 0; b < batchCount; ++b)
                {
                    try
                    {
                        var batchIndices = torch.randint(trainingData.Count, args.TrainingBatchSize).data<long>().ToList();
                        var batch = batchIndices.Select(i => trainingData[(int)i]);

                        var oneHotArray = batch.Select(td => OneHotEncode(td.State)).ToArray();
                        var desiredProbsArray = batch.Select(td => td.ActionProbs).ToArray();
                        var desiredVsArray = batch.Select(td => td.Player1Value).ToArray();

                        using var oneHotBatchTensor = torch.stack(oneHotArray.Select(a => torch.from_array(a))).reshape(args.TrainingBatchSize, -1);
                        using var desiredProbsBatchTensor = torch.stack(desiredProbsArray.Select(p => torch.from_array(p))).reshape(args.TrainingBatchSize, -1);
                        using var desiredVsBatchTensor = torch.from_array(desiredVsArray);

                        var (logProbs, vt) = _model.forward(oneHotBatchTensor);

                        var lossV = LossV(desiredVsBatchTensor, vt);
                        var lossProbs = LossProbs(desiredProbsBatchTensor, logProbs);
                        var totalLoss = lossV + lossProbs;

                        optimizer.zero_grad();
                        totalLoss.backward();
                        optimizer.step();

                        totalLossV += lossV.ToSingle();
                        totalLossProbs += lossProbs.ToSingle();

                        if (epoch == 0 && b == 0)
                            Console.WriteLine($"epoch start: {epoch + 1} Loss: {totalLossV + totalLossProbs}, lossV: {totalLossV}, lossProbs: {totalLossProbs}");
                    }
                    catch (Exception ex)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine(ex.ToString());
                        sb.AppendLine($"ERROR in batch");
                        sb.AppendLine($"batchId {b}");
                        sb.AppendLine($"trainingData.count:  {trainingData.Count}");
                        sb.AppendLine();
                        sb.AppendLine();
                        Console.WriteLine(sb.ToString());
                        File.AppendAllText("c:\\temp\\zerosharp\\errors.txt", sb.ToString());
                    }

                    if (epoch == args.TrainingEpochs - 1 && b == batchCount - 1)
                        Console.WriteLine($"epoch end: {epoch + 1} Loss: {(totalLossV + totalLossProbs) / batchCount}, lossV: {totalLossV / batchCount}, lossProbs: {totalLossProbs / batchCount}");
                }
            }

            _model.save($"c:\\temp\\zerosharp\\tixy-model-post-train-{iteration}.pt");
            _model.save("c:\\temp\\zerosharp\\tixy-model-post-train-latest.pt");
        }

        public void Suggest(byte[] state, float[] dstActionsProbs, out float v)
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
    }
}
