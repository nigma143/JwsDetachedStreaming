using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JwsDetachedStreaming.IO;
using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming
{
    public class JwsDetachedReader : IAsyncDisposable
    {
        public Stream Payload => _payload;

        private readonly Verifier _verifier;
        private readonly Stream _payload;
        private readonly JObject _header;
        private readonly ReadOnlyMemory<byte> _signature;
        
        private JwsDetachedReader(Verifier verifier, JObject header, ReadOnlyMemory<byte> signature)
        {
            _verifier = verifier;
            _header = header;
            _payload = new Base64UrlEncodeStream(
                verifier);
            _signature = signature;
        }

        public async Task<JObject?> ReadAsync(CancellationToken cancellationToken = default)
        {
            await _payload.DisposeAsync();

            var verify = await _verifier.VerifyAsync(_signature, cancellationToken);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!verify)
            {
                return null;
            }

            return _header;
        }

        private static readonly byte[] DotBytes = Encoding.UTF8.GetBytes(".");
        private static readonly byte[] DoubleDotBytes = Encoding.UTF8.GetBytes("..");

        public static async Task<JwsDetachedReader> CreateAsync(ReadOnlyMemory<byte> jwsDetached,
            IVerifierFactory verifierFactory, CancellationToken cancellationToken = default)
        {
            var payloadStartIndex = jwsDetached.Span.IndexOf(DoubleDotBytes);
            if (payloadStartIndex == -1)
            {
                throw new FormatException("Expected three segments with detached payload");
            }
            
            var encodedHeaderBytes = jwsDetached.Slice(0, payloadStartIndex);
            var encodedSignature = jwsDetached.Slice(payloadStartIndex + DoubleDotBytes.Length);
            
            var header = JObject.Parse(
                    Base64UrlEncoder.DecodeToString(encodedHeaderBytes.Span));
            
            var verifier = verifierFactory.Create(header);

            // part[1] is detached payload

            var signature = Base64UrlEncoder.Decode(encodedSignature.Span);
            
            await verifier.WriteAsync(encodedHeaderBytes, cancellationToken);
            await verifier.WriteAsync(DotBytes, cancellationToken);
            
            return new JwsDetachedReader(verifier, header, signature);
        }
        
        public async ValueTask DisposeAsync()
        {
            await _payload.DisposeAsync();
            await _verifier.DisposeAsync();
        }
    }
}
