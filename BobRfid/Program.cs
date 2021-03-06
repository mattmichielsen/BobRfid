using Impinj.OctaneSdk;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BobRfid
{
    static class Program
    {
        private const int MIN_LAP_SECONDS = 30;

        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static ImpinjReader reader = new ImpinjReader();
        static ConcurrentDictionary<string, TagStats> tagStats = new ConcurrentDictionary<string, TagStats>();
        static HttpClient httpClient = new HttpClient();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to reader: {ex}");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(reader, tagStats));

            reader.Stop();
            reader.Disconnect();
        }

        private static void Connect()
        {
            reader.Connect("169.254.1.1");
            Settings settings = reader.QueryDefaultSettings();

            // Start the reader as soon as it's configured.
            // This will allow it to run without a client connected.
            settings.AutoStart.Mode = AutoStartMode.Immediate;
            settings.AutoStop.Mode = AutoStopMode.None;

            // Use Advanced GPO to set GPO #1 
            // when an client (LLRP) connection is present.
            settings.Gpos.GetGpo(1).Mode = GpoMode.LLRPConnectionStatus;

            // Tell the reader to include the timestamp in all tag reports.
            settings.Report.IncludeFirstSeenTime = true;
            settings.Report.IncludeLastSeenTime = true;
            settings.Report.IncludeSeenCount = true;

            // If this application disconnects from the 
            // reader, hold all tag reports and events.
            settings.HoldReportsOnDisconnect = true;

            // Enable keepalives.
            settings.Keepalives.Enabled = true;
            settings.Keepalives.PeriodInMs = 5000;

            // Enable link monitor mode.
            // If our application fails to reply to
            // five consecutive keepalive messages,
            // the reader will close the network connection.
            settings.Keepalives.EnableLinkMonitorMode = true;
            settings.Keepalives.LinkDownThreshold = 5;

            // Assign an event handler that will be called
            // when keepalive messages are received.
            reader.KeepaliveReceived += OnKeepaliveReceived;

            // Assign an event handler that will be called
            // if the reader stops sending keepalives.
            reader.ConnectionLost += OnConnectionLost;

            // Apply the newly modified settings.
            reader.ApplySettings(settings);

            // Save the settings to the reader's 
            // non-volatile memory. This will
            // allow the settings to persist
            // through a power cycle.
            reader.SaveSettings();

            // Assign the TagsReported event handler.
            // This specifies which method to call
            // when tags reports are available.
            reader.TagsReported += OnTagsReported;
        }

        private static void OnTagsReported(ImpinjReader reader, TagReport report)
        {
            Task.Factory.StartNew(async () =>
            {
                var now = DateTime.Now;
                foreach (Tag tag in report)
                {
                    var key = tag.Epc.ToHexString();
                    try
                    { 
                        if (!tagStats.ContainsKey(key))
                        {
                            tagStats[key] = new TagStats();
                            tagStats[key].TimeStamp = now;
                            tagStats[key].LapStartTime = now;
                            logger.Trace($"Started tracking first lap for ID '{key}'.");
                        }

                        tagStats[key].LastReport = tag;
                        tagStats[key].Count++;
                        if (now > tagStats[key].TimeStamp.AddSeconds(MIN_LAP_SECONDS))
                        {
                            var lapTime = now - tagStats[key].LapStartTime;
                            logger.Trace($"Logging lap time of {lapTime.TotalSeconds} seconds for ID '{key}'.");
                            var result = await httpClient.PostAsync($"http://localhost:3000/api/v1/lap_track?transponder_token={key}&lap_time_in_ms={lapTime.TotalMilliseconds}", null);
                            if (result.IsSuccessStatusCode)
                            {
                                tagStats[key].LapStartTime = now;
                                logger.Trace($"Successfully logged lap time for ID '{key}'.");
                            }
                            else
                            {
                                throw new Exception($"Failed to log lap time of {lapTime.TotalSeconds} seconds for ID '{key}'. Full error: {await result.Content.ReadAsStringAsync()}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Error logging lap time: {ex}");
                    }
                    finally
                    {
                        tagStats[key].TimeStamp = now;
                    }
                }
               
            });
        }

        private static void OnConnectionLost(ImpinjReader reader)
        {
        }

        private static void OnKeepaliveReceived(ImpinjReader reader)
        {
        }
    }
}
