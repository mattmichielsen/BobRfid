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
                CountLabel.Invoke(new Action(() => CountLabel.Text = $"Count: {count}"));
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
    }
}
