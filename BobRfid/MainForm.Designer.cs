
using BrightIdeasSoftware;

namespace BobRfid
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.TagStatsListView = new BrightIdeasSoftware.ObjectListView();
            this.EpcColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.CountColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.CountLabel = new System.Windows.Forms.Label();
            this.ReaderSettingsButton = new System.Windows.Forms.Button();
            this.AntennaSettingsButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.TagStatsListView)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TagStatsListView
            // 
            this.TagStatsListView.AllColumns.Add(this.EpcColumn);
            this.TagStatsListView.AllColumns.Add(this.CountColumn);
            this.TagStatsListView.CellEditUseWholeCell = false;
            this.TagStatsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.EpcColumn,
            this.CountColumn});
            this.tableLayoutPanel1.SetColumnSpan(this.TagStatsListView, 3);
            this.TagStatsListView.Cursor = System.Windows.Forms.Cursors.Default;
            this.TagStatsListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TagStatsListView.HideSelection = false;
            this.TagStatsListView.Location = new System.Drawing.Point(3, 3);
            this.TagStatsListView.Name = "TagStatsListView";
            this.TagStatsListView.Size = new System.Drawing.Size(680, 345);
            this.TagStatsListView.TabIndex = 0;
            this.TagStatsListView.UseCompatibleStateImageBehavior = false;
            this.TagStatsListView.UseNotifyPropertyChanged = true;
            this.TagStatsListView.View = System.Windows.Forms.View.Details;
            // 
            // EpcColumn
            // 
            this.EpcColumn.AspectName = "Epc";
            this.EpcColumn.Tag = "";
            this.EpcColumn.Text = "Epc";
            this.EpcColumn.Width = 191;
            // 
            // CountColumn
            // 
            this.CountColumn.AspectName = "Count";
            this.CountColumn.Tag = "";
            this.CountColumn.Text = "Count";
            this.CountColumn.Width = 86;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Controls.Add(this.TagStatsListView, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.CountLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.ReaderSettingsButton, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.AntennaSettingsButton, 2, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90.25641F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.743589F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(686, 390);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // CountLabel
            // 
            this.CountLabel.AutoSize = true;
            this.CountLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountLabel.Location = new System.Drawing.Point(3, 351);
            this.CountLabel.Name = "CountLabel";
            this.CountLabel.Size = new System.Drawing.Size(222, 39);
            this.CountLabel.TabIndex = 1;
            this.CountLabel.Text = "Count: 0";
            this.CountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ReaderSettingsButton
            // 
            this.ReaderSettingsButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ReaderSettingsButton.Location = new System.Drawing.Point(231, 354);
            this.ReaderSettingsButton.Name = "ReaderSettingsButton";
            this.ReaderSettingsButton.Size = new System.Drawing.Size(222, 33);
            this.ReaderSettingsButton.TabIndex = 2;
            this.ReaderSettingsButton.Text = "Reader Settings";
            this.ReaderSettingsButton.UseVisualStyleBackColor = true;
            this.ReaderSettingsButton.Click += new System.EventHandler(this.ReaderSettingsButton_Click);
            // 
            // AntennaSettingsButton
            // 
            this.AntennaSettingsButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AntennaSettingsButton.Location = new System.Drawing.Point(459, 354);
            this.AntennaSettingsButton.Name = "AntennaSettingsButton";
            this.AntennaSettingsButton.Size = new System.Drawing.Size(224, 33);
            this.AntennaSettingsButton.TabIndex = 3;
            this.AntennaSettingsButton.Text = "Antenna Settings";
            this.AntennaSettingsButton.UseVisualStyleBackColor = true;
            this.AntennaSettingsButton.Click += new System.EventHandler(this.AntennaSettingsButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 390);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "MainForm";
            this.Text = "BobRfid";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.TagStatsListView)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private ObjectListView TagStatsListView;
        private OLVColumn EpcColumn;
        private OLVColumn CountColumn;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label CountLabel;
        private System.Windows.Forms.Button ReaderSettingsButton;
        private System.Windows.Forms.Button AntennaSettingsButton;
    }
}

