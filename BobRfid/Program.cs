using CsvHelper;
using Impinj.OctaneSdk;
using Newtonsoft.Json;
using SharpZebra.Printing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BobRfid
{
    static class Program
    {
        private const int MIN_LAP_SECONDS = 30;

        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static IReader reader;
        static ConcurrentDictionary<string, TagStats> tagStats = new ConcurrentDictionary<string, TagStats>();
        static ConcurrentDictionary<string, Pilot> registeredPilots = new ConcurrentDictionary<string, Pilot>();
        static ConcurrentDictionary<string, bool> printed = new ConcurrentDictionary<string, bool>();
        static HttpClient httpClient;
        static DebounceThrottle.ThrottleDispatcher dispatcher = new DebounceThrottle.ThrottleDispatcher(100);
        static IZebraPrinter zebraPrinter;
        static BlockingCollection<TagSeen> tagsToProcess = new BlockingCollection<TagSeen>();
        static Queue<Pilot> pendingRegistrations = new Queue<Pilot>();
        static BlockingCollection<PendingLap> pendingLaps = new BlockingCollection<PendingLap>();
        static AppSettings appSettings = new AppSettings();

        public static bool RegistrationMode { get; set; } = false;

        static IZebraPrinter ZebraPrinter
        {
            get
            {
                if (zebraPrinter == null)
                {
                    var printerSettings = new PrinterSettings() { PrinterName = "ZDesigner TLP 2844-Z" };
                    zebraPrinter = new USBPrinter(printerSettings);
                }

                return zebraPrinter;
            }
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("BobRfid starting up.");
            appSettings.SettingsSaving += AppSettings_SettingsSaving;

            InitializeClient();

            if (args.Length > 0 && args.Contains("--test"))
            {
                reader = new FakeReader();
                Console.WriteLine("TEST MODE");
            }
            else
            {
                reader = new RealReader();
            }

            if (args.Length > 0 && args.Contains("--register"))
            {
                Console.WriteLine("REGISTRATION MODE");
                RegistrationMode = true;
                logger.Trace("Started in registration mode.");
            }

            var lowPower = false;
            if (args.Length > 0 && args.Contains("--lowpower"))
            {
                lowPower = true;
            }

            Task.Run(() => ProcessTags());

            if (args.Length > 0 && args.Contains("--verifytrace"))
            {
                try
                {
                    VerifyTrace();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex}");
                }

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();

                return;
            }

            Task.Run(() => SubmitLaps());

            Console.WriteLine($"Waiting {appSettings.StartupDelaySeconds} seconds for reader to start up.");
            Thread.Sleep(TimeSpan.FromSeconds(appSettings.StartupDelaySeconds));

            try
            {
                Connect(lowPower);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to reader: {ex}");
            }

            Task.Run(() => CheckConnections());

            if (args.Length > 0 && args.Contains("--form"))
            {
                Console.WriteLine("Form GUI is not currently supported.");   
            }
            else
            {
                string input = string.Empty;
                Console.WriteLine("Type 'exit' to stop.");
                while (true)
                {
                    Console.Write("BobRfid:> ");
                    input = Console.ReadLine().Trim();
                    if (input.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }
                    else if (input.Equals("connect", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Connecting...");
                        reader.Connect();
                    }
                    else if (input.Equals("disconnect", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Disconnecting...");
                        reader.Disconnect();
                    }
                    else if (input.Equals("instance", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine($"Currently connecting to '{appSettings.ServiceBaseAddress}'.");
                        Console.Write("New instance name (blank to leave unchanged):> ");
                        var newInstance = Console.ReadLine().Trim();
                        if (!string.IsNullOrWhiteSpace(newInstance))
                        {
                            appSettings.ServiceBaseAddress = $"http://legsofsteel.bob85.com/{newInstance}/";
                            appSettings.Save();
                        }
                    }
                    else if (input.Equals("uri", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine($"Currently connecting to '{appSettings.ServiceBaseAddress}'.");
                        Console.Write("New URI (blank to leave unchanged):> ");
                        var newUri = Console.ReadLine().Trim();
                        if (!string.IsNullOrWhiteSpace(newUri))
                        {
                            if (Uri.TryCreate(newUri, UriKind.Absolute, out Uri result))
                            {
                                Console.WriteLine("Valid URI. Saving.");
                                appSettings.ServiceBaseAddress = newUri;
                                appSettings.Save();
                            }
                            else
                            {
                                Console.WriteLine($"Invalid URI: {newUri}");
                            }
                        }
                    }
                    else if (input.Equals("timeout", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine($"Current service timeout is {appSettings.ServiceTimeoutSeconds} seconds.");
                        Console.Write("New value in seconds (blank to leave unchanged:> ");
                        var newTimeout = Console.ReadLine().Trim();
                        if (!string.IsNullOrWhiteSpace(newTimeout))
                        {
                            if (int.TryParse(newTimeout, out int timeout) && timeout > 0)
                            {
                                appSettings.ServiceTimeoutSeconds = timeout;
                                appSettings.Save();
                            }
                            else
                            {
                                logger.Warn($"Invalid timeout value '{newTimeout}'.");
                            }
                        }
                    }
                    else if (input.Equals("ip", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine($"Currently connecting to reader at host '{appSettings.ReaderIpAddress}'.");
                        Console.Write("New hostname or IP address (blank to leave unchanged):> ");
                        var newReaderHost = Console.ReadLine().Trim();
                        if (!string.IsNullOrWhiteSpace(newReaderHost))
                        {
                            appSettings.ReaderIpAddress = newReaderHost;
                            appSettings.Save();
                        }
                    }
                    else if (input.Equals("startupdelay", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine($"Current startup delay is {appSettings.StartupDelaySeconds} seconds.");
                        Console.Write("New value in seconds (blank to leave unchanged:> ");
                        var newStartupDelay = Console.ReadLine().Trim();
                        if (!string.IsNullOrWhiteSpace(newStartupDelay))
                        {
                            if (int.TryParse(newStartupDelay, out int delay) && delay >= 0)
                            {
                                appSettings.StartupDelaySeconds = delay;
                                appSettings.Save();
                            }
                            else
                            {
                                logger.Warn($"Invalid startup delay value '{newStartupDelay}'.");
                            }
                        }
                    }
                    else if (RegistrationMode && input.StartsWith("load", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var split = input.Split(' ');
                        if (split.Length == 2)
                        {
                            LoadRegistrants(split[1]);
                        }
                    }
                    else if (reader is FakeReader)
                    {
                        ((FakeReader)reader).SendCommand(input);
                    }
                }
            }

            try
            {
                Console.WriteLine("Exiting...");
                reader.Stop();
                reader.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to disconnect upon exit: {ex}");
            }
            finally
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private static void InitializeClient()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(appSettings.ServiceBaseAddress);
            httpClient.Timeout = TimeSpan.FromSeconds(appSettings.ServiceTimeoutSeconds);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static async Task<MonitorData> Monitor()
        {
            var result = await httpClient.GetAsync($"api/v1/monitor");
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<MonitorData>(await result.Content.ReadAsStringAsync());
            }
            else if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return null;
            }
            else
            {
                throw new Exception($"Monitor API call failed with code '{result.StatusCode}' and content: {await result.Content.ReadAsStringAsync()}");
            }
        }

        private static async Task CheckConnections()
        {
            while (true)
            {
                var monitorResult = false;
                try
                {
                    var monitor = await Monitor();
                    monitorResult = !string.IsNullOrWhiteSpace(monitor?.Session?.Title); 
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Monitor error: {ex}");
                }

                if (reader != null && reader.IsConnected && monitorResult)
                {
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                await Task.Delay(5000);
            }
        }

        private static void AppSettings_SettingsSaving(object sender, System.ComponentModel.CancelEventArgs e)
        {
            InitializeClient();
            reader.Disconnect();
            reader.Connect(appSettings.ReaderIpAddress);
        }

        private static void VerifyTrace()
        {
            Console.WriteLine("Input trace log file or directory:");
            var path = Console.ReadLine();
            var files = new List<string>();
            if (File.Exists(path))
            {
                files.Add(path);
            }
            else if (Directory.Exists(path))
            {
                files.AddRange(Directory.GetFiles(path));
            }
            else
            {
                throw new InvalidOperationException($"File or Directory '{path}' does not exist.");
            }

            foreach (var file in files)
            {
                if (file.EndsWith(".log"))
                {
                    Console.WriteLine($"Reading file '{file}'.");
                    var logRecords = new List<LogRecord>();
                    using (var reader = new StreamReader(file))
                    {
                        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null, Delimiter = "|", HasHeaderRecord = false, BadDataFound = (b) => { Console.WriteLine($"Bad log line found: {b.RawRecord}"); } };
                        using (var csv = new CsvReader(reader, config))
                        {
                            logRecords = csv.GetRecords<LogRecord>().ToList();
                        }
                    }

                    foreach (var record in logRecords)
                    {
                        if (record.LogLevel.Equals("Trace"))
                        {
                            var match = Regex.Match(record.Message, @"^Tracking ID '(\w+)'.$", RegexOptions.RightToLeft);
                            if (match.Success)
                            {
                                tagsToProcess.Add(new TagSeen { TimeStamp = DateTime.Parse(record.DateTime), Epc = match.Captures[0].Value });
                            }
                        }
                    }
                }
            }
        }

        private static void Connect(bool lowPower)
        {
            Console.WriteLine($"Connecting to reader at '{appSettings.ReaderIpAddress}'.");
            reader.Connect(appSettings.ReaderIpAddress);
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

            if (lowPower)
            {
                settings.ReaderMode = ReaderMode.AutoSetDenseReader;
                settings.SearchMode = SearchMode.SingleTarget;
                settings.Session = 1;
                settings.Antennas.TxPowerMax = false;
                settings.Antennas.TxPowerInDbm = 20;
                settings.Antennas.RxSensitivityMax = false;
                settings.Antennas.RxSensitivityInDbm = -70;
            }
            else
            {
                settings.ReaderMode = ReaderMode.AutoSetDenseReaderDeepScan;
            }

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

        public static int PendingRegistrations { get => pendingRegistrations.Count(); }

        public static int LoadRegistrants(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null };
                using (var csv = new CsvReader(reader, config))
                {
                    foreach (var record in csv.GetRecords<Pilot>())
                    {
                        pendingRegistrations.Enqueue(record);
                    }

                    return pendingRegistrations.Count();
                }
            }
        }

        public static void Print(string id, string name, string team)
        {
            var printerType = (PrinterType)appSettings.PrinterType;
            if (printerType == PrinterType.Zebra)
            {
                var zpl = $@"^XA^MCY^XZ^XA
^FO15,30^A0N,30,23^FH_^FD{id}^FS
^FO15,60^A0N,30,25^FH_^FD{name}^FS
^FO15,90^A0N,30,25^FH_^FD{team}^FS
^PQ1,0,0,N^XZ";
                ZebraPrinter.Print(System.Text.Encoding.ASCII.GetBytes(zpl));
            }
            else if (printerType == PrinterType.Dymo)
            {
                var label = DYMO.Label.Framework.Label.Open(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DymoTemplate.label"));
                label.SetObjectText("id", id);
                label.SetObjectText("name", name);
                label.SetObjectText("team", team);
                label.Print("DYMO LabelWriter 450");
            }
            else
            {
                throw new NotSupportedException($"Unknown printer type '{printerType}'.");
            }
        }

        private static async Task<Pilot> GetPilot(string transponderToken)
        {
            Pilot result = null;
            if (registeredPilots.ContainsKey(transponderToken))
            {
                result = registeredPilots[transponderToken];
            }

            var getResult = await httpClient.GetAsync($"api/v1/pilot/{transponderToken}");
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
            var postResult = await httpClient.PostAsync($"api/v1/pilot", new StringContent(jsonPilot));
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
            dispatcher.Throttle(() =>
            {
                var now = DateTime.Now;
                if (RegistrationMode && report.Tags.Count > 1)
                {
                    logger.Warn($"Registration mode requires 1 tag at a time. {report.Tags.Count} were found.");
                    return;
                }

                foreach (Tag tag in report)
                {
                    var epc = tag.Epc.ToHexString();
                    logger.Trace($"Tracking ID '{epc}'.");
                    tagsToProcess.Add(new TagSeen { Epc = epc, Tag = tag, TimeStamp = now });
                }
            });
        }

        private static async void ProcessTags()
        {
            foreach (var seen in tagsToProcess.GetConsumingEnumerable())
            {
                if (RegistrationMode)
                {
                    try
                    {
                        var pilot = await GetPilot(seen.Epc);
                        if (pilot != null)
                        {
                            logger.Trace($"Found existing pilot '{pilot.Name}'.");
                        }
                        else
                        {
                            var imported = pendingRegistrations.Dequeue();
                            try
                            {
                                pilot = await AddPilot(new Pilot { Name = imported.Name, Team = imported.Team, TransponderToken = seen.Epc });
                            }
                            catch
                            {
                                pendingRegistrations.Enqueue(imported);
                                throw;
                            }
                        }

                        if (!printed.ContainsKey(seen.Epc) || !printed[seen.Epc])
                        {
                            Print(seen.Epc, pilot.Name, pilot.Team);
                            printed[seen.Epc] = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Error registering pilot: {ex}");
                    }
                }
                else
                {
                    try
                    {
                        if (!tagStats.ContainsKey(seen.Epc))
                        {
                            tagStats[seen.Epc] = new TagStats();
                            tagStats[seen.Epc].TimeStamp = seen.TimeStamp;
                            tagStats[seen.Epc].LapStartTime = seen.TimeStamp;
                            logger.Info($"Started tracking first lap for ID '{seen.Epc}'.");
                        }

                        tagStats[seen.Epc].LastReport = seen.Tag;
                        tagStats[seen.Epc].Count++;
                        if (seen.TimeStamp > tagStats[seen.Epc].TimeStamp.AddSeconds(MIN_LAP_SECONDS))
                        {
                            var lapTime = seen.TimeStamp - tagStats[seen.Epc].LapStartTime;
                            logger.Info($"Tracking lap for ID '{seen.Epc}' with time '{lapTime}'.");
                            pendingLaps.Add(new PendingLap { Epc = seen.Epc, LapTime = lapTime });
                            tagStats[seen.Epc].LapStartTime = seen.TimeStamp;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Error tracking tag '{seen.Epc}': {ex}");
                    }
                    finally
                    {
                        tagStats[seen.Epc].TimeStamp = seen.TimeStamp;
                    }
                }
            }
        }

        private static async Task SubmitLaps()
        {
            foreach (var pending in pendingLaps.GetConsumingEnumerable())
            {
                try
                {
                    logger.Info($"Logging lap time of {pending.LapTime.TotalSeconds} seconds for ID '{pending.Epc}'.");
                    if (pending.IsRetry)
                    {
                        logger.Trace("Lap submission is a retry.");
                    }

                    var result = await httpClient.PostAsync($"api/v1/lap_track?transponder_token={pending.Epc}&lap_time_in_ms={pending.LapTime.TotalMilliseconds}", null);
                    if (result.IsSuccessStatusCode)
                    {
                        var lap = JsonConvert.DeserializeObject<PilotRaceLap>(await result.Content.ReadAsStringAsync());
                        logger.Info($"Successfully logged lap '{lap.LapNum}' time '{TimeSpan.FromMilliseconds(lap.LapTime)}' for '{lap.Pilot.Name}' with ID '{pending.Epc}'.");
                    }
                    else
                    {
                        throw new TrackingFailedException($"Failed to log lap time of {pending.LapTime.TotalSeconds} seconds for ID '{pending.Epc}'. Full error: {await result.Content.ReadAsStringAsync()}");
                    }
                }
                catch (TrackingFailedException ex)
                {
                    logger.Error(ex);
                }
                catch (Exception ex)
                {
                    pending.IsRetry = true;
                    logger.Error(ex);
                    pendingLaps.Add(pending);
                }

                await Task.Delay(100);
            }
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
