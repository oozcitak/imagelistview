using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using Manina.Windows.Forms;
using System.Xml;
using System.ServiceModel.Syndication;

namespace ImageListViewTests
{
    public partial class TestForm : Form
    {
        #region Custom Item Adaptor
        /// <summary>
        /// A custom item adaptor.
        /// </summary>
        private class CustomAdaptor : ImageListView.ImageListViewItemAdaptor
        {
            public override Image GetThumbnail(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool useExifOrientation, bool useWIC)
            {
                string file = key as string;
                if (!string.IsNullOrEmpty(file))
                {
                    using (Image img = Image.FromFile(file))
                    {
                        Bitmap thumb = new Bitmap(img, size);
                        return thumb;
                    }
                }

                return null;
            }
            public override string GetUniqueIdentifier(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool useExifOrientation, bool useWIC)
            {
                return (string)key;
            }
            public override string GetSourceImage(object key)
            {
                string file = key as string;
                return file;
            }

            public override Utility.Tuple<ColumnType, string, object>[] GetDetails(object key, bool useWIC)
            {
                throw new NotImplementedException();
            }

            public override void Dispose()
            {
                ;
            }
        }
        #endregion

        #region Constructor
        string[] files;
        CustomAdaptor adaptor;
        ImageListView.ImageListViewItemAdaptor uriAdaptor;

        public TestForm()
        {
            InitializeComponent();

            Application.Idle += new EventHandler(Application_Idle);

            adaptor = new CustomAdaptor();
            uriAdaptor = new ImageListViewItemAdaptors.URIAdaptor();

            string picturePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            files = Directory.GetFiles(picturePath, "*.jpg");

            imageListView.ThumbnailCaching += new Manina.Windows.Forms.ThumbnailCachingEventHandler(imageListView1_ThumbnailCaching);
            imageListView.ThumbnailCached += new Manina.Windows.Forms.ThumbnailCachedEventHandler(imageListView1_ThumbnailCached);
            imageListView.CacheError += new Manina.Windows.Forms.CacheErrorEventHandler(imageListView1_CacheError);
            imageListView.ItemCollectionChanged += new ItemCollectionChangedEventHandler(imageListView_ItemCollectionChanged);
            imageListView.KeyPress += new KeyPressEventHandler(imageListView_KeyPress);

            // Find and add built-in renderers
            Assembly assembly = Assembly.GetAssembly(typeof(ImageListView));
            foreach (Type type in assembly.GetTypes())
            {
                if (type.BaseType == typeof(ImageListView.ImageListViewRenderer))
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(type.Name);
                    item.Click += SelectRenderer_Click;
                    SelectRenderer.DropDownItems.Add(item);
                }
            }
        }
        #endregion

        #region Choose Source
        private void ChooseImageSource_Click(object sender, EventArgs e)
        {
            ChooseSourcePath.Description = "Select folder containing source images.";
            ChooseSourcePath.ShowNewFolderButton = false;
            if (ChooseSourcePath.ShowDialog() == DialogResult.OK)
            {
                string picturePath = ChooseSourcePath.SelectedPath;
                files = Directory.GetFiles(picturePath, "*.jpg");

                if (files.Length == 0)
                {
                    MessageBox.Show("There are no JPEG images in the source folder.", "ImageListView Tests", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion

        #region Events
        // Search
        void imageListView_KeyPress(object sender, KeyPressEventArgs e)
        {
            string s = e.KeyChar.ToString();
            int index = imageListView.FindString(s);
            if (index != -1)
            {
                imageListView.ClearSelection();
                imageListView.Items[index].Selected = true;
                imageListView.EnsureVisible(index);
            }
        }
        // Cache error
        void imageListView1_CacheError(object sender, Manina.Windows.Forms.CacheErrorEventArgs e)
        {
            if (!benchMarking && logEventsCheckbox.Checked)
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
            else if (logEventsCheckbox.Checked)
            {
                int index = -1;
                if (e.Item != null)
                    index = e.Item.Index;
                LogEvent(string.Format("<-- {0} ({1})", index, e.Size));
            }
        }
        // Thumbnail caching
        void imageListView1_ThumbnailCaching(object sender, Manina.Windows.Forms.ThumbnailCachingEventArgs e)
        {
            if (!benchMarking && logEventsCheckbox.Checked)
            {
                int index = -1;
                if (e.Item != null)
                    index = e.Item.Index;
                LogEvent(string.Format("--> {0} ({1})", index, e.Size));
            }
        }
        // Collection changed
        void imageListView_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs e)
        {
            if (!benchMarking && logEventsCheckbox.Checked)
            {
                if (e.Action == CollectionChangeAction.Add)
                    LogEvent(string.Format("Item added at index {0}", e.Item.Index));
                else if (e.Action == CollectionChangeAction.Remove)
                    LogEvent(string.Format("Item removed from index {0}", e.Item.Index));
                else if (e.Action == CollectionChangeAction.Refresh)
                    LogEvent("Items cleared.");
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
            Random r = new Random();
            imageListView.Items.Add(files[r.Next(0, files.Length - 1)]);
        }
        // Insert item
        private void InsertItemAtIndex0_Click(object sender, EventArgs e)
        {
            Random r = new Random();
            imageListView.Items.Insert(0, files[r.Next(0, files.Length - 1)]);
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
                    imageListView.Items.Add(files[j], files[j], adaptor);
            imageListView.ResumeLayout();
        }
        // Add URI items
        private void AddURIItems_Click(object sender, EventArgs e)
        {
            imageListView.SuspendLayout();
            string query = "lemur";
            string feedUrl = "http://search.yahooapis.com/ImageSearchService/rss/imageSearch.xml?appid=yahoosearchimagerss&query=" + query;
            using (XmlReader reader = XmlReader.Create(feedUrl))
            {
                Rss20FeedFormatter rssFormatter = new Rss20FeedFormatter();
                rssFormatter.ReadFrom(reader);

                foreach (SyndicationItem rssItem in rssFormatter.Feed.Items)
                {
                    if (rssItem.Links.Count > 0)
                    {
                        // Create a virtual item passing image URL as the item key.
                        string title = rssItem.Title.Text;
                        string link = rssItem.Links[0].Uri.ToString();
                        imageListView.Items.Add(link, title, uriAdaptor);
                    }
                }
            }
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
        // Enabled
        private void SetEnabled_Click(object sender, EventArgs e)
        {
            imageListView.Enabled = !imageListView.Enabled;
        }
        // Select renderer
        private void SelectRenderer_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;

            // Find the renderer
            Assembly assembly = Assembly.GetAssembly(typeof(ImageListView));
            foreach (Type type in assembly.GetTypes())
            {
                if (type.BaseType == typeof(ImageListView.ImageListViewRenderer) && type.Name == item.Text)
                {
                    ImageListView.ImageListViewRenderer renderer = (ImageListView.ImageListViewRenderer)assembly.CreateInstance(type.FullName);
                    imageListView.SetRenderer(renderer);
                }
            }
        }
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
        // Group
        private void GroupByName_Click(object sender, EventArgs e)
        {
            if (imageListView.GroupOrder == Manina.Windows.Forms.SortOrder.None)
                imageListView.GroupOrder = Manina.Windows.Forms.SortOrder.Ascending;
            else
                imageListView.GroupOrder = Manina.Windows.Forms.SortOrder.None;
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
        // Persistent cache
        private void UsePersistentCache_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(imageListView.PersistentCacheDirectory))
            {
                ChooseSourcePath.Description = "Select folder for saving cached image thumbnails.";
                ChooseSourcePath.ShowNewFolderButton = true;
                if (ChooseSourcePath.ShowDialog() == DialogResult.OK)
                {
                    string dir = ChooseSourcePath.SelectedPath;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    imageListView.PersistentCacheDirectory = dir;
                }
            }
            else
                imageListView.PersistentCacheDirectory = string.Empty;
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
            foreach (ToolStripItem item in TestToolStrip.Items)
            {
                item.Enabled = (files.Length != 0);
            }
            ChooseImageSource.Enabled = true;

            SetEnabled.Checked = imageListView.Enabled;
            ViewThumbnails.Checked = (imageListView.View == Manina.Windows.Forms.View.Thumbnails);
            ViewGallery.Checked = (imageListView.View == Manina.Windows.Forms.View.Gallery);
            ViewPane.Checked = (imageListView.View == Manina.Windows.Forms.View.Pane);
            ViewDetails.Checked = (imageListView.View == Manina.Windows.Forms.View.Details);

            ShowFileIcons.Checked = imageListView.ShowFileIcons;
            ShowCheckboxes.Checked = imageListView.ShowCheckBoxes;
            ShowScrollbars.Checked = imageListView.ScrollBars;
            GroupByName.Checked = (imageListView.GroupOrder != Manina.Windows.Forms.SortOrder.None);

            CacheOnDemand.Checked = (imageListView.CacheMode == Manina.Windows.Forms.CacheMode.OnDemand);
            UsePersistentCache.Checked = (!string.IsNullOrEmpty(imageListView.PersistentCacheDirectory));
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
                StatusLabel.Text = string.Format("{0} items: {1} selected, {2} checked{3}", imageListView.Items.Count, imageListView.SelectedItems.Count, imageListView.CheckedItems.Count, focused);
            }
        }
        #endregion

        #region Event Context Menu
        // Event list
        private void ClearEventList_Click(object sender, EventArgs e)
        {
            EventsListBox.Items.Clear();
        }
        #endregion

        #region Item Context Menu
        // Clone
        private void cloneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ImageListViewItem item in imageListView.SelectedItems)
                imageListView.Items.Add((ImageListViewItem)item.Clone());
        }
        // Delete
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ImageListViewItem item in imageListView.SelectedItems)
                imageListView.Items.Remove(item);
        }
        // Rotate
        private void rotateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ImageListViewItem item in imageListView.SelectedItems)
            {
                item.BeginEdit();
                using (var img = Image.FromFile(item.FileName))
                {
                    img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    img.Save(item.FileName);
                }
                item.EndEdit();
            }
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
            if (System.Diagnostics.Debugger.IsAttached)
            {
                if (MessageBox.Show("Benchmarks should be run outside the IDE. Do you want to continue?", "ImageListView Tests", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                    return;
            }

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
                    if (string.Compare(Path.GetExtension(file), ".jpg", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(Path.GetExtension(file), ".png", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(Path.GetExtension(file), ".gif", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(Path.GetExtension(file), ".bmp", StringComparison.OrdinalIgnoreCase) == 0)
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
