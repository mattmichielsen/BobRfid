using Impinj.OctaneSdk;
using System;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace BobRfid
{
    public partial class MainForm : Form
    {
        private static IReader reader;
        private static ConcurrentDictionary<string, TagStats> tagStats;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public MainForm(IReader r, ConcurrentDictionary<string, TagStats> t)
        {
            InitializeComponent();

            reader = r;
            tagStats = t;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            reader.TagsReported += OnTagsReported;
            TagStatsListView.SetObjects(tagStats);
        }

        private void OnTagsReported(object reader, TagReport report)
        {
            UpdateCount(tagStats.Count); 
        }

        private void UpdateCount(int count)
        {
            if (CountLabel.InvokeRequired && !Disposing)
            {
                try
                {
                    CountLabel.Invoke(new Action(() => CountLabel.Text = $"Count: {count}"));
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"Error updating count: {ex}");
                }
            }
            else
            {
                CountLabel.Text = $"Count: {count}";
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            reader.TagsReported -= OnTagsReported;
        }

        private void ReaderSettingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = reader.QueryDefaultSettings();
                using (var settingsForm = new SettingsForm(settings))
                {
                    if (settingsForm.ShowDialog() == DialogResult.OK)
                    {
                        reader.ApplySettings(settings);
                        reader.SaveSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error applying settings: {ex}";
                logger.Error(ex, errorMessage);
                MessageBox.Show(errorMessage);
            }
            finally
            {
                reader.Connect();
            }
        }

        private void AntennaSettingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = reader.QueryDefaultSettings();
                using (var settingsForm = new SettingsForm(settings.Antennas.GetAntenna(1)))
                {
                    if (settingsForm.ShowDialog() == DialogResult.OK)
                    {
                        reader.ApplySettings(settings);
                        reader.SaveSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error applying settings: {ex}";
                logger.Error(ex, errorMessage);
                MessageBox.Show(errorMessage);
            }
            finally
            {
                reader.Connect();
            }
        }

        private void RegistrationModeButton_Click(object sender, EventArgs e)
        {
            Program.RegistrationMode = !Program.RegistrationMode;
            if (Program.RegistrationMode)
            {
                var pending = Program.PendingRegistrations;
                if (pending > 0 && MessageBox.Show($"You already have {pending} registrations pending. Do you want to load more?") == DialogResult.No)
                {
                    return;
                }

                RegistrationModeButton.Text = "Back to timing mode";
                using (var f = new OpenFileDialog())
                {
                    f.DefaultExt = "csv";
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        var result = Program.LoadRegistrants(f.FileName);
                        MessageBox.Show($"Loaded {result} participants.");
                    }
                }
            }
            else
            {
                RegistrationModeButton.Text = "Registration Mode";
            }
        }
    }
}
