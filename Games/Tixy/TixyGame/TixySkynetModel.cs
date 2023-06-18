using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModel : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly Linear _lin1;
        private readonly BatchNorm1d _bn1;
        private readonly ReLU _relu1;
        private readonly Dropout _drop1;

        private readonly Linear _lin2;
        private readonly BatchNorm1d _bn2;
        private readonly ReLU _relu2;
        private readonly Dropout _drop2;

        private readonly Linear _lin3;
        private readonly BatchNorm1d _bn3;
        private readonly ReLU _relu3;
        private readonly Dropout _drop3;

        private readonly Linear _fc_probs;
        private readonly Linear _fc_v;

        private readonly LogSoftmax _logSoftmax;
        private readonly Tanh _tanh;

        const float DropOut = 0.5f;
        const int size1 = 512;
        const int size2 = 256;
        const int size3 = 0;

        public TixySkynetModel(int inputSize, int outputSize) : base("tixy")
        {
            _lin1 = torch.nn.Linear(inputSize, size1);
            _bn1 = torch.nn.BatchNorm1d(size1);
            _relu1 = torch.nn.ReLU();
            _drop1 = torch.nn.Dropout(DropOut);

            _lin2 = torch.nn.Linear(size1, size2);
            _bn2 = torch.nn.BatchNorm1d(size2);
            _relu2 = torch.nn.ReLU();
            _drop2 = torch.nn.Dropout(DropOut);

            _lin3 = torch.nn.Linear(size2, size3);
            _bn3 = torch.nn.BatchNorm1d(size3);
            _relu3 = torch.nn.ReLU();
            _drop3 = torch.nn.Dropout(DropOut);

            int lastSize = size3 > 0 ? size3 : size2;
            _fc_probs = torch.nn.Linear(lastSize, outputSize);
            _fc_v = torch.nn.Linear(lastSize, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();

            RegisterComponents();
        }

        public override (torch.Tensor, torch.Tensor) forward(torch.Tensor x)
        {
            x = _drop1.forward(_relu1.forward(_bn1.forward(_lin1.forward(x))));
            x = _drop2.forward(_relu2.forward(_bn2.forward(_lin2.forward(x))));
            if (size3 > 0)
                x = _drop3.forward(_relu3.forward(_bn3.forward(_lin3.forward(x))));

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
