using Impinj.OctaneSdk;
using System;

namespace BobRfid
{
    public class FakeReader : IReader
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Tag _tag;
        private bool _spamming = false;
        private System.Timers.Timer _spamTimer;

        public event EventHandler<TagReport> TagsReported;
        public event EventHandler KeepaliveReceived;
        public event EventHandler ConnectionLost;

        public FakeReader()
        {
            _tag = (Tag)Activator.CreateInstance(typeof(Tag), true);
            _spamTimer = new System.Timers.Timer(100);
            _spamTimer.Elapsed += _spamTimer_Elapsed;
            _spamTimer.Start();
        }

        public bool IsConnected { get; private set; }

        private void _spamTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_spamming)
            {
                SendReport();
            }
        }

        public void ApplySettings(Settings settings)
        {
            logger.Trace("Applying settings.");
        }

        public void Connect()
        {
            IsConnected = true;
            logger.Trace("Connecting to previously connected address.");
        }

        public void Connect(string address)
        {
            IsConnected = true;
            logger.Trace($"Connecting to '{address}'.");
        }

        public void Disconnect()
        {
            IsConnected = false;
            logger.Trace("Disconnecting.");
        }

        public Settings QueryDefaultSettings()
        {
            logger.Trace("Querying default settings.");
            return new Settings();
        }

        public void SaveSettings()
        {
            logger.Trace("Saving settings.");
        }

        public void Stop()
        {
            logger.Trace("Stopping.");    
        }

        private void SendReport()
        {
            var report = (TagReport)Activator.CreateInstance(typeof(TagReport), true);
            report.Tags.Add(_tag);
            TagsReported?.Invoke(this, report);
        }

        private void GenerateNewTag()
        {
            var rand = Convert.ToUInt32(new Random().Next(0, 10000000));
            _tag.Epc = TagData.FromUnsignedInt(rand);
            Console.WriteLine($"New EPC generated: {_tag.Epc}");
        }

        public void SendCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }
            else if (command.StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
            {
                var split = command.Split(' ');
                if (split.Length == 2)
                {
                    if (uint.TryParse(split[1], out uint result))
                    {
                        _tag.Epc = TagData.FromUnsignedInt(result);
                        Console.WriteLine($"Test EPC set to: {_tag.Epc}");
                    }
                    else
                    {
                        logger.Warn($"Invalid uint value '{split[1]}'.");
                    }
                }
                else
                {
                    logger.Warn($"Invalid set command '{command}'");
                }
            }
            else if (command.Equals("new", StringComparison.InvariantCultureIgnoreCase))
            {
                GenerateNewTag();
            }
            else if (command.Equals("send", StringComparison.InvariantCultureIgnoreCase))
            {
                SendReport();
            }
            else if (command.Equals("spam", StringComparison.InvariantCultureIgnoreCase))
            {
                _spamming = !_spamming;
            }
            else
            {
                logger.Warn($"Unknown command '{command}'.");
            }
        }
    }
}
