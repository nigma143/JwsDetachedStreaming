using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming
{
    public class JwsDetachedHandler
    {
        public string Write(string algorithm, ISignerResolver signerResolver, Stream payloadStream)
        {
            return Write(new JObject(), algorithm, signerResolver, payloadStream);
        }

        public string Write(JObject header, string algorithm, ISignerResolver signerResolver, Stream payloadStream)
        {
            header.Add("alg", algorithm);
            
            var encodedHeaderBytes = Base64UrlEncoder.Encode(
                Encoding.UTF8.GetBytes(
                    header.ToString(Formatting.None)));

            using var encodedPayloadStream = new SuppressDisposeExceptionCryptoStream(
                payloadStream,
                new ToBase64UrlTransform(),
                CryptoStreamMode.Read,
                leaveOpen: true);
            
            var signer = signerResolver.Resolve(header);

            var signature = signer.Sign(
                new CompositeReadStream(
                    new[] {encodedHeaderBytes, new byte[]{ 0xE2 }},//E2 - dot byte
                    encodedPayloadStream,
                    leaveOpen: true));

            var encodedSignature = Base64UrlEncoder.Encode(signature);

            return String.Concat(
                Encoding.UTF8.GetString(encodedHeaderBytes),
                "..",
                Encoding.UTF8.GetString(encodedSignature));
        }

        public JObject? Read(string jwsDetached, IVerifierResolver verifierResolver, Stream payloadStream)
        {
            var parts = jwsDetached.Split('.');
            if (parts.Length != 3)
            {
                throw new FormatException("Expected three segments");
            }

            var encodedHeaderBytes = Encoding.UTF8.GetBytes(parts[0]);

            var header = JObject.Parse(
                Encoding.UTF8.GetString(
                    Base64UrlEncoder.Decode(encodedHeaderBytes)));
            // part[1] is detached payload
            var signature = Base64UrlEncoder.DecodeFromString(parts[2]);

            using var encodedPayloadStream = new SuppressDisposeExceptionCryptoStream(
                payloadStream,
                new ToBase64UrlTransform(),
                CryptoStreamMode.Read,
                leaveOpen: true);

            var verifier = verifierResolver.Resolve(header);

            var verify = verifier.Verify(
                new CompositeReadStream(
                    new[] {encodedHeaderBytes, new byte[] {0xE2}}, //E2 - dot byte
                    encodedPayloadStream,
                    leaveOpen: true),
                signature);

            if (!verify)
            {
                return null;
            }

            return header;
        }
    }

    class SuppressDisposeExceptionCryptoStream : CryptoStream
    {
        public SuppressDisposeExceptionCryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode) : base(stream, transform, mode)
        {
        }

        public SuppressDisposeExceptionCryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode, bool leaveOpen) : base(stream, transform, mode, leaveOpen)
        {
        }

        public override ValueTask DisposeAsync()
        {
            try
            {
                return base.DisposeAsync();
            }
            catch
            {
                // ignored
                // Inner stream may be throw NotSupportedException at Flush, for example FileBufferingReadStream

                return default;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            catch
            {
                // ignored
                // Inner stream may be throw NotSupportedException at Flush, for example FileBufferingReadStream
            }
        }
    }
}
