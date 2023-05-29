using System;
using System.Collections.Generic;
using System.Linq;

namespace AlphaSharp
{
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
                return left == right;

            return left.SequenceEqual(right);
        }

        public int GetHashCode(byte[] key)
        {
            unchecked
            {
                var result = 0;
                foreach (byte b in key)
                    result = (result * 31) ^ b;

                return result;
            }
        }
    }
}
