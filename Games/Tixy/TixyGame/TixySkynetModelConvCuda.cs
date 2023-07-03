using AlphaSharp.Interfaces;
using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModelConvCuda : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private sealed class Conv2dBlock
        {
            public Conv2d Conv { get; set; }
            public BatchNorm2d BatchNorm { get; set; }

            public void ToDevice(torch.Device device)
            {
                Conv = Conv.to(device);
                BatchNorm = BatchNorm.to(device);
            }
        }

        private readonly Conv2d _conv2dEntry;
        private readonly BatchNorm2d _batchNorm2dEntry;

        private readonly Conv2d _conv2d1;
        private readonly BatchNorm2d _batchNorm2d1;

        private readonly Conv2d _conv2d2;
        private readonly BatchNorm2d _batchNorm2d2;

        private readonly Conv2d _conv2d3;
        private readonly BatchNorm2d _batchNorm2d3;

        private readonly Conv2d _conv2d4;
        private readonly BatchNorm2d _batchNorm2d4;

        private readonly Conv2d _conv2d5;
        private readonly BatchNorm2d _batchNorm2d5;

        private readonly List<Conv2dBlock> _blocks = new List<Conv2dBlock>();

        private readonly IGame _game;
        private readonly int _numInputChannels;

        private readonly Linear _fcEnd;
        private readonly Dropout _dropoutEnd;

        private readonly Linear _fc_probs;
        private readonly Linear _fc_v;

        private readonly LogSoftmax _logSoftmax;
        private readonly Tanh _tanh;

        public torch.Device Device { get; set; }

        public void SetDevice(DeviceType deviceType)
        {
            Device = torch.device(deviceType);
            this.to(Device);
        }

        public void SetDeviceAuto()
        {
            var deviceType = torch.cuda.is_available() ? DeviceType.CUDA : DeviceType.CPU;
            SetDevice(deviceType);
        }

        const int NumChannels = 32;
        const int NumConvBlocks = 5;

        public TixySkynetModelConvCuda(IGame game, int numInputChannels, bool forceCpu = false) : base("tixy")
        {
            _game = game;
            _numInputChannels = numInputChannels;

            const int kernelSize = 3;
            const int stride = 1;
            int padding = ((game.W - 1) * stride + kernelSize - game.W) / 2;

            if (forceCpu)
                SetDevice(DeviceType.CPU);
            else
                SetDeviceAuto();

            SetDeviceAuto();
            Console.WriteLine($"--- using torch device: {Device}");

            _conv2dEntry = torch.nn.Conv2d(numInputChannels, NumChannels, kernelSize, stride, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2dEntry = torch.nn.BatchNorm2d(NumChannels);
            _conv2dEntry = _conv2dEntry.to(Device);
            _batchNorm2dEntry = _batchNorm2dEntry.to(Device);

            _conv2d1 = torch.nn.Conv2d(NumChannels, NumChannels, kernelSize, stride: 1, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2d1 = torch.nn.BatchNorm2d(NumChannels);

            _conv2d2 = torch.nn.Conv2d(NumChannels, NumChannels, kernelSize, stride: 1, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2d2 = torch.nn.BatchNorm2d(NumChannels);

            _conv2d3 = torch.nn.Conv2d(NumChannels, NumChannels, kernelSize, stride: 1, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2d3 = torch.nn.BatchNorm2d(NumChannels);

            _conv2d4 = torch.nn.Conv2d(NumChannels, NumChannels, kernelSize, stride: 1, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2d4 = torch.nn.BatchNorm2d(NumChannels);

            _conv2d5 = torch.nn.Conv2d(NumChannels, NumChannels, kernelSize, stride: 1, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2d5 = torch.nn.BatchNorm2d(NumChannels);

            _blocks.Add(new Conv2dBlock { Conv = _conv2d1, BatchNorm = _batchNorm2d1 });
            _blocks.Add(new Conv2dBlock { Conv = _conv2d2, BatchNorm = _batchNorm2d2 });
            _blocks.Add(new Conv2dBlock { Conv = _conv2d3, BatchNorm = _batchNorm2d3 });
            _blocks.Add(new Conv2dBlock { Conv = _conv2d4, BatchNorm = _batchNorm2d4 });
            _blocks.Add(new Conv2dBlock { Conv = _conv2d5, BatchNorm = _batchNorm2d5 });

            for (int i = 0; i < NumConvBlocks; i++)
                _blocks[i].ToDevice(Device);

            _fcEnd = torch.nn.Linear(NumChannels * game.W * game.H, NumChannels * game.W * game.H);
            _dropoutEnd = torch.nn.Dropout(0.5f);
            _fcEnd = _fcEnd.to(Device);
            _dropoutEnd = _dropoutEnd.to(Device);

            _fc_probs = torch.nn.Linear(NumChannels * game.W * game.H, game.ActionCount);
            _fc_v = torch.nn.Linear(NumChannels * game.W * game.H, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();

            // Move all tensors and models to the selected device
            _fc_probs = _fc_probs.to(Device);
            _fc_v = _fc_v.to(Device);
            _logSoftmax = _logSoftmax.to(Device);
            _tanh = _tanh.to(Device);

            RegisterComponents();
        }

        public override (torch.Tensor, torch.Tensor) forward(torch.Tensor x)
        {
            // Move input tensor to the same device as the model
            x = x.to(Device);

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

                x = _blocks[i].Conv.forward(x);
                x = _blocks[i].BatchNorm.forward(x);
                x = torch.nn.functional.relu(x);

                if (!isLastBlock)
                    x += risidual;
            }

            x = x.view(-1, NumChannels * _game.W * _game.H);

            x = _fcEnd.forward(x);
            x = _dropoutEnd.forward(x);

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
