using Impinj.OctaneSdk;
using System;

namespace BobRfid
{
    public class ImpinjReader : IReader
    {
        private Impinj.OctaneSdk.ImpinjReader _reader;

        public ImpinjReader()
        {
            _reader = new Impinj.OctaneSdk.ImpinjReader();
            _reader.TagsReported += OnTagsReported;
            _reader.KeepaliveReceived += OnKeepaliveReceived;
            _reader.ConnectionLost += OnConnectionLost;
        }

        public bool IsConnected => _reader.IsConnected;

        private void OnTagsReported(Impinj.OctaneSdk.ImpinjReader reader, TagReport report)
        {
            TagsReported?.Invoke(this, report);
        }
        
        private void OnKeepaliveReceived(Impinj.OctaneSdk.ImpinjReader reader)
        {
            KeepaliveReceived?.Invoke(this, new EventArgs());    
        }

        private void OnConnectionLost(Impinj.OctaneSdk.ImpinjReader reader)
        {
            ConnectionLost?.Invoke(this, new EventArgs());
        }

        public event EventHandler<TagReport> TagsReported;
        public event EventHandler KeepaliveReceived;
        public event EventHandler ConnectionLost;

        public void ApplySettings(Settings settings)
        {
            _reader.ApplySettings(settings);    
        }

        public void Connect()
        {
            _reader.Connect();
        }

        public void Connect(string address)
        {
            _reader.Connect(address);
        }

        public Settings QueryDefaultSettings()
        {
            return _reader.QueryDefaultSettings();
        }

        public void SaveSettings()
        {
            _reader.SaveSettings();
        }

        public void Stop()
        {
            _reader.Stop();
        }

        public void Disconnect()
        {
            _reader.Disconnect();
        }
    }
}
