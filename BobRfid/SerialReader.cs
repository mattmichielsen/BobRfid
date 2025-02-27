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
            var r = Reader.Create($"tmr:///{portName}");
            _reader = r as ThingMagic.SerialReader;
            r.ParamSet("/reader/transportTimeout", 100);
            r.ParamSet("/reader/commandTimeout", 100);
            if (_reader != null)
            {
                _reader.TagRead += _reader_TagRead;
            }
        }

        private void _reader_TagRead(object sender, TagReadDataEventArgs e)
        {
            var report = new TagReport();
            var tag = new Tag() { Epc = new TagData(e.TagReadData.Epc) };
            report.Tags.Add(tag);
            TagsReported?.Invoke(this, report);
        }

        public void ApplySettings(Settings settings)
        {
        }

        public void Connect(string portName)
        {
            if (portName.StartsWith("COM"))
            {
                _portName = portName;
            }

            Connect();
        }

        public void Connect()
        {
            _reader.OpenSerialPort(_portName, ref _baud);
            SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.GEN2, null, null, 1000);
            _reader.ParamSet("/reader/read/plan", plan);
            _reader.StartReading();
            _connected = true;
        }

        public void Disconnect()
        {
            _reader.stopStreaming();
        }

        public Settings QueryDefaultSettings()
        {
            return new Settings();
        }

        public void SaveSettings()
        {
        }

        public void Stop()
        {
            _reader.StopReading();
        }
    }
}
