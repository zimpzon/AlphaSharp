//using AlphaSharp;
//using Torch;
//using TorchSharp;
//using TorchSharp.Tensor;

//namespace TixyGame
//{
//    public class TixyNet : TorchModel
//    {
//        private readonly int numChannels;
//        private readonly int boardX;
//        private readonly int boardY;
//        private readonly int actionSize;

//        private readonly TorchModule conv1;
//        private readonly TorchModule conv2;
//        private readonly TorchModule conv3;
//        private readonly TorchModule conv4;
//        private readonly TorchModule bn1;
//        private readonly TorchModule bn2;
//        private readonly TorchModule bn3;
//        private readonly TorchModule bn4;
//        private readonly TorchModule fc1;
//        private readonly TorchModule fc_bn1;
//        private readonly TorchModule fc2;
//        private readonly TorchModule fc_bn2;
//        private readonly TorchModule fc3;
//        private readonly TorchModule fc4;

//        public MyNet(int numChannels, int boardX, int boardY, int actionSize)
//        {
//            this.numChannels = numChannels;
//            this.boardX = boardX;
//            this.boardY = boardY;
//            this.actionSize = actionSize;

//            conv1 = new TorchModule("nn.Conv2d", new object[] { 8, numChannels, 3, 1, 1 });
//            conv2 = new TorchModule("nn.Conv2d", new object[] { numChannels, numChannels, 3, 1, 1 });
//            conv3 = new TorchModule("nn.Conv2d", new object[] { numChannels, numChannels, 3, 1, 1 });
//            conv4 = new TorchModule("nn.Conv2d", new object[] { numChannels, numChannels, 3, 1 });

//            bn1 = new TorchModule("nn.BatchNorm2d", new object[] { numChannels });
//            bn2 = new TorchModule("nn.BatchNorm2d", new object[] { numChannels });
//            bn3 = new TorchModule("nn.BatchNorm2d", new object[] { numChannels });
//            bn4 = new TorchModule("nn.BatchNorm2d", new object[] { numChannels });

//            int flattenSize = numChannels * (boardX - 2) * (boardY - 2);

//            fc1 = new TorchModule("nn.Linear", new object[] { flattenSize, 1024 });
//            fc_bn1 = new TorchModule("nn.BatchNorm1d", new object[] { 1024 });

//            fc2 = new TorchModule("nn.Linear", new object[] { 1024, 512 });
//            fc_bn2 = new TorchModule("nn.BatchNorm1d", new object[] { 512 });

//            fc3 = new TorchModule("nn.Linear", new object[] { 512, actionSize });

//            fc4 = new TorchModule("nn.Linear", new object[] { 512, 1 });
//        }

//        public (TorchTensor, TorchTensor) Forward(TorchTensor x)
//        {
//            var h = x;

//            h = conv1.forward(h);
//            h = bn1.forward(h);
//            h = TorchSharp.Functional.relu(h);

//            h = conv2.forward(h);
//            h = bn2.forward(h);
//            h = TorchSharp.Functional.relu(h);

//            h = conv3.forward(h);
//            h = bn3.forward(h);
//            h = TorchSharp.Functional.relu(h);

//            h = conv4.forward(h);
//            h = bn4.forward(h);
//            h = TorchSharp.Functional.relu(h);

//            var s = h.Shape;
//            h = h.view(new long[] { s[0], -1 });

//            h = fc1.forward(h);
//            h = fc_bn1.forward(h);
//            h = Torch
//        }
//    }
