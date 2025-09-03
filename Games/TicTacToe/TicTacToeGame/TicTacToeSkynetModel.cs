using AlphaSharp.Interfaces;
using TorchSharp;
using TorchSharp.Modules;

namespace TicTacToeGame
{
    /// <summary>
    /// Appropriately sized neural network for TicTacToe
    /// Much smaller than the generic model to prevent overfitting
    /// </summary>
    public class TicTacToeSkynetModel : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly Linear _fc1;
        private readonly Linear _fc2;
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

        public TicTacToeSkynetModel(IGame game, int oneHotSize, bool forceCpu = false) : base("tictactoe")
        {
            if (forceCpu)
                SetDevice(DeviceType.CPU);
            else
                SetDeviceAuto();

            Console.WriteLine($"--- TicTacToe model using torch device: {Device}");

            // Much smaller network appropriate for TicTacToe
            // Input: 18 (9 cells × 2 pieces)
            // Hidden 1: 64 neurons (reasonable for simple game)
            // Hidden 2: 32 neurons
            // Output: 9 actions + 1 value
            
            int fc1Size = 64;
            int fc2Size = 32;

            _fc1 = torch.nn.Linear(oneHotSize, fc1Size);
            _fc1.to(Device);

            _fc2 = torch.nn.Linear(fc1Size, fc2Size);
            _fc2.to(Device);

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

            // First hidden layer with ReLU
            x = _fc1.forward(x);
            x = torch.nn.functional.relu(x);

            // Second hidden layer with ReLU
            x = _fc2.forward(x);
            x = torch.nn.functional.relu(x);

            // Value head (win/loss/draw prediction)
            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            // Policy head (action probabilities)
            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
