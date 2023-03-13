using Impinj.OctaneSdk;
using System;

namespace BobRfid
{
    public class TagSeen
    {
        public string Epc { get; set; }
        public Tag Tag { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool IsRetry { get; set; }
    }
}
