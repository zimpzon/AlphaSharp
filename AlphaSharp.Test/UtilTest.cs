using Xunit;

namespace AlphaSharp.Test
{
    public class UtilTest
    {
        [Fact]
        public void RandomAction()
        {
            var arr = new byte[10];
            arr[0] = 1;
            arr[9] = 1;

            Assert.Equal(2, Util.CountNonZero(arr));
            Assert.Equal(0, Util.FindNthNonZeroIndex(arr, 1));
            Assert.Equal(9, Util.FindNthNonZeroIndex(arr, 2));

            arr[0] = 0;
            Assert.Equal(9, ActionUtil.PickRandomNonZeroAction(arr));
            arr[0] = 1;
            arr[9] = 0;
            Assert.Equal(0, ActionUtil.PickRandomNonZeroAction(arr));
        }
    }
}