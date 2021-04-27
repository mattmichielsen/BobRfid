using Impinj.OctaneSdk;
using System;

namespace BobRfid
{
    public class RealReader : IReader
    {
        private ImpinjReader _reader;

        public RealReader()
        {
            _reader = new ImpinjReader();
            _reader.TagsReported += OnTagsReported;
            _reader.KeepaliveReceived += OnKeepaliveReceived;
            _reader.ConnectionLost += OnConnectionLost;
        }

        public bool IsConnected => _reader.IsConnected;

        private void OnTagsReported(ImpinjReader reader, TagReport report)
        {
            TagsReported?.Invoke(this, report);
        }
        
        private void OnKeepaliveReceived(ImpinjReader reader)
        {
            KeepaliveReceived?.Invoke(this, new EventArgs());    
        }

        private void OnConnectionLost(ImpinjReader reader)
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
