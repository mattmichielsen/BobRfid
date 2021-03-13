using Impinj.OctaneSdk;
using SharpZebra.Printing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BobRfid
{
    static class Program
    {
        private const int MIN_LAP_SECONDS = 30;

        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static IReader reader;
        static ConcurrentDictionary<string, TagStats> tagStats = new ConcurrentDictionary<string, TagStats>();
        static ConcurrentDictionary<string, Pilot> registeredPilots = new ConcurrentDictionary<string, Pilot>();
        static HttpClient httpClient = new HttpClient();
        static DebounceThrottle.ThrottleDispatcher dispatcher = new DebounceThrottle.ThrottleDispatcher(500);
        static bool registrationMode = false;
        static IZebraPrinter printer;

        static IZebraPrinter Printer
        {
            get
            {
                if (printer == null)
                {
                    var printerSettings = new PrinterSettings() { PrinterName = "ZDesigner TLP 2844-Z" };
                    printer = new USBPrinter(printerSettings);
                }

                return printer;
            }
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            if (args.Length > 0 && args.Contains("--test"))
            {
                reader = new FakeReader();
                ((Form)reader).Show();
            }
            else
            {
                reader = new RealReader();
            }

            if (args.Length > 0 && args.Contains("--register"))
            {
                registrationMode = true;
                logger.Trace("Started in registration mode.");
            }

            try
            {
                Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to reader: {ex}");
            }

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
            //settings.Gpos.GetGpo(1).Mode = GpoMode.LLRPConnectionStatus;

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

        public static void Print(string id, string name, string className)
        {
            var zpl = $@"^XA^MCY^XZ^XA
^FO15,30^A0N,30,25^FH_^FD{id}^FS
^FO15,60^A0N,30,25^FH_^FD{name}^FS
^FO15,90^A0N,30,25^FH_^FD{className}^FS
^PQ1,0,0,N^XZ";
            Printer.Print(System.Text.Encoding.ASCII.GetBytes(zpl));
        }

        private static async Task<Pilot> GetPilot(string transponderToken)
        {
            Pilot result = null;
            if (registeredPilots.ContainsKey(transponderToken))
            {
                result = registeredPilots[transponderToken];
            }

            var getResult = await httpClient.GetAsync($"http://localhost:3000/api/v1/pilot/{transponderToken}");
            if (getResult.IsSuccessStatusCode)
            {
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<Pilot>(await getResult.Content.ReadAsStringAsync());
                registeredPilots[transponderToken] = result;
            }
            else
            {
                throw new Exception($"Can't find pilot with transponder token '{transponderToken}': {getResult.Content.ReadAsStringAsync()}");
            }

            return result;
        }

        private static async Task<Pilot> AddPilot(Pilot pilot)
        {
            var jsonPilot = Newtonsoft.Json.JsonConvert.SerializeObject(pilot);
            logger.Trace($"Adding pilot: {jsonPilot}");
            var postResult = await httpClient.PostAsync($"http://localhost:3000/api/v1/pilot", new StringContent(jsonPilot));
            if (postResult.IsSuccessStatusCode)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Pilot>(await postResult.Content.ReadAsStringAsync());
                registeredPilots[pilot.TransponderToken] = result;
                return result;
            }
            else
            {
                throw new Exception($"Failed to add pilot: {await postResult.Content.ReadAsStringAsync()}");
            }
        }

        private static void OnTagsReported(object reader, TagReport report)
        {
            dispatcher.Throttle(async () =>
            {
                var now = DateTime.Now;
                if (registrationMode && report.Tags.Count > 1)
                {
                    logger.Warn($"Registration mode requires 1 tag at a time. {report.Tags.Count} were found.");
                    return;
                }

                foreach (Tag tag in report)
                {
                    var key = tag.Epc.ToHexString();
                    logger.Trace($"Tracking ID '{key}'.");
                    if (registrationMode)
                    {
                        try
                        {
                            var pilot = await GetPilot(key);
                            if (pilot != null)
                            {
                                logger.Trace($"Found existing pilot '{pilot.Name}'.");
                            }
                            else
                            {
                                //TODO: Get from list
                                pilot = await AddPilot(new Pilot { Name = "Name", Team = "Team", TransponderToken = key });
                            }

                            if (!pilot.Printed)
                            {
                                Print(key, pilot.Name, pilot.Team);
                                registeredPilots[key].Printed = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error registering pilot: {ex}");
                        }

                        return;
                    }

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
                        logger.Error(ex, $"Error tracking tag '{key}': {ex}");
                    }
                    finally
                    {
                        tagStats[key].TimeStamp = now;
                    }
                }
               
            });
        }

        private static void OnConnectionLost(object reader, EventArgs e)
        {
            logger.Error("Reader connection lost!");
        }

        private static void OnKeepaliveReceived(object reader, EventArgs e)
        {
        }
    }
}
