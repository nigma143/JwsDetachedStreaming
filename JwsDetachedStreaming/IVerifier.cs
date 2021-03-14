using System.IO;

namespace JwsDetachedStreaming
{
    public interface IVerifier
    {
        bool Verify(Stream inputStream, byte[] signature);
    }
}
