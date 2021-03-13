using Impinj.OctaneSdk;
using System;

namespace BobRfid
{
    public class FakeReader : System.Windows.Forms.Form, IReader
    {
        private System.Windows.Forms.Button TagReportButton;
        private System.Windows.Forms.PropertyGrid TagPropertyGrid;
        private System.Windows.Forms.Button NewEpcButton;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private System.Windows.Forms.Button SpamButton;
        private Tag _tag;
        private bool _spamming = false;
        private System.Timers.Timer _spamTimer;

        public event EventHandler<TagReport> TagsReported;
        public event EventHandler KeepaliveReceived;
        public event EventHandler ConnectionLost;

        public FakeReader()
        {
            InitializeComponent();
            _tag = (Tag)Activator.CreateInstance(typeof(Tag), true);
            TagPropertyGrid.SelectedObject = _tag;
            _spamTimer = new System.Timers.Timer(100);
            _spamTimer.Elapsed += _spamTimer_Elapsed;
            _spamTimer.Start();
        }

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
            logger.Trace("Connecting to previously connected address.");
        }

        public void Connect(string address)
        {
            logger.Trace($"Connecting to '{address}'.");
        }

        public void Disconnect()
        {
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

        private void InitializeComponent()
        {
            this.TagReportButton = new System.Windows.Forms.Button();
            this.TagPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.NewEpcButton = new System.Windows.Forms.Button();
            this.SpamButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // TagReportButton
            // 
            this.TagReportButton.Location = new System.Drawing.Point(12, 404);
            this.TagReportButton.Name = "TagReportButton";
            this.TagReportButton.Size = new System.Drawing.Size(265, 23);
            this.TagReportButton.TabIndex = 0;
            this.TagReportButton.Text = "Send tag report";
            this.TagReportButton.UseVisualStyleBackColor = true;
            this.TagReportButton.Click += new System.EventHandler(this.TagReportButton_Click);
            // 
            // TagPropertyGrid
            // 
            this.TagPropertyGrid.Location = new System.Drawing.Point(12, 47);
            this.TagPropertyGrid.Name = "TagPropertyGrid";
            this.TagPropertyGrid.Size = new System.Drawing.Size(612, 351);
            this.TagPropertyGrid.TabIndex = 1;
            // 
            // NewEpcButton
            // 
            this.NewEpcButton.Location = new System.Drawing.Point(12, 12);
            this.NewEpcButton.Name = "NewEpcButton";
            this.NewEpcButton.Size = new System.Drawing.Size(612, 29);
            this.NewEpcButton.TabIndex = 2;
            this.NewEpcButton.Text = "New EPC";
            this.NewEpcButton.UseVisualStyleBackColor = true;
            this.NewEpcButton.Click += new System.EventHandler(this.NewEpcButton_Click);
            // 
            // SpamButton
            // 
            this.SpamButton.Location = new System.Drawing.Point(348, 404);
            this.SpamButton.Name = "SpamButton";
            this.SpamButton.Size = new System.Drawing.Size(276, 23);
            this.SpamButton.TabIndex = 3;
            this.SpamButton.Text = "Spam";
            this.SpamButton.UseVisualStyleBackColor = true;
            this.SpamButton.Click += new System.EventHandler(this.SpamButton_Click);
            // 
            // FakeReader
            // 
            this.ClientSize = new System.Drawing.Size(636, 439);
            this.Controls.Add(this.SpamButton);
            this.Controls.Add(this.NewEpcButton);
            this.Controls.Add(this.TagPropertyGrid);
            this.Controls.Add(this.TagReportButton);
            this.Name = "FakeReader";
            this.Text = "Fake reader";
            this.ResumeLayout(false);
        }

        private void SendReport()
        {
            var report = (TagReport)Activator.CreateInstance(typeof(TagReport), true);
            report.Tags.Add(_tag);
            TagsReported?.Invoke(this, report);
        }

        private void TagReportButton_Click(object sender, EventArgs e)
        {
            SendReport();
        }

        private void NewEpcButton_Click(object sender, EventArgs e)
        {
            var rand = Convert.ToUInt32(new Random().Next(0, 10000000));
            _tag.Epc = TagData.FromUnsignedInt(rand);
            TagPropertyGrid.Refresh();
        }

        private void SpamButton_Click(object sender, EventArgs e)
        {
            _spamming = !_spamming;
            if (_spamming)
            {
                SpamButton.Text = "Stop spamming";
            }
            else
            {
                SpamButton.Text = "Spam";
            }
        }
    }
}
