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

            imageListView1.RetrieveVirtualItemThumbnail += new Manina.Windows.Forms.RetrieveVirtualItemThumbnailEventHandler(imageListView1_RetrieveVirtualItemThumbnail);
            imageListView1.ThumbnailCaching += new Manina.Windows.Forms.ThumbnailCachingEventHandler(imageListView1_ThumbnailCaching);
            imageListView1.ThumbnailCached += new Manina.Windows.Forms.ThumbnailCachedEventHandler(imageListView1_ThumbnailCached);
            imageListView1.CacheError += new Manina.Windows.Forms.CacheErrorEventHandler(imageListView1_CacheError);
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
            LogEvent(string.Format("!!! {0} -> {1}", (e.CacheThread == Manina.Windows.Forms.CacheThread.Thumbnail ? "Thumbnail" : "Details"), e.Error.Message));
        }
        // Thumbnail cached
        void imageListView1_ThumbnailCached(object sender, Manina.Windows.Forms.ThumbnailCachedEventArgs e)
        {
            int index = -1;
            if (e.Item != null)
                index = e.Item.Index;
            if (e.Error)
                LogEvent(string.Format("<-- {0} !!!", index));
            else
                LogEvent(string.Format("<-- {0} ({1})", index, e.Size));
        }
        // Thumbnail caching
        void imageListView1_ThumbnailCaching(object sender, Manina.Windows.Forms.ThumbnailCachingEventArgs e)
        {
            int index = -1;
            if (e.Item != null)
                index = e.Item.Index;
            LogEvent(string.Format("--> {0} ({1})", index, e.Size));
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
            imageListView1.SuspendLayout();
            for (int i = 0; i < 1000 / files.Length; i++)
                imageListView1.Items.AddRange(files);
            imageListView1.ResumeLayout();
        }
        // Add item
        private void AddOneItem_Click(object sender, EventArgs e)
        {
            imageListView1.Items.Add(files[0]);
        }
        // Insert item
        private void InsertItemAtIndex0_Click(object sender, EventArgs e)
        {
            imageListView1.Items.Insert(0, files[0]);
        }
        // Remove item
        private void RemoveItemAtIndex0_Click(object sender, EventArgs e)
        {
            imageListView1.Items.RemoveAt(0);
        }
        // Clear items
        private void ClearItems_Click(object sender, EventArgs e)
        {
            imageListView1.Items.Clear();
        }
        // Add virtual items
        private void AddVirtualItems_Click(object sender, EventArgs e)
        {
            imageListView1.SuspendLayout();
            for (int i = 0; i < 1000 / files.Length; i++)
                for (int j = 0; j < files.Length; j++)
                    imageListView1.Items.Add(files[j], files[j]);
            imageListView1.ResumeLayout();
        }
        #endregion

        #region Thumbnail Tests
        // Rebuild thumbnails
        private void RebuildThumbnails_Click(object sender, EventArgs e)
        {
            imageListView1.ClearThumbnailCache();
        }
        #endregion

        #region Appearance Settings
        // View modes
        private void ViewThumbnails_Click(object sender, EventArgs e)
        {
            imageListView1.View = Manina.Windows.Forms.View.Thumbnails;
        }
        private void ViewGallery_Click(object sender, EventArgs e)
        {
            imageListView1.View = Manina.Windows.Forms.View.Gallery;
        }
        private void ViewPane_Click(object sender, EventArgs e)
        {
            imageListView1.View = Manina.Windows.Forms.View.Pane;
        }
        private void ViewDetails_Click(object sender, EventArgs e)
        {
            imageListView1.View = Manina.Windows.Forms.View.Details;
        }
        // Show file icons
        private void ShowFileIcons_Click(object sender, EventArgs e)
        {
            imageListView1.ShowFileIcons = !imageListView1.ShowFileIcons;
        }
        // Show checkboxes
        private void ShowCheckboxes_Click(object sender, EventArgs e)
        {
            imageListView1.ShowCheckBoxes = !imageListView1.ShowCheckBoxes;
        }
        // Show scroll bars
        private void ShowScrollbars_Click(object sender, EventArgs e)
        {
            imageListView1.ScrollBars = !imageListView1.ScrollBars;
        }
        #endregion

        #region Behavior Settings
        // Cache mode
        private void CacheOnDemand_Click(object sender, EventArgs e)
        {
            if (imageListView1.CacheMode == Manina.Windows.Forms.CacheMode.Continuous)
                imageListView1.CacheMode = Manina.Windows.Forms.CacheMode.OnDemand;
            else
                imageListView1.CacheMode = Manina.Windows.Forms.CacheMode.Continuous;
        }
        // Duplicte filenames
        private void AllowDuplicateFilenames_Click(object sender, EventArgs e)
        {
            imageListView1.AllowDuplicateFileNames = !imageListView1.AllowDuplicateFileNames;
        }
        // Integral scroll
        private void IntegralScroll_Click(object sender, EventArgs e)
        {
            imageListView1.IntegralScroll = !imageListView1.IntegralScroll;
        }
        // Multi select
        private void MultiSelect_Click(object sender, EventArgs e)
        {
            imageListView1.MultiSelect = !imageListView1.MultiSelect;
        }
        // Embedded thumbnails
        private void UseEmbeddedThumbnails_Click(object sender, EventArgs e)
        {
            if (imageListView1.UseEmbeddedThumbnails == Manina.Windows.Forms.UseEmbeddedThumbnails.Auto)
                imageListView1.UseEmbeddedThumbnails = Manina.Windows.Forms.UseEmbeddedThumbnails.Never;
            else
                imageListView1.UseEmbeddedThumbnails = Manina.Windows.Forms.UseEmbeddedThumbnails.Auto;
        }
        // Auto rotate thumbnails
        private void AutoRotateThumbnails_Click(object sender, EventArgs e)
        {
            imageListView1.AutoRotateThumbnails = !imageListView1.AutoRotateThumbnails;
        }
        #endregion

        #region Update UI
        void Application_Idle(object sender, EventArgs e)
        {
            ViewThumbnails.Checked = (imageListView1.View == Manina.Windows.Forms.View.Thumbnails);
            ViewGallery.Checked = (imageListView1.View == Manina.Windows.Forms.View.Gallery);
            ViewPane.Checked = (imageListView1.View == Manina.Windows.Forms.View.Pane);
            ViewDetails.Checked = (imageListView1.View == Manina.Windows.Forms.View.Details);

            ShowFileIcons.Checked = imageListView1.ShowFileIcons;
            ShowCheckboxes.Checked = imageListView1.ShowCheckBoxes;
            ShowScrollbars.Checked = imageListView1.ScrollBars;

            CacheOnDemand.Checked = (imageListView1.CacheMode == Manina.Windows.Forms.CacheMode.OnDemand);
            AllowDuplicateFilenames.Checked = imageListView1.AllowDuplicateFileNames;
            IntegralScroll.Checked = imageListView1.IntegralScroll;
            MultiSelect.Checked = imageListView1.MultiSelect;
            UseEmbeddedThumbnails.Checked = (imageListView1.UseEmbeddedThumbnails == Manina.Windows.Forms.UseEmbeddedThumbnails.Auto);
            AutoRotateThumbnails.Checked = imageListView1.AutoRotateThumbnails;

            string focused = imageListView1.Items.FocusedItem == null ? "" : ", focused: " + imageListView1.Items.FocusedItem.Index.ToString();
            StatusLabel.Text = string.Format("{0} items: {1} selected, {2} checked{3}",
                imageListView1.Items.Count,
                imageListView1.SelectedItems.Count,
                imageListView1.CheckedItems.Count,
                focused);
        }
        #endregion

        #region Context Menus
        // Event list
        private void ClearEventList_Click(object sender, EventArgs e)
        {
            EventsListBox.Items.Clear();
        }
        #endregion
    }
}
