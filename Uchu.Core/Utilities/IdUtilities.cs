using System;

namespace Uchu.Core
{
    public static class IdUtilities
    {
        private static readonly Random Random = new Random();

        private static long RandomLong(long min, long max)
        {
            var buf = new byte[8];

            Random.NextBytes(buf);

            var res = BitConverter.ToInt64(buf, 0);

            return Math.Abs(res % (max - min)) + min;
        }

        public static long GenerateObjectId() => RandomLong(1000000000000000000, 1999999999999999999);
    }
}