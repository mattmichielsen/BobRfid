using Newtonsoft.Json;

namespace BobRfid
{
    public class Pilot
    {
        [JsonProperty("transponder_token")]
        public string TransponderToken { get; set; }
        public string Name { get; set; }
        public string Team { get; set; }
        public bool Printed { get; set; }
    }
}
