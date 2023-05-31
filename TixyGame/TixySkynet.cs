using AlphaSharp.Interfaces;
using TorchSharp;
using static TorchSharp.torch;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace TixyGame
{
    public class TixySkynet : ISkynet
    {
        private readonly IGame _game;
        private readonly Linear _fc1;
        private readonly BatchNorm1d _fc_bn1;
        private readonly Linear _fc2;
        private readonly BatchNorm1d _fc_bn2;
        private readonly Linear _fc3;
        private readonly Linear _fc4;

        public TixySkynet(IGame game)
        {
            _game = game;

            int oneHotEncodedInputSize = _game.W * _game.H * TixyPieces.NumberOfPieces;

            _fc1 = Linear(oneHotEncodedInputSize, 1024);
            _fc_bn1 = BatchNorm1d(1024);

            _fc2 = Linear(1024, 512);
            _fc_bn2 = BatchNorm1d(512);

            _fc3 = Linear(512, _game.ActionCount);
            _fc4 = Linear(512, 1);
        }

        private void Forward()
        {
            // batch count, channels, height, width - flattened
            Tensor s = new FloatTensor(1, 1, 1, 1);
            _fc1.forward(s);

        }

        public void Suggest(byte[] currentState, float[] actionsProbs, out float v)
        {
            var rnd = new Random();
            for (int i = 0; i < actionsProbs.Length; i++)
                actionsProbs[i] = (float)rnd.NextDouble();

            // here state will be converted to 1-hot encoded. write directly to a tensor, if possible
            // or we need to create an array in this call, OR pass in a temp array to avoid miltithreading conflicts.
            //var t = torch.tensor(currentState, );
            v = 0.1f;
        }
    }
}
