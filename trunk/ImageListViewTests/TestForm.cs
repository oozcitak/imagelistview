using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ImageListViewTests
{
    public partial class TestForm : Form
    {
        #region Constructor
        string[] files;

        public TestForm()
        {
            InitializeComponent();

            Application.Idle += new EventHandler(Application_Idle);

            string picturePath = Path.GetDirectoryName(Path.GetDirectoryName(Application.StartupPath)) + Path.DirectorySeparatorChar + "Pictures";
            files = Directory.GetFiles(picturePath, "*.jpg");

            imageListView.RetrieveVirtualItemThumbnail += new Manina.Windows.Forms.RetrieveVirtualItemThumbnailEventHandler(imageListView1_RetrieveVirtualItemThumbnail);
            imageListView.ThumbnailCaching += new Manina.Windows.Forms.ThumbnailCachingEventHandler(imageListView1_ThumbnailCaching);
            imageListView.ThumbnailCached += new Manina.Windows.Forms.ThumbnailCachedEventHandler(imageListView1_ThumbnailCached);
            imageListView.CacheError += new Manina.Windows.Forms.CacheErrorEventHandler(imageListView1_CacheError);
        }
        #endregion

        #region Events
        // Retrive virtual item thumbnail
        void imageListView1_RetrieveVirtualItemThumbnail(object sender, Manina.Windows.Forms.VirtualItemThumbnailEventArgs e)
        {
            string file = e.Key as string;
            if (!string.IsNullOrEmpty(file))
            {
                using (Image img = Image.FromFile(file))
                {
                    Bitmap thumb = new Bitmap(img, e.ThumbnailDimensions);
                    e.ThumbnailImage = thumb;
                }
            }
        }
        // Cache error
        void imageListView1_CacheError(object sender, Manina.Windows.Forms.CacheErrorEventArgs e)
        {
            if (!benchMarking)
                LogEvent(string.Format("!!! {0} -> {1}", (e.CacheThread == Manina.Windows.Forms.CacheThread.Thumbnail ? "Thumbnail" : "Details"), e.Error.Message));
        }
        // Thumbnail cached
        void imageListView1_ThumbnailCached(object sender, Manina.Windows.Forms.ThumbnailCachedEventArgs e)
        {
            if (benchMarking)
            {
                lastThumbnailTime = benchmarkSW.ElapsedMilliseconds;
                cachedThumbnailCount++;
            }
            else
            {
                int index = -1;
                if (e.Item != null)
                    index = e.Item.Index;
                if (e.Error)
                    LogEvent(string.Format("<-- {0} !!!", index));
                else
                    LogEvent(string.Format("<-- {0} ({1})", index, e.Size));
            }
        }
        // Thumbnail caching
        void imageListView1_ThumbnailCaching(object sender, Manina.Windows.Forms.ThumbnailCachingEventArgs e)
        {
            if (!benchMarking)
            {
                int index = -1;
                if (e.Item != null)
                    index = e.Item.Index;
                LogEvent(string.Format("--> {0} ({1})", index, e.Size));
            }
        }
        // Log event to list box
        private void LogEvent(string message)
        {
            EventsListBox.Items.Add(message);
            EventsListBox.SelectedIndex = EventsListBox.Items.Count - 1;
        }
        #endregion

        #region Item Collection Tests
        // Add items
        private void AddItems_Click(object sender, EventArgs e)
        {
            imageListView.SuspendLayout();
            for (int i = 0; i < 1000 / files.Length; i++)
                imageListView.Items.AddRange(files);
            imageListView.ResumeLayout();
        }
        // Add item
        private void AddOneItem_Click(object sender, EventArgs e)
        {
            imageListView.Items.Add(files[0]);
        }
        // Insert item
        private void InsertItemAtIndex0_Click(object sender, EventArgs e)
        {
            imageListView.Items.Insert(0, files[0]);
        }
        // Remove item
        private void RemoveItemAtIndex0_Click(object sender, EventArgs e)
        {
            imageListView.Items.RemoveAt(0);
        }
        // Clear items
        private void ClearItems_Click(object sender, EventArgs e)
        {
            imageListView.Items.Clear();
        }
        // Add virtual items
        private void AddVirtualItems_Click(object sender, EventArgs e)
        {
            imageListView.SuspendLayout();
            for (int i = 0; i < 1000 / files.Length; i++)
                for (int j = 0; j < files.Length; j++)
                    imageListView.Items.Add(files[j], files[j]);
            imageListView.ResumeLayout();
        }
        #endregion

        #region Thumbnail Tests
        // Rebuild thumbnails
        private void RebuildThumbnails_Click(object sender, EventArgs e)
        {
            imageListView.ClearThumbnailCache();
        }
        #endregion

        #region Appearance Settings
        // View modes
        private void ViewThumbnails_Click(object sender, EventArgs e)
        {
            imageListView.View = Manina.Windows.Forms.View.Thumbnails;
        }
        private void ViewGallery_Click(object sender, EventArgs e)
        {
            imageListView.View = Manina.Windows.Forms.View.Gallery;
        }
        private void ViewPane_Click(object sender, EventArgs e)
        {
            imageListView.View = Manina.Windows.Forms.View.Pane;
        }
        private void ViewDetails_Click(object sender, EventArgs e)
        {
            imageListView.View = Manina.Windows.Forms.View.Details;
        }
        // Show file icons
        private void ShowFileIcons_Click(object sender, EventArgs e)
        {
            imageListView.ShowFileIcons = !imageListView.ShowFileIcons;
        }
        // Show checkboxes
        private void ShowCheckboxes_Click(object sender, EventArgs e)
        {
            imageListView.ShowCheckBoxes = !imageListView.ShowCheckBoxes;
        }
        // Show scroll bars
        private void ShowScrollbars_Click(object sender, EventArgs e)
        {
            imageListView.ScrollBars = !imageListView.ScrollBars;
        }
        #endregion

        #region Behavior Settings
        // Cache mode
        private void CacheOnDemand_Click(object sender, EventArgs e)
        {
            if (imageListView.CacheMode == Manina.Windows.Forms.CacheMode.Continuous)
                imageListView.CacheMode = Manina.Windows.Forms.CacheMode.OnDemand;
            else
                imageListView.CacheMode = Manina.Windows.Forms.CacheMode.Continuous;
        }
        // Duplicte filenames
        private void AllowDuplicateFilenames_Click(object sender, EventArgs e)
        {
            imageListView.AllowDuplicateFileNames = !imageListView.AllowDuplicateFileNames;
        }
        // Integral scroll
        private void IntegralScroll_Click(object sender, EventArgs e)
        {
            imageListView.IntegralScroll = !imageListView.IntegralScroll;
        }
        // Multi select
        private void MultiSelect_Click(object sender, EventArgs e)
        {
            imageListView.MultiSelect = !imageListView.MultiSelect;
        }
        // Use WIC
        private void UseWIC_Click(object sender, EventArgs e)
        {
            if (imageListView.UseWIC == Manina.Windows.Forms.UseWIC.Auto)
                imageListView.UseWIC = Manina.Windows.Forms.UseWIC.Never;
            else
                imageListView.UseWIC = Manina.Windows.Forms.UseWIC.Auto;
        }
        // Embedded thumbnails
        private void UseEmbeddedThumbnails_Click(object sender, EventArgs e)
        {
            if (imageListView.UseEmbeddedThumbnails == Manina.Windows.Forms.UseEmbeddedThumbnails.Auto)
                imageListView.UseEmbeddedThumbnails = Manina.Windows.Forms.UseEmbeddedThumbnails.Never;
            else
                imageListView.UseEmbeddedThumbnails = Manina.Windows.Forms.UseEmbeddedThumbnails.Auto;
        }
        // Auto rotate thumbnails
        private void AutoRotateThumbnails_Click(object sender, EventArgs e)
        {
            imageListView.AutoRotateThumbnails = !imageListView.AutoRotateThumbnails;
        }
        #endregion

        #region Update UI
        void Application_Idle(object sender, EventArgs e)
        {
            ViewThumbnails.Checked = (imageListView.View == Manina.Windows.Forms.View.Thumbnails);
            ViewGallery.Checked = (imageListView.View == Manina.Windows.Forms.View.Gallery);
            ViewPane.Checked = (imageListView.View == Manina.Windows.Forms.View.Pane);
            ViewDetails.Checked = (imageListView.View == Manina.Windows.Forms.View.Details);

            ShowFileIcons.Checked = imageListView.ShowFileIcons;
            ShowCheckboxes.Checked = imageListView.ShowCheckBoxes;
            ShowScrollbars.Checked = imageListView.ScrollBars;

            CacheOnDemand.Checked = (imageListView.CacheMode == Manina.Windows.Forms.CacheMode.OnDemand);
            AllowDuplicateFilenames.Checked = imageListView.AllowDuplicateFileNames;
            IntegralScroll.Checked = imageListView.IntegralScroll;
            MultiSelect.Checked = imageListView.MultiSelect;
            UseWIC.Checked = (imageListView.UseWIC == Manina.Windows.Forms.UseWIC.Auto);
            UseEmbeddedThumbnails.Checked = (imageListView.UseEmbeddedThumbnails == Manina.Windows.Forms.UseEmbeddedThumbnails.Auto);
            AutoRotateThumbnails.Checked = imageListView.AutoRotateThumbnails;

            if (benchMarking)
            {
                StatusLabel.Text = string.Format("Extracted thumbnail {0} of {1}", cachedThumbnailCount, imageListView.Items.Count);
            }
            else
            {
                string focused = imageListView.Items.FocusedItem == null ? "" : ", focused: " + imageListView.Items.FocusedItem.Index.ToString();
                StatusLabel.Text = string.Format("{0} items: {1} selected, {2} checked{3}",
                    imageListView.Items.Count,
                    imageListView.SelectedItems.Count,
                    imageListView.CheckedItems.Count,
                    focused);
            }
        }
        #endregion

        #region Context Menus
        // Event list
        private void ClearEventList_Click(object sender, EventArgs e)
        {
            EventsListBox.Items.Clear();
        }
        #endregion

        #region Benchmarks
        private bool benchMarking = false;
        private System.Diagnostics.Stopwatch benchmarkSW = new System.Diagnostics.Stopwatch();
        private long lastThumbnailTime = 0;
        private Manina.Windows.Forms.CacheMode oldCM;
        private int cachedThumbnailCount = 0;

        // StartBenchmark
        private void StartBenchmark_Click(object sender, EventArgs e)
        {
            benchMarking = true;

            if (ChooseBenchmarkPath.ShowDialog() == DialogResult.OK)
            {
                oldCM = imageListView.CacheMode;

                imageListView.Items.Clear();
                imageListView.CacheMode = Manina.Windows.Forms.CacheMode.Continuous;

                TestToolStrip.Enabled = false;
                imageListView.Enabled = false;
                EventsListBox.Enabled = false;

                benchMarking = true;
                CheckBenchmarkEndTimer.Enabled = true;

                benchmarkSW.Reset();
                benchmarkSW.Start();
                lastThumbnailTime = 0;
                cachedThumbnailCount = 0;

                imageListView.SuspendLayout();
                foreach (string file in Directory.GetFiles(ChooseBenchmarkPath.SelectedPath))
                    if (string.Compare(Path.GetExtension(file), ".jpg", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(Path.GetExtension(file), ".png", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(Path.GetExtension(file), ".gif", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(Path.GetExtension(file), ".bmp", StringComparison.OrdinalIgnoreCase) == 0)
                        imageListView.Items.Add(file);
            }
        }

        // Check if the benchmark ended
        private void CheckBenchmarkEndTimer_Tick(object sender, EventArgs e)
        {
            if ((benchmarkSW.ElapsedMilliseconds - lastThumbnailTime) > 2000)
            {
                CheckBenchmarkEndTimer.Enabled = false;
                benchMarking = false;
                benchmarkSW.Stop();

                TestToolStrip.Enabled = true;
                imageListView.Enabled = true;
                EventsListBox.Enabled = true;

                imageListView.CacheMode = oldCM;
                imageListView.ResumeLayout();

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Benchmark Results:");
                sb.AppendLine();
                sb.AppendFormat("Cached {0} images in {1} milliseconds.", imageListView.Items.Count, lastThumbnailTime);

                if (MessageBox.Show(sb.ToString() + Environment.NewLine + Environment.NewLine + "Copy information to clipboard?", "ImageListView Benchmark", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Clipboard.SetText(sb.ToString());
                }
            }
        }
        #endregion
    }
}
