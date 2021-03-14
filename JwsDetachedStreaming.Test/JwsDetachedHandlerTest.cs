﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming.Test
{
    [TestClass]
    public class JwsDetachedHandlerTest
    {
        [TestMethod]
        public void CommonTest()
        {
            using var payload = File.OpenRead("Provision/cert.pfx.test");

            var header = new JObject {{"custom", "value"}};

            var handler = new JwsDetachedHandler();

            payload.Position = 0;
            var jwsDetached= handler.Write(header, "PS256", new SignerResolver(), payload);

            payload.Position = 0;
            header = handler.Read(jwsDetached, new VerifierResolver(), payload);

            Assert.IsTrue(header.GetValue("custom").ToString() == "value");
        }

        class SignerResolver: ISignerResolver
        {
            private readonly X509Certificate2 _certificate;

            public SignerResolver()
            {
                _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
            }

            public ISigner Resolve(JObject header)
            {
                return header.GetValue("alg").ToString() switch
                {
                    "PS256" => new SignerPs256(_certificate),
                    _ => throw new NotSupportedException("Signature algorithm not supported")
                };
            }
        }

        class SignerPs256 : ISigner
        {
            private readonly X509Certificate2 _certificate;

            public SignerPs256(X509Certificate2 certificate)
            {
                _certificate = certificate;
            }

            public byte[] Sign(Stream inputStream)
            {
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(inputStream);

                using var privateKey = _certificate.GetRSAPrivateKey();
                return privateKey.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
        }

        class VerifierResolver : IVerifierResolver
        {
            private readonly X509Certificate2 _certificate;

            public VerifierResolver()
            {
                _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
            }

            public IVerifier Resolve(JObject header)
            {
                return header.GetValue("alg").ToString() switch
                {
                    "PS256" => new VerifierPs256(_certificate),
                    _ => throw new NotSupportedException("Signature algorithm not supported")
                };
            }
        }

        class VerifierPs256 : IVerifier
        {
            private readonly X509Certificate2 _certificate;

            public VerifierPs256(X509Certificate2 certificate)
            {
                _certificate = certificate;
            }

            public bool Verify(Stream inputStream, byte[] signature)
            {
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(inputStream);

                using var privateKey = _certificate.GetRSAPrivateKey();
                return privateKey.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
        }
    }
}