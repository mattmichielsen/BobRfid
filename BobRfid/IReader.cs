using Impinj.OctaneSdk;
using System;

namespace BobRfid
{
    public interface IReader
    {
        event EventHandler KeepaliveReceived;
        event EventHandler ConnectionLost;
        event EventHandler<TagReport> TagsReported;

        bool IsConnected { get; }

        void Connect(string v);
        Settings QueryDefaultSettings();
        void ApplySettings(Settings settings);
        void SaveSettings();
        void Stop();
        void Disconnect();
        void Connect();
    }
}
