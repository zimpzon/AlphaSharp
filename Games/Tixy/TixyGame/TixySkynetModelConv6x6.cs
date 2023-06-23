﻿using TorchSharp;
using TorchSharp.Modules;

namespace TixyGame
{
    public class TixySkynetModelConv6x6 : torch.nn.Module<torch.Tensor, (torch.Tensor, torch.Tensor)>
    {
        private readonly Conv2d _conv1;
        private readonly Conv2d _conv2;
        private readonly Conv2d _conv3;

        private readonly BatchNorm2d _bn2d1;
        private readonly BatchNorm2d _bn2d2;
        private readonly BatchNorm2d _bn2d3;

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

        private readonly Linear _lin4;
        private readonly BatchNorm1d _bn4;
        private readonly ReLU _relu4;
        private readonly Dropout _drop4;

        private readonly Linear _lin5;
        private readonly BatchNorm1d _bn5;
        private readonly ReLU _relu5;
        private readonly Dropout _drop5;

        private readonly Linear _fc_probs;
        private readonly Linear _fc_v;

        private readonly LogSoftmax _logSoftmax;
        private readonly Tanh _tanh;

        const float DropOut = 0.5f;
        const int size1 = 512;
        const int size2 = 1024;
        const int size3 = 512;
        const int size4 = 512;
        const int size5 = 512;

        const int NumChannels = 128;

        public TixySkynetModelConv6x6(int inputSize, int outputSize) : base("tixy")
        {
            _conv1 = torch.nn.Conv2d(8, NumChannels, kernelSize: 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros); // -> 6
            _conv2 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 1, dilation: 1, PaddingModes.Zeros); // -> 5
            _conv3 = torch.nn.Conv2d(NumChannels, NumChannels, 3, stride: 1, padding: 0, dilation: 1, PaddingModes.Zeros); // -> 4

            _bn2d1 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d2 = torch.nn.BatchNorm2d(NumChannels);
            _bn2d3 = torch.nn.BatchNorm2d(NumChannels);

            _lin1 = torch.nn.Linear(NumChannels * 4 * 4, 1024);
            _bn1 = torch.nn.BatchNorm1d(1024);
            _drop1 = torch.nn.Dropout(DropOut);

            _lin2 = torch.nn.Linear(1024, 512);
            _bn2 = torch.nn.BatchNorm1d(512);
            _drop2 = torch.nn.Dropout(DropOut);

            _fc_probs = torch.nn.Linear(512, outputSize);
            _fc_v = torch.nn.Linear(512, 1);


            //_bn1 = torch.nn.BatchNorm1d(size1);
            //_relu1 = torch.nn.ReLU();
            //_drop1 = torch.nn.Dropout(DropOut);

            //_lin2 = torch.nn.Linear(size1, size2);
            //_bn2 = torch.nn.BatchNorm1d(size2);
            //_relu2 = torch.nn.ReLU();
            //_drop2 = torch.nn.Dropout(DropOut);

            //_lin3 = torch.nn.Linear(size2, size3);
            //_bn3 = torch.nn.BatchNorm1d(size3);
            //_relu3 = torch.nn.ReLU();
            //_drop3 = torch.nn.Dropout(DropOut);

            //_lin4 = torch.nn.Linear(size3, size4);
            //_bn4 = torch.nn.BatchNorm1d(size4);
            //_relu4 = torch.nn.ReLU();
            //_drop4 = torch.nn.Dropout(DropOut);

            //_lin5 = torch.nn.Linear(size4, size5);
            //_bn5 = torch.nn.BatchNorm1d(size5);
            //_relu5 = torch.nn.ReLU();
            //_drop5 = torch.nn.Dropout(DropOut);

            //_fc_probs = torch.nn.Linear(size2, outputSize);
            //_fc_v = torch.nn.Linear(size2, 1);

            _logSoftmax = torch.nn.LogSoftmax(1);
            _tanh = torch.nn.Tanh();

            RegisterComponents();
        }

        public override (torch.Tensor, torch.Tensor) forward(torch.Tensor x)
        {
            x = x.view(x.shape[0], 8, 6, 6); // hardcoded for 6x6 board

            //x = _drop1.forward(_relu1.forward(_bn1.forward(_lin1.forward(x))));
            //x = _drop2.forward(_relu2.forward(_bn2.forward(_lin2.forward(x))));
            //x = _drop3.forward(_relu3.forward(_bn3.forward(_lin3.forward(x))));
            //x = _drop4.forward(_relu4.forward(_bn4.forward(_lin4.forward(x))));
            //x = _drop5.forward(_relu5.forward(_bn5.forward(_lin5.forward(x))));

            x = _conv1.forward(x);
            x = _bn2d1.forward(x);
            x = torch.nn.functional.relu(x);

            x = _conv2.forward(x);
            x = _bn2d2.forward(x);
            x = torch.nn.functional.relu(x);

            x = _conv3.forward(x);
            x = _bn2d3.forward(x);
            x = torch.nn.functional.relu(x);

            x = x.view(-1, NumChannels * 4 * 4);

            x = _lin1.forward(x);
            x = _bn1.forward(x);
            x = torch.nn.functional.relu(x);
            x = _drop1.forward(x);

            x = _lin2.forward(x);
            x = _bn2.forward(x);
            x = torch.nn.functional.relu(x);
            x = _drop2.forward(x);

            var value = _fc_v.forward(x);
            var v = _tanh.forward(value);

            var probs = _fc_probs.forward(x);
            return (_logSoftmax.forward(probs), v);
        }
    }
}
