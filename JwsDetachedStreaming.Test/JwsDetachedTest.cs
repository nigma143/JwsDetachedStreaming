using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JwsDetachedStreaming.IO;
using JwsDetachedStreaming.Test.Provision;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming.Test
{
    [TestClass]
    public class JwsDetachedTest
    {
        [TestMethod]
        public async Task CommonTest()
        {
            await using var payload = File.OpenRead("Provision/cert.pfx.test");

            await WriteReadTest(payload);
        }

        [TestMethod]
        public async Task EmptyPayloadTest()
        {
            await WriteReadTest(new MemoryStream());
        }
        
        private async Task WriteReadTest(Stream payload)
        {
            var header = new JObject { { "custom", "value" } };

            await using var output = new MemoryStream();

            await using var writer = await JwsDetachedWriter.CreateAsync(output, header, "PS256", new SignerFactory());
            payload.Position = 0;
            await payload.CopyToAsync(writer.Payload);
            await writer.Finish();

            var jwsDetached = Encoding.ASCII.GetString(output.ToArray());

            await using var reader = await JwsDetachedReader.CreateAsync(output.ToArray().AsMemory(), new VerifierFactory());
            payload.Position = 0;
            await payload.CopyToAsync(reader.Payload);

            var extractHeader = await reader.ReadAsync();

            if (extractHeader == null)
            {
                Assert.Fail("Invalid signature");
            }
            else
            {
                Assert.IsTrue(extractHeader.GetValue("custom").ToString() == "value");
            }

            {
                await using var payloadMemory = new MemoryStream();
                payload.Position = 0;
                await payload.CopyToAsync(payloadMemory);

                var encodedPayload = "";
                if (payloadMemory.Length > 0)
                {
                    encodedPayload = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(payloadMemory.ToArray());
                }

                var segments = jwsDetached.Split('.');
                var jws = segments[0] + "." +
                          encodedPayload + "." +
                          segments[2];

                new JsonWebSignatureHandler().Validate(jws,
                    new Microsoft.IdentityModel.Tokens.X509SecurityKey(new X509Certificate2("Provision/cert.pfx.test", "123456")));
            }
        }

        [TestMethod]
        public async Task MultiWriteStreamTest()
        {
            var payload = new MemoryStream(RandomGenerator.GenerateRndArray(2048));

            var header = new JObject { { "custom", "value" } };

            await using var slidingPayloadOutput = new MemoryStream();
            await using var output = new MemoryStream();
            
            await using var writer = await JwsDetachedWriter.CreateAsync(output, header, "PS256", new SignerFactory());
            
            await using var multiWriteStream = new MultiWriteStream(writer.Payload, slidingPayloadOutput);
            
            payload.Position = 0;
            await payload.CopyToAsync(multiWriteStream);
            await writer.Finish();

            var jwsDetached = Encoding.ASCII.GetString(output.ToArray());

            await using var reader = await JwsDetachedReader.CreateAsync(output.ToArray().AsMemory(), new VerifierFactory());
            payload.Position = 0;
            await payload.CopyToAsync(reader.Payload);

            var extractHeader = await reader.ReadAsync();

            if (extractHeader == null)
            {
                Assert.Fail("Invalid signature");
            }
            else
            {
                Assert.IsTrue(extractHeader.GetValue("custom").ToString() == "value");
            }
            
            Assert.IsTrue(payload.ToArray().SequenceEqual(slidingPayloadOutput.ToArray()));
        }

        class SignerFactory : ISignerFactory
        {
            private readonly X509Certificate2 _certificate;

            public SignerFactory()
            {
                _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
            }

            public Signer Create(JObject header)
            {
                return header.GetValue("alg").ToString() switch
                {
                    "PS256" => new SignerPs256(_certificate),
                    _ => throw new NotSupportedException("Signature algorithm not supported")
                };
            }
        }

        class SignerPs256 : Signer
        {
            private readonly HashAlgorithm _hashAlgorithm = SHA256.Create();

            private readonly X509Certificate2 _certificate;

            public SignerPs256(X509Certificate2 certificate)
            {
                _certificate = certificate;
            }

            public override ValueTask WriteInputAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
                {
                    _hashAlgorithm.TransformBlock(array.Array!, array.Offset, array.Count, null, 0);
                }
                else
                {
                    byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                    buffer.Span.CopyTo(sharedBuffer);
                    _hashAlgorithm.TransformBlock(sharedBuffer, 0, buffer.Length, null, 0);
                }

                return new ValueTask();
            }

            public override Task<byte[]> GetSignatureAsync(CancellationToken cancellationToken = default)
            {
                _hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                var hash = _hashAlgorithm.Hash;

                using var privateKey = _certificate.GetRSAPrivateKey();
                return Task.FromResult(privateKey.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            }

            protected override void Dispose(bool disposing)
            {
                _hashAlgorithm.Dispose();
            }
        }

        public class VerifierFactory : IVerifierFactory
        {
            private readonly X509Certificate2 _certificate;

            public VerifierFactory()
            {
                _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
            }

            public Verifier Create(JObject header)
            {
                return header.GetValue("alg").ToString() switch
                {
                    "PS256" => new VerifierPs256(_certificate),
                    _ => throw new NotSupportedException("Signature algorithm not supported")
                };
            }
        }

        class VerifierPs256 : Verifier
        {
            private readonly HashAlgorithm _hashAlgorithm = SHA256.Create();

            private readonly X509Certificate2 _certificate;

            public VerifierPs256(X509Certificate2 certificate)
            {
                _certificate = certificate;
            }

            public override ValueTask WriteInputAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
                {
                    _hashAlgorithm.TransformBlock(array.Array!, array.Offset, array.Count, null, 0);
                }
                else
                {
                    byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                    buffer.Span.CopyTo(sharedBuffer);
                    _hashAlgorithm.TransformBlock(sharedBuffer, 0, buffer.Length, null, 0);
                }

                return new ValueTask();
            }

            public override Task<bool> VerifyAsync(ReadOnlyMemory<byte> signature, CancellationToken cancellationToken = default)
            {
                _hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                var hash = _hashAlgorithm.Hash;

                using var privateKey = _certificate.GetRSAPrivateKey();
                return Task.FromResult(privateKey.VerifyHash(hash, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            }

            protected override void Dispose(bool disposing)
            {
                _hashAlgorithm.Dispose();
            }
        }
    }
}