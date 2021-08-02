using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JwsDetachedStreaming.Test.Provision
{
    public class JsonWebSignatureHandler
    {
        public string[]? ValidateSupportedAlgorithms { get; set; }

        public string Write(SigningCredentials signingCredentials, byte[] payload, bool onlyAlgHeader = true)
        {
            var header = new JwtHeader(signingCredentials);

            if (onlyAlgHeader)
            {
                foreach (var key in header.Keys.Where(key => key != "alg"))
                {
                    header.Remove(key);
                }
            }

            var encodedPayload = Base64UrlEncoder.Encode(payload);
            var encodedHeader = header.Base64UrlEncode();

            string encodedSignature = String.Empty;
            if (signingCredentials != null)
            {
                encodedSignature = JwtTokenUtilities.CreateEncodedSignature(
                    String.Concat(encodedHeader, ".", encodedPayload), signingCredentials);
            }

            return String.Concat(encodedHeader, ".", encodedPayload, ".", encodedSignature);
        }

        public void Validate(string jws, SecurityKey issuerSigningKey)
        {
            var parts = jws.Split('.');
            if (parts.Length != 3)
                throw new FormatException("Jws expected three segments");

            var headerRaw = parts[0];
            var header = JwtHeader.Base64UrlDeserialize(headerRaw);
            var payloadRaw = parts[1];
            var signature = parts[2];

            if (ValidateSupportedAlgorithms != default)
            {
                if (!ValidateSupportedAlgorithms.Contains(header.Alg, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new NotSupportedException(
                        $"Signature algorithm '{header.Alg}' not supported");
                }
            }

            var signatureBytes = Base64UrlEncoder.DecodeBytes(signature);
            byte[] encodedBytes = Encoding.UTF8.GetBytes(headerRaw + "." + payloadRaw);

            var cryptoProviderFactory = issuerSigningKey.CryptoProviderFactory;
            var signatureProvider = cryptoProviderFactory.CreateForVerifying(issuerSigningKey, header.Alg);

            try
            {
                if (!signatureProvider.Verify(encodedBytes, signatureBytes))
                {
                    throw new Exception("Invalid signature");
                }
            }
            finally
            {
                cryptoProviderFactory.ReleaseSignatureProvider(signatureProvider);
            }
        }
    }
}
