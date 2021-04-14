using Newtonsoft.Json;

namespace BobRfid
{
    public class PilotRaceLap
    {
        [JsonProperty("lap_num")]
        public int LapNum { get; set; }

        [JsonProperty("lap_time")]
        public int LapTime { get; set; }
        
        public Pilot Pilot { get; set; }
    }
}
