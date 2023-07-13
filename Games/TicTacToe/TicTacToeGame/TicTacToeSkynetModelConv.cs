﻿using AlphaSharp.Interfaces;
using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TicTacToeSkynetModelConv : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly Conv2d _conv2dEntry;
        private readonly BatchNorm2d _batchNorm2dEntry;

        private readonly IGame _game;
        private readonly int _numInputChannels;

        private readonly Linear _fcEnd1;
        private readonly BatchNorm1d _batchEnd1;

        private readonly Linear _fcEnd2;
        private readonly BatchNorm1d _batchEnd2;

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

        const int NumChannels = 16;

        public TicTacToeSkynetModelConv(IGame game, int numInputChannels, bool forceCpu = false) : base("tixy")
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

            Console.WriteLine($"--- using torch device: {Device}");

            _conv2dEntry = torch.nn.Conv2d(numInputChannels, NumChannels, kernelSize, stride, padding, dilation: 1, PaddingModes.Zeros, bias: false);
            _batchNorm2dEntry = torch.nn.BatchNorm2d(NumChannels);
            _conv2dEntry = _conv2dEntry.to(Device);
            _batchNorm2dEntry = _batchNorm2dEntry.to(Device);

            _fcEnd1 = torch.nn.Linear(NumChannels * game.W * game.H, 256);
            _batchEnd1 = torch.nn.BatchNorm1d(256);
            _fcEnd1 = _fcEnd1.to(Device);
            _batchEnd1 = _batchEnd1.to(Device);

            _fcEnd2 = torch.nn.Linear(256, 128);
            _batchEnd2 = torch.nn.BatchNorm1d(128);
            _fcEnd2 = _fcEnd2.to(Device);
            _batchEnd2 = _batchEnd2.to(Device);

            _fc_probs = torch.nn.Linear(128, game.ActionCount);
            _fc_v = torch.nn.Linear(128, 1);

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

            x = x.view(-1, NumChannels * _game.W * _game.H);

            x = _fcEnd1.forward(x);
            x = _batchEnd1.forward(x);
            x = torch.nn.functional.relu(x);

            x = _fcEnd2.forward(x);
            x = _batchEnd2.forward(x);
            x = torch.nn.functional.relu(x);

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
