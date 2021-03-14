using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JwsDetachedStreaming.Test
{
    [TestClass]
    public class Base64UrlEncoderTest
    {
        [TestMethod]
        public void LikeFuzzing()
        {
            Parallel.For(0, 128, (size, state) =>
            {
                for (var iteration = 0; iteration < 5096; iteration++)
                {
                    var expected = GenerateRndArray(size);

                    try
                    {
                        var encodedInput = Base64UrlEncoder.Encode(expected);
                        var actual = Base64UrlEncoder.Decode(encodedInput);

                        Assert.IsTrue(expected.SequenceEqual(actual),
                            $"Size/Iteration {size}/{iteration}. Expected: {BitConverter.ToString(expected)}. Actual: {BitConverter.ToString(actual)}");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Size/Iteration {size}/{iteration}. Expected: {BitConverter.ToString(expected)}");
                        throw;
                    }
                }
            });
        }

        private static readonly Random Random = new Random();

        private static byte[] GenerateRndArray(int size)
        {
            var buf = new byte[size];
            Random.NextBytes(buf);

            return buf;
        }
    }
}
