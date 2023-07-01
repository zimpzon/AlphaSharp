using AlphaSharp.Interfaces;
using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModelConv : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private sealed class Conv2dBlock
        {
            public Conv2d Conv { get; set; }
            public BatchNorm2d BatchNorm { get; set; }
        }

        private readonly List<Conv2dBlock> conv2dBlocks = new();

        private readonly Conv2d _conv2dEntry;
        private readonly BatchNorm2d _batchNorm2dEntry;
        private readonly IGame _game;
        private readonly int _numInputChannels;

        private readonly Linear _fc_probs;
        private readonly Linear _fc_v;

        private readonly LogSoftmax _logSoftmax;
        private readonly Tanh _tanh;

        const int NumChannels = 128;
        const int NumConvBlocks = 3;

        public TixySkynetModelConv(IGame game, int numInputChannels) : base("tixy")
        {
            _game = game;
            _numInputChannels = numInputChannels;

            const int kernelSize = 3;
            const int stride = 1;
            int padding = ((game.W - 1) * stride + kernelSize - game.W) / 2;

            _conv2dEntry = torch.nn.Conv2d(numInputChannels, NumChannels, kernelSize, stride, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2dEntry = torch.nn.BatchNorm2d(NumChannels);

            for (int i = 0; i < NumConvBlocks; i++)
            {
                var block = new Conv2dBlock
                {
                    Conv = torch.nn.Conv2d(NumChannels, NumChannels, kernelSize, stride: 1, padding, dilation: 1, PaddingModes.Zeros, bias: false),
                    BatchNorm = torch.nn.BatchNorm2d(NumChannels)
                };

                conv2dBlocks.Add(block);
            }

            _fc_probs = torch.nn.Linear(NumChannels * game.W * game.H, game.ActionCount);
            _fc_v = torch.nn.Linear(NumChannels * game.W * game.H, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();

            RegisterComponents();
        }

        public override (torch.Tensor, torch.Tensor) forward(torch.Tensor x)
        {
            x = x.view(x.shape[0], _numInputChannels, _game.W, _game.H);

            x = _conv2dEntry.forward(x);
            x = _batchNorm2dEntry.forward(x);
            x = torch.nn.functional.relu(x);

            for (int i = 0; i < NumConvBlocks; i++)
            {
                torch.Tensor risidual = null;
                bool isLastBlock = i == NumConvBlocks - 1;
                if (!isLastBlock)
                    risidual = x;

                var block = conv2dBlocks[i];
                x = block.Conv.forward(x);
                x = block.BatchNorm.forward(x);
                x = torch.nn.functional.relu(x);

                if (!isLastBlock)
                    x += risidual;
            }

            x = x.view(-1, NumChannels * _game.W * _game.H);

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
