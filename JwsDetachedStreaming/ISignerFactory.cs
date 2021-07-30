using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming
{
    public interface ISignerFactory
    {
        Signer Create(JObject header);
    }
}
