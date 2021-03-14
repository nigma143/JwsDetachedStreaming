using System.IO;

namespace JwsDetachedStreaming
{
    public interface ISigner
    {
        byte[] Sign(Stream inputStream);
    }
}
