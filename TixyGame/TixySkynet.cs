using AlphaSharp.Interfaces;
using TorchSharp;

namespace TixyGame
{
    public class TixySkynet : ISkynet
    {
        private readonly IGame _game;

        private readonly TixySkynetModel _model;

        public TixySkynet(IGame game)
        {
            _game = game;
            torch.set_num_threads(4);
            torch.set_num_interop_threads(2);

            int oneHotEncodedInputSize = _game.W * _game.H * TixyPieces.NumberOfPieces;
            _model = new TixySkynetModel(oneHotEncodedInputSize, _game.ActionCount, isTraining: false);
        }

        private static torch.Tensor OneHotEncode(torch.Tensor batch)
        {
            long batchCount = batch.shape[0];
            long layerSize = batch.shape[1];
            long pieceCount = TixyPieces.NumberOfPieces;

            var oneHotPlanes = torch.zeros(new long[] { batchCount, pieceCount, layerSize }, dtype: torch.float32);

            for (long batchIdx = 0; batchIdx < batchCount; batchIdx++)
            {
                for (int i = 1; i <= TixyPieces.NumberOfPieces; i++) // these are the piece types. It sets a 1 in the correct plane for each piece type, for each batch.
                {
                    oneHotPlanes[batchIdx, i - 1] = batch[batchIdx].eq(i); // planes 0..numberOfPieces-1
                }
            }

            return oneHotPlanes.flatten(start_dim: 1);
        }

        public void Suggest(byte[] state, float[] actionsProbs, out float v)
        {
            const int BatchSize = 1;
            using var batch = torch.from_array(state).reshape(BatchSize, state.Length);

            torch.no_grad();
            _model.eval();

            using var oneHotEncode = OneHotEncode(batch);
            using var output = _model.Forward(oneHotEncode, out v);
            using var probs = torch.exp(output);

            for (int i = 0; i < probs.shape[1]; i++)
            {
                actionsProbs[i] = probs[0, i].ToSingle();
            }
        }
    }
}
