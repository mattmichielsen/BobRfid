using System;

namespace BobRfid
{
    public class TrackingFailedException : Exception
    {
        public TrackingFailedException(string message) : base(message) { }
    }
}
