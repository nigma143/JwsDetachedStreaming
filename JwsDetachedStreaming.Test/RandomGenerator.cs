using System;
using System.Linq;

namespace JwsDetachedStreaming.Test
{
    public static class RandomGenerator
    {
        private static readonly Random Random = new Random();

        public static byte[] GenerateRndArray(int size)
        {
            var buf = new byte[size];
            Random.NextBytes(buf);

            return buf;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}
