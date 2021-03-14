using Newtonsoft.Json.Linq;

namespace JwsDetachedStreaming
{
    public interface ISignerResolver
    {
        ISigner Resolve(JObject header);
    }
}
