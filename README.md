[![NuGet version (JwsDetachedStreaming)](https://img.shields.io/nuget/v/JwsDetachedStreaming.svg?style=flat-square)](https://www.nuget.org/packages/JwsDetachedStreaming/)
[![FuGet version (JwsDetachedStreaming)](https://www.fuget.org/packages/JwsDetachedStreaming/badge.svg)](https://www.fuget.org/packages/JwsDetachedStreaming)

# JwsDetachedStreaming

Streaming sign and verify detached JSON Web Signature

## Example
```csharp
using var payload = File.OpenRead("payload.test");// payload as memory stream

var header = new JObject {{"custom", "value"}};

await using var ms = new MemoryStream();

await using var writer = await JwsDetachedWriter.CreateAsync(ms, header, "PS256", new SignerFactory());
payload.Position = 0;
await payload.CopyToAsync(writer.Payload);
await writer.Finish();

var jwsDetached = Encoding.ASCII.GetString(ms.ToArray());
            
await using var reader = await JwsDetachedReader.CreateAsync(ms.ToArray().AsMemory(), new VerifierFactory());
payload.Position = 0;
await payload.CopyToAsync(reader.Payload);

var readedHeader = await reader.ReadAsync();
```

Signer and Verifier implementation for example
```csharp
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
```
