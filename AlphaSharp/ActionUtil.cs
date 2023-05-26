using System;

namespace AlphaSharp
{
    public static class ActionUtil
    {
        public static int PickRandomAction(byte[] validActions)
        {
            int nonZeroCount = ArrayUtil.CountNonZero(validActions);
            int selectedNo = Random.Shared.Next(0, nonZeroCount);
            return ArrayUtil.FindNthNonZeroIndex(validActions, selectedNo + 1);
        }
    }
}
