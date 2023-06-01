using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModel : torch.nn.Module
    {
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

        public TixySkynetModel(int inputSize, int outputSize, bool isTraining) : base("tixy")
        {
            const float DropOut = 0.3f;

            _fc1 = torch.nn.Linear(inputSize, 1024);
            _fc1.train(isTraining);

            _bn1 = torch.nn.BatchNorm1d(1024);
            _bn1.train(false);

            _drop1 = torch.nn.Dropout(DropOut);
            _drop1.train(false);

            _fc2 = torch.nn.Linear(1024, 512);
            _fc2.train(false);

            _bn2 = torch.nn.BatchNorm1d(512);
            _bn2.train(false);

            _drop2 = torch.nn.Dropout(DropOut);
            _drop2.train(false);

            _fc3 = torch.nn.Linear(512, outputSize);
            _fc3.train(false);

            _fc4 = torch.nn.Linear(512, 1);
            _fc4.train(false);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();
        }

        public torch.Tensor Forward(torch.Tensor x, out float v)
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
            return _logSoftmax.forward(probs);
        }
    }
}
