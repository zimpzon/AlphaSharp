using AlphaSharp.Interfaces;
using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModelFc : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly IGame _game;
        private readonly int _numInputChannels;

        private readonly Linear _fc1;
        private readonly BatchNorm1d _batch1;
        private readonly Dropout _dropout1;

        private readonly Linear _fc2;
        private readonly BatchNorm1d _batch2;
        private readonly Dropout _dropout2;

        private readonly Linear _fc3;
        private readonly BatchNorm1d _batch3;
        private readonly Dropout _dropout3;

        private readonly Linear _fc4;
        private readonly BatchNorm1d _batch4;
        private readonly Dropout _dropout4;

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

        public TixySkynetModelFc(IGame game, int numInputChannels, bool forceCpu = false) : base("tixy")
        {
            _game = game;
            _numInputChannels = numInputChannels;

            if (forceCpu)
                SetDevice(DeviceType.CPU);
            else
                SetDeviceAuto();

            Console.WriteLine($"--- using torch device: {Device}");

            const int fc1 = 512;
            const int fc2 = 256;

            _fc1 = torch.nn.Linear(numInputChannels * _game.W * _game.H, fc1);
            _batch1 = torch.nn.BatchNorm1d(fc1);
            _dropout1 = torch.nn.Dropout(0.5);
            _fc1.to(Device);
            _batch1.to(Device);
            _dropout1.to(Device);

            _fc2 = torch.nn.Linear(fc1, fc1);
            _batch2 = torch.nn.BatchNorm1d(fc1);
            _dropout2 = torch.nn.Dropout(0.5);
            _fc2.to(Device);
            _batch2.to(Device);
            _dropout2.to(Device);

            _fc3 = torch.nn.Linear(fc1, fc1);
            _batch3 = torch.nn.BatchNorm1d(fc1);
            _dropout3 = torch.nn.Dropout(0.5);
            _fc3.to(Device);
            _batch3.to(Device);
            _dropout3.to(Device);

            _fc4 = torch.nn.Linear(fc1, fc2);
            _batch4 = torch.nn.BatchNorm1d(fc2);
            _dropout4 = torch.nn.Dropout(0.5);
            _fc4.to(Device);
            _batch4.to(Device);
            _dropout4.to(Device);

            _fc_probs = torch.nn.Linear(fc2, game.ActionCount);
            _fc_v = torch.nn.Linear(fc2, 1);

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
            // Move input tensor to the same device as the model
            x = x.to(Device);

            x = _fc1.forward(x);
            x = _batch1.forward(x);
            x = torch.nn.functional.relu(x);
            x = _dropout1.forward(x);

            x = _fc2.forward(x);
            x = _batch2.forward(x);
            x = torch.nn.functional.relu(x);
            x = _dropout2.forward(x);

            x = _fc3.forward(x);
            x = _batch3.forward(x);
            x = torch.nn.functional.relu(x);
            x = _dropout3.forward(x);

            x = _fc4.forward(x);
            x = _batch4.forward(x);
            x = torch.nn.functional.relu(x);
            x = _dropout4.forward(x);

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
