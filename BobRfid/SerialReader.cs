using Impinj.OctaneSdk;
using System;
using ThingMagic;

namespace BobRfid
{
    class SerialReader : IReader
    {
        private ThingMagic.SerialReader _reader;
        private string _portName;
        private int _baud;
        private bool _connected;

        public bool IsConnected
        {
            get
            {
                return _connected;
            }
        }

        public event EventHandler KeepaliveReceived;
        public event EventHandler ConnectionLost;
        public event EventHandler<TagReport> TagsReported;

        public SerialReader(string portName)
        {
            _portName = portName;
            _baud = 115200;
            var r = Reader.Create($"tmr://{portName}");
            _reader = r as ThingMagic.SerialReader;
            _reader.TagRead += _reader_TagRead;
        }

        private void _reader_TagRead(object sender, TagReadDataEventArgs e)
        {
            var report = (TagReport)Activator.CreateInstance(typeof(TagReport), true);
            var tag = (Tag)Activator.CreateInstance(typeof(Tag), true);
            tag.Epc = Impinj.OctaneSdk.TagData.FromByteArray(e.TagReadData.Epc);
            report.Tags.Add(tag);
            TagsReported?.Invoke(this, report);
        }

        public void ApplySettings(Settings settings)
        {
            throw new NotImplementedException();
        }

        public void Connect(string portName)
        {
            _portName = portName;
            Connect();
        }

        public void Connect()
        {
            _reader.OpenSerialPort(_portName, ref _baud);
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public Settings QueryDefaultSettings()
        {
            throw new NotImplementedException();
        }

        public void SaveSettings()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
