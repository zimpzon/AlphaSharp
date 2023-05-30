using System;
using System.Linq;

namespace AlphaSharp
{
    public static class ActionUtil
    {
        public static int PickRandomNonZeroAction(byte[] validActions)
        {
            int nonZeroCount = ArrayUtil.CountNonZero(validActions);
            int selectedNo = Random.Shared.Next(0, nonZeroCount);
            return ArrayUtil.FindNthNonZeroIndex(validActions, selectedNo + 1);
        }

        public static int PickActionByHighestVisitCount(StateNode.Action[] actions)
        {
            var rnd = new Random();
            int maxVisitCount = actions.Max(a => a.VisitCount);
            var candidates = actions.Where(a => a.VisitCount == maxVisitCount).ToList();
            return rnd.Next(0, candidates.Count);
        }
    }
}
