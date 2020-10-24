using Newtonsoft.Json;

namespace Microsoft.Azure.Cosmos
{
    public abstract class CosmosItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }
    }
}