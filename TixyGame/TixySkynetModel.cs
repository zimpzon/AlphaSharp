using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModel : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly Linear _fc3;
        private readonly Linear _fc4;

        private readonly Sequential _seq;

        private readonly LogSoftmax _logSoftmax;
        private readonly Tanh _tanh;

        public TixySkynetModel(int inputSize, int outputSize) : base("tixy")
        {
            const float DropOut = 0.3f;
            const int size1 = 1024;
            const int size2 = 512;

            _seq = torch.nn.Sequential(
                ("lin1", torch.nn.Linear(inputSize, size1)),
                ("bn1", torch.nn.BatchNorm1d(size1)),
                ("relu1", torch.nn.ReLU()),
                ("drop1", torch.nn.Dropout(DropOut)),

                ("lin2", torch.nn.Linear(size1, size2)),
                ("bn2", torch.nn.BatchNorm1d(size2)),
                ("relu2", torch.nn.ReLU()),
                ("drop2", torch.nn.Dropout(DropOut)));

            _fc3 = torch.nn.Linear(512, outputSize);
            _fc4 = torch.nn.Linear(512, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();

            SetIsTraining(false);

            RegisterComponents();
        }

        public void SetIsTraining(bool isTraining)
        {
            _seq.train(isTraining);
            _fc3.train(isTraining);
            _fc4.train(isTraining);
        }

        public override (torch.Tensor, torch.Tensor) forward(torch.Tensor x)
        {
            x = _seq.forward(x);

            var value = _fc4.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc3.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
