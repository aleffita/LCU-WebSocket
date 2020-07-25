using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LCUConnector.WebSockets
{
    public class LeagueEvent
    {
        [JsonProperty("data")] public JToken Data { get; set; }

        [JsonProperty("eventType")] public string EventType { get; set; }

        [JsonProperty("uri")] public string Uri { get; set; }
    }
}