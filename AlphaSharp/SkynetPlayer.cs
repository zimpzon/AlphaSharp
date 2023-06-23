using AlphaSharp.Interfaces;
using System;

namespace AlphaSharp
{
    public class SkynetPlayer : IPlayer
    {
        public string Name { get; }

        private readonly ISkynet _skynet;
        private readonly IGame _game;
        private readonly Random _rnd = new ();
        private readonly float[] _probs;
        private readonly byte[] _validActions;

        public SkynetPlayer(string name, IGame game, ISkynet skynet, AlphaParameters _)
        {
            Name = name;
            _skynet = skynet;
            _game = game;
            _probs = new float[game.ActionCount];
            _validActions = new byte[game.ActionCount];
        }

        public int PickAction(byte[] state, int playerTurn)
        {
            _game.GetValidActions(state, _validActions);
            _skynet.Suggest(state, _probs, out _);
            Util.FilterProbsByValidActions(_probs, _validActions);
            Util.Normalize(_probs);
            return Util.WeightedChoice(_rnd, _probs);
        }
    }
}
