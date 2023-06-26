using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModel : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly Conv2d _conv1;
        private readonly Conv2d _conv2;
        private readonly Conv2d _conv3;
        private readonly Conv2d _conv4;
        private readonly Conv2d _conv5;
        private readonly Conv2d _conv6;
        private readonly Conv2d _conv7;

        private readonly BatchNorm2d _bn2d1;
        private readonly BatchNorm2d _bn2d2;
        private readonly BatchNorm2d _bn2d3;
        private readonly BatchNorm2d _bn2d4;
        private readonly BatchNorm2d _bn2d5;
        private readonly BatchNorm2d _bn2d6;
        private readonly BatchNorm2d _bn2d7;

        private readonly Linear _fc_probs;
        private readonly Linear _fc_v;

        private readonly LogSoftmax _logSoftmax;
        private readonly Tanh _tanh;

        const float DropOut = 0.3f;
        const int size1 = 1024;
        const int size2 = 256;
        const int size3 = 512;
        const int size4 = 512;
        const int size5 = 512;

        const int NumChannels = 128;

        public TixySkynetModel(int inputSize, int outputSize) : base("tixy")
        {
            _conv1 = torch.nn.Conv2d(8, NumChannels, kernelSize: 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros, bias: false);
            _conv2 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros, bias: false);
            _conv3 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros, bias: false);
            _conv4 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros, bias: false);
            _conv5 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros, bias: false);
            _conv6 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros, bias: false);
            _conv7 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros, bias: false);

            _bn2d1 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d2 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d3 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d4 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d5 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d6 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d7 = torch.nn.BatchNorm2d(NumChannels);

            _fc_probs = torch.nn.Linear(NumChannels * 5 * 5, outputSize);
            _fc_v = torch.nn.Linear(NumChannels * 5 * 5, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();

            RegisterComponents();
        }

        public override (torch.Tensor, torch.Tensor) forward(torch.Tensor x)
        {
            x = x.view(x.shape[0], 8, 5, 5); // hardcoded board size

            x = _conv1.forward(x);
            x = _bn2d1.forward(x);
            x = torch.nn.functional.relu(x);

            var residual = x;

            x = _conv2.forward(x);
            x = _bn2d2.forward(x);
            x = torch.nn.functional.relu(x);

            x = _conv3.forward(x);
            x = _bn2d3.forward(x);
            x = torch.nn.functional.relu(x);

            x += residual;
            residual = x;

            x = _conv4.forward(x);
            x = _bn2d4.forward(x);
            x = torch.nn.functional.relu(x);

            x = _conv5.forward(x);
            x = _bn2d5.forward(x);
            x = torch.nn.functional.relu(x);

            x += residual;

            x = _conv6.forward(x);
            x = _bn2d6.forward(x);
            x = torch.nn.functional.relu(x);

            x = _conv7.forward(x);
            x = _bn2d7.forward(x);
            x = torch.nn.functional.relu(x);

            x = x.view(-1, NumChannels * 5 * 5);

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
