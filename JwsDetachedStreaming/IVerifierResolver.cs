using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming
{
    public interface IVerifierResolver
    {
        IVerifier Resolve(JObject header);
    }
}
