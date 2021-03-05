using Impinj.OctaneSdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BobRfid
{
    public partial class MainForm : Form
    {
        static ImpinjReader reader = new ImpinjReader();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting: {ex}");
            }
        }

        private void Connect()
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

        private void OnTagsReported(ImpinjReader reader, TagReport report)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var client = new HttpClient();
                    foreach (Tag tag in report)
                    {
                        await client.PostAsync($"http://localhost:3000/api/v1/lap_track?transponder_token={tag.Epc}&lap_time_in_ms=5000", null);
                    }
                }
                catch (Exception ex)
                {
                    //TODO: log exception
                }
            });
        }

        private void OnConnectionLost(ImpinjReader reader)
        {
        }

        private void OnKeepaliveReceived(ImpinjReader reader)
        {
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            reader.Stop();
            reader.Disconnect();
        }
    }
}
