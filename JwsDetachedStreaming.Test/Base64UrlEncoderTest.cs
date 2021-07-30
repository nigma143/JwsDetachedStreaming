using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JwsDetachedStreaming.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JwsDetachedStreaming.Test
{
    [TestClass]
    public class Base64UrlEncoderTest
    {
        [TestMethod]
        public void EncodeTest()
        {
            for (var size = 1; size < 4096; size++)
            {
                var payload = RandomGenerator.GenerateRndArray(size);

                var expected = Encoding.ASCII.GetBytes(Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(payload));
                var actual = Base64UrlEncoder.Encode(payload);

                Assert.IsTrue(actual.Span.SequenceEqual(expected),
                    $"Size {size}. Expected: {BitConverter.ToString(expected)}. Actual: {BitConverter.ToString(actual.ToArray())}");
            }
        }

        [TestMethod]
        public void DecodeTest()
        {
            for (var size = 1; size < 4096; size++)
            {
                var payload = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(RandomGenerator.GenerateRndArray(size));

                var expected = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(payload);
                var actual = Base64UrlEncoder.Decode(Encoding.UTF8.GetBytes(payload));

                Assert.IsTrue(actual.Span.SequenceEqual(expected),
                    $"Size {size}. Expected: {BitConverter.ToString(expected)}. Actual: {BitConverter.ToString(actual.ToArray())}");
            }
        }

        [TestMethod]
        public void EncodeToStringTest()
        {
            for (var size = 1; size < 4096; size++)
            {
                var payload = RandomGenerator.RandomString(size);

                var expected = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(payload);
                var actual = Base64UrlEncoder.EncodeToString(payload);

                Assert.IsTrue(actual.SequenceEqual(expected),
                    $"Size {size}. Expected: {expected}. Actual: {actual}");
            }
        }

        [TestMethod]
        public void DecodeToStringTest()
        {
            for (var size = 1; size < 4096; size++)
            {
                var payload = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(RandomGenerator.RandomString(size));

                var expected = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode(payload);
                var actual = Base64UrlEncoder.DecodeToString(payload);

                Assert.IsTrue(actual.SequenceEqual(expected),
                    $"Size {size}. Expected: {expected}. Actual: {actual}");
            }
        }
        
        [TestMethod]
        public void PerfComparison()
        {
            var maxSize = 1024;
            var iterationCount = 8;

            var timer1 = Stopwatch.StartNew();
            
            for (int size = 1; size < maxSize; size++)
            {
                for (var iteration = 0; iteration < iterationCount; iteration++)
                {
                    var expected = RandomGenerator.GenerateRndArray(size);

                    var encodedInput = Base64UrlEncoder.Encode(expected);
                    var actual = Base64UrlEncoder.Decode(encodedInput.Span);

                    Assert.IsTrue(actual.Span.SequenceEqual(expected));
                }
            }

            timer1.Stop();
            Console.WriteLine(timer1.Elapsed);

            var timer2 = Stopwatch.StartNew();

            for (int size = 1; size < maxSize; size++)
            {
                for (var iteration = 0; iteration < iterationCount; iteration++)
                {
                    var expected = RandomGenerator.GenerateRndArray(size);

                    var encodedInput =
                        Encoding.ASCII.GetBytes(Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(expected));
                    var actual =
                        Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(
                            Encoding.ASCII.GetString(encodedInput));

                    Assert.IsTrue(expected.SequenceEqual(actual));
                }
            }

            timer2.Stop();

            Console.WriteLine(timer2.Elapsed);
        }

        
    }
}
