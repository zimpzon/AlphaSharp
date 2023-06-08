using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    internal class EvalParam
    {
        public bool NewModelIsPlayer1 { get; set; }
        public IGame Game { get; set; }
        public ISkynet Skynet { get; set; }
        public ISkynet EvaluationSkynet { get; set; }
        public AlphaParameters Args { get; set; }
    }
}
