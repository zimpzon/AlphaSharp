using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    internal class EpisodeParam
    {
        public IGame Game { get; set; }
        public ISkynet Skynet { get; set; }
        public AlphaParameters Args { get; set; }
        public int Episode { get; set; }
    }
}
