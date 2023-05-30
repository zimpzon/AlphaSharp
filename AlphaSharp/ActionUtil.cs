﻿using System;
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
            var candidateCount = actions.Count(a => a.VisitCount == maxVisitCount);
            int selectedCandiate = rnd.Next(0, candidateCount);

            int counter = 0;
            for (int i = 0; i < actions.Length; ++i)
            {
                if (actions[i].VisitCount == maxVisitCount)
                {
                    if (counter++ == selectedCandiate)
                        return i;
                }
            }
            return -1;
        }
    }
}
