using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JwsDetachedStreaming.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JwsDetachedStreaming.Test
{
    [TestClass]
    public class Base64UrlEncodeStreamTest
    {
        [TestMethod]
        public async Task WriteTest()
        {
            for (var size = 1; size < 4096; size++)
            {
                var payload = RandomGenerator.GenerateRndArray(size);

                await using var outputStream = new MemoryStream();
                await using (var targetStream = new Base64UrlEncodeStream(outputStream))
                {
                    await using var payloadStream = new MemoryStream(payload);
                    //await payloadStream.CopyToAsync(targetStream);

                    byte[] buffer = ArrayPool<byte>.Shared.Rent(64);
                    try
                    {
                        while (true)
                        {
                            int bytesRead = await payloadStream.ReadAsync(new Memory<byte>(buffer))
                                .ConfigureAwait(false);
                            if (bytesRead == 0) break;
                            await targetStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead))
                                .ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                var expected = Encoding.ASCII.GetBytes(Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(payload));
                var actual = outputStream.ToArray();

                Assert.IsTrue(actual.SequenceEqual(expected),
                    $"Size {size}. Expected: {BitConverter.ToString(expected)}. Actual: {BitConverter.ToString(actual)}");
            }
        }
    }
}
