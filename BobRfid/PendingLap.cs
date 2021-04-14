using System;

namespace BobRfid
{
    internal class PendingLap
    {
        public string Epc { get; set; }
        public TimeSpan LapTime { get; set; }
    }
}