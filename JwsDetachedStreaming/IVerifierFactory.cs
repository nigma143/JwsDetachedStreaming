using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming
{
    public interface IVerifierFactory
    {
        Verifier Create(JObject header);
    }
}
