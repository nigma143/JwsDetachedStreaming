using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JwsDetachedStreaming.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming
{
    public partial class JwsDetachedWriter : IAsyncDisposable
    {
        public Stream Payload => _payload;

        private readonly Signer _signer;
        private readonly Stream _payload;
        private readonly Stream _output;

        private readonly bool _outputLeaveOpen;

        private JwsDetachedWriter(Signer signer, Stream output, bool outputLeaveOpen)
        {
            _signer = signer;
            _payload = new Base64UrlEncodeStream(
                signer);
            _output = output;
            _outputLeaveOpen = outputLeaveOpen;
        }

        public async Task Finish(CancellationToken cancellationToken = default)
        {
            await _payload.DisposeAsync();

            var signature = await _signer.GetSignatureAsync(cancellationToken);
            var encodedSignature = Base64UrlEncoder.Encode(signature);

            await _output.WriteAsync(encodedSignature, cancellationToken);
        }
        
        private static readonly byte[] DotBytes = Encoding.UTF8.GetBytes(".");
        private static readonly byte[] DoubleDotBytes = Encoding.UTF8.GetBytes("..");

        public static async Task<JwsDetachedWriter> CreateAsync(Stream output, JObject header, 
            ISignerFactory signerFactory, CancellationToken cancellationToken = default, bool outputLeaveOpen = false)
        {
            var signer = signerFactory.Create(header);

            var encodedHeaderBytes = Base64UrlEncoder.Encode(
                Encoding.UTF8.GetBytes(
                    header.ToString(Formatting.None)));

            await signer.WriteAsync(encodedHeaderBytes, cancellationToken);
            await signer.WriteAsync(DotBytes, cancellationToken);

            await output.WriteAsync(encodedHeaderBytes, cancellationToken);
            await output.WriteAsync(DoubleDotBytes, cancellationToken);

            return new JwsDetachedWriter(signer, output, outputLeaveOpen);
        }

        public static Task<JwsDetachedWriter> CreateAsync(Stream output, JObject header, string algorithm, 
            ISignerFactory signerFactory, CancellationToken cancellationToken = default, bool outputLeaveOpen = false)
        {
            header.Add("alg", algorithm);

            return CreateAsync(output, header, signerFactory, cancellationToken, outputLeaveOpen);
        }

        public static Task<JwsDetachedWriter> CreateAsync(Stream output, string algorithm, 
            ISignerFactory signerFactory, CancellationToken cancellationToken = default, bool outputLeaveOpen = false)
        {
            return CreateAsync(output, new JObject(), algorithm, signerFactory, cancellationToken, outputLeaveOpen);
        }
        
        public async ValueTask DisposeAsync()
        {
            await _signer.DisposeAsync();
            await _payload.DisposeAsync();

            if (!_outputLeaveOpen)
            {
                await _output.DisposeAsync();
            }
        }
    }
}
