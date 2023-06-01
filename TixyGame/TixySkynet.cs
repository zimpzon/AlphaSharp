using AlphaSharp.Interfaces;
using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynet : torch.nn.Module, ISkynet
    {
        private readonly IGame _game;

        private readonly Linear _fc1;
        private readonly BatchNorm1d _bn1;
        private readonly Dropout _drop1;

        private readonly Linear _fc2;
        private readonly BatchNorm1d _bn2;
        private readonly Dropout _drop2;

        private readonly Linear _fc3;
        private readonly Linear _fc4;

        private readonly LogSoftmax _logSoftmax;
        private readonly Tanh _tanh;

        public TixySkynet(IGame game) : base("tixy")
        {
            _game = game;

            const float DropOut = 0.3f;

            int oneHotEncodedInputSize = _game.W * _game.H * TixyPieces.NumberOfPieces;

            _fc1 = torch.nn.Linear(oneHotEncodedInputSize, 1024);
            _bn1 = torch.nn.BatchNorm1d(1024);
            _drop1 = torch.nn.Dropout(DropOut);

            _fc2 = torch.nn.Linear(1024, 512);
            _bn2 = torch.nn.BatchNorm1d(512);
            _drop2 = torch.nn.Dropout(DropOut);

            _fc3 = torch.nn.Linear(512, _game.ActionCount);
            _fc4 = torch.nn.Linear(512, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();
        }

        private float[] Forward(torch.Tensor x, out float v)
        {
            // PERF: switch to Sequential for better performance
            x = _fc1.forward(x);
            x = _bn1.forward(x);
            x = torch.nn.functional.relu(x);
            x = _drop1.forward(x);

            x = _fc2.forward(x);
            x = _bn2.forward(x);
            x = torch.nn.functional.relu(x);
            x = _drop2.forward(x);

            var value = _fc4.forward(x);
            v = _tanh.forward(value).ToSingle();

            var probs = _fc3.forward(x);
            probs = _logSoftmax.forward(probs);

            return ((IList<float>)probs.tolist()).ToArray();
        }

        public torch.Tensor OneHotEncode(torch.Tensor batch)
        {
            long batchCount = batch.shape[0];
            long pieceCount = TixyPieces.NumberOfPieces;
            long oneHotSize = batch.shape[1] * pieceCount;

            var oneHotPlanes = torch.zeros(new long[] { batchCount, oneHotSize }, dtype: torch.float32);

            for (long batchIdx = 0; batchIdx < batchCount; batchIdx++)
            {
                for (int i = 1; i <= TixyPieces.NumberOfPieces; i++) // these are the piece types. It sets a 1 in the correct plane for each piece type, for each batch.
                {
                    oneHotPlanes[batchIdx, i - 1] = batch[batchIdx].eq(i); // planes 0..numberOfPieces-1
                }
            }

            return oneHotPlanes;
        }

        public void Suggest(byte[] state, float[] actionsProbs, out float v)
        {
            const int BatchSize = 1;
            var batch = torch.from_array(state).reshape(BatchSize, state.Length);

            var oneHotEncode = OneHotEncode(batch);
            Forward(oneHotEncode, out v);

            // here state will be converted to 1-hot encoded. write directly to a tensor, if possible
            // or we need to create an array in this call, OR pass in a temp array to avoid miltithreading conflicts.
            //var t = torch.tensor(currentState, );
            v = 0.1f;
        }
    }
}
