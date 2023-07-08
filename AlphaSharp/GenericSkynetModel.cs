using AlphaSharp.Interfaces;
using System;
using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class GenericSkynetModel : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly Linear _fc1;
        private readonly BatchNorm1d _batch1;
        private readonly Dropout _dropout1;

        private readonly Linear _fc2;
        private readonly BatchNorm1d _batch2;
        private readonly Dropout _dropout2;

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

        public GenericSkynetModel(IGame game, int oneHotSize, bool forceCpu = false) : base("tixy")
        {
            if (forceCpu)
                SetDevice(DeviceType.CPU);
            else
                SetDeviceAuto();

            Console.WriteLine($"--- using torch device: {Device}");

            int fc1Size = oneHotSize * 5;
            int fc2Size = fc1Size / 2;

            _fc1 = torch.nn.Linear(oneHotSize, fc1Size);
            _batch1 = torch.nn.BatchNorm1d(fc1Size);
            _dropout1 = torch.nn.Dropout(0.3);
            _fc1.to(Device);
            _batch1.to(Device);
            _dropout1.to(Device);

            _fc2 = torch.nn.Linear(fc1Size, fc2Size);
            _batch2 = torch.nn.BatchNorm1d(fc2Size);
            _dropout2 = torch.nn.Dropout(0.3);
            _fc2.to(Device);
            _batch2.to(Device);
            _dropout2.to(Device);

            _fc_probs = torch.nn.Linear(fc2Size, game.ActionCount);
            _fc_v = torch.nn.Linear(fc2Size, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();

            _fc_probs = _fc_probs.to(Device);
            _fc_v = _fc_v.to(Device);
            _logSoftmax = _logSoftmax.to(Device);
            _tanh = _tanh.to(Device);

            RegisterComponents();
        }

        public override (torch.Tensor, torch.Tensor) forward(torch.Tensor x)
        {
            x = x.to(Device);

            x = _fc1.forward(x);
            x = _batch1.forward(x);
            x = torch.nn.functional.relu(x);
            x = _dropout1.forward(x);

            x = _fc2.forward(x);
            x = _batch2.forward(x);
            x = torch.nn.functional.relu(x);
            x = _dropout2.forward(x);

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
