using Newtonsoft.Json;

namespace BobRfid
{
    public class Pilot
    {
        [JsonProperty("transponder_token")]
        public string TransponderToken { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("team")]
        public string Team { get; set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; set; }
    }
}
