using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Manina.Windows.Forms
{
    public partial class DemoForm : Form
    {
        #region Member variables
        private BackgroundWorker bw = new BackgroundWorker();
        private string message = "";
        #endregion

        #region Renderer and color combobox items
        /// <summary>
        /// Represents an item in the renderer combobox.
        /// </summary>
        private struct RendererComboBoxItem
        {
            public string Name;
            public string FullName;

            public override string ToString()
            {
                return Name;
            }

            public RendererComboBoxItem(Type type)
            {
                Name = type.Name;
                FullName = type.FullName;
            }
        }

        /// <summary>
        /// Represents an item in the custom color combobox.
        /// </summary>
        private struct ColorComboBoxItem
        {
            public string Name;
            public PropertyInfo Field;

            public override string ToString()
            {
                return Name;
            }

            public ColorComboBoxItem(PropertyInfo field)
            {
                Name = field.Name;
                Field = field;
            }
        }
        #endregion

        #region Constructor
        public DemoForm()
        {
            InitializeComponent();

            // Setup the background worker
            Application.Idle += new EventHandler(Application_Idle);
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            // Find and add built-in renderers
            Assembly assembly = Assembly.GetAssembly(typeof(ImageListView));
            int i = 0;
            foreach (Type type in assembly.GetTypes())
            {
                if (type.BaseType == typeof(ImageListView.ImageListViewRenderer))
                {
                    renderertoolStripComboBox.Items.Add(new RendererComboBoxItem(type));
                    if (type.Name == "DefaultRenderer")
                        renderertoolStripComboBox.SelectedIndex = i;
                    i++;
                }
            }
            // Find and add custom colors
            Type colorType = typeof(ImageListViewColor);
            i = 0;
            foreach (PropertyInfo field in colorType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                colorToolStripComboBox.Items.Add(new ColorComboBoxItem(field));
                if (field.Name == "Default")
                    colorToolStripComboBox.SelectedIndex = i;
                i++;
            }
            // Dynamically add aligment values
            foreach (object o in Enum.GetValues(typeof(ContentAlignment)))
            {
                ToolStripMenuItem item1 = new ToolStripMenuItem(o.ToString());
                item1.Tag = o;
                item1.Click += new EventHandler(checkboxAlignmentToolStripButton_Click);
                checkboxAlignmentToolStripMenuItem.DropDownItems.Add(item1);
                ToolStripMenuItem item2 = new ToolStripMenuItem(o.ToString());
                item2.Tag = o;
                item2.Click += new EventHandler(iconAlignmentToolStripButton_Click);
                iconAlignmentToolStripMenuItem.DropDownItems.Add(item2);
            }

            imageListView1.AllowDuplicateFileNames = true;
            imageListView1.SetRenderer(new ImageListViewRenderers.DefaultRenderer());
            imageListView1.SortColumn = 0;
            imageListView1.SortOrder = SortOrder.AscendingNatural;

            string cacheDir = Path.Combine(
                Path.GetDirectoryName(new Uri(assembly.GetName().CodeBase).LocalPath),
                "Cache"
                );
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);
            imageListView1.PersistentCacheDirectory = cacheDir;
            imageListView1.Columns.Add(ColumnType.Name);
            imageListView1.Columns.Add(ColumnType.Dimensions);
            imageListView1.Columns.Add(ColumnType.FileSize);
            imageListView1.Columns.Add(ColumnType.FolderName);
            imageListView1.Columns.Add(ColumnType.DateModified);
            imageListView1.Columns.Add(ColumnType.FileType);
            var col = new ImageListView.ImageListViewColumnHeader(ColumnType.Custom, "random", "Random");
            col.Comparer = new RandomColumnComparer();
            imageListView1.Columns.Add(col);

            Text = string.Format("ImageListView Demo ({0})", imageListView1.ThumbnailExtractor.Name);

            TreeNode node = new TreeNode("Loading...", 3, 3);
            node.Tag = null;
            treeView1.Nodes.Clear();
            treeView1.Nodes.Add(node);
            while (bw.IsBusy) ;
            bw.RunWorkerAsync(node);
        }

        public class RandomColumnComparer : IComparer<ImageListViewItem>
        {
            public int Compare(ImageListViewItem x, ImageListViewItem y)
            {
                return int.Parse(x.SubItems["random"].Text).CompareTo(int.Parse(y.SubItems["random"].Text));
            }
        }
        #endregion

        #region Update UI while idle
        private void Application_Idle(object sender, EventArgs e)
        {
            detailsToolStripButton.Checked = (imageListView1.View == View.Details);
            thumbnailsToolStripButton.Checked = (imageListView1.View == View.Thumbnails);
            galleryToolStripButton.Checked = (imageListView1.View == View.Gallery);
            paneToolStripButton.Checked = (imageListView1.View == View.Pane);
            horizontalStripToolStripButton.Checked = (imageListView1.View == View.HorizontalStrip);
            verticalStripToolStripButton.Checked = (imageListView1.View == View.VerticalStrip);

            integralScrollToolStripMenuItem.Checked = imageListView1.IntegralScroll;

            showCheckboxesToolStripMenuItem.Checked = imageListView1.ShowCheckBoxes;
            showFileIconsToolStripMenuItem.Checked = imageListView1.ShowFileIcons;

            x96ToolStripMenuItem.Checked = imageListView1.ThumbnailSize == new Size(96, 96);
            x120ToolStripMenuItem.Checked = imageListView1.ThumbnailSize == new Size(120, 120);
            x200ToolStripMenuItem.Checked = imageListView1.ThumbnailSize == new Size(200, 200);

            allowCheckBoxClickToolStripMenuItem.Checked = imageListView1.AllowCheckBoxClick;
            allowColumnClickToolStripMenuItem.Checked = imageListView1.AllowColumnClick;
            allowColumnResizeToolStripMenuItem.Checked = imageListView1.AllowColumnResize;
            allowPaneResizeToolStripMenuItem.Checked = imageListView1.AllowPaneResize;
            multiSelectToolStripMenuItem.Checked = imageListView1.MultiSelect;
            allowItemReorderToolStripMenuItem.Checked = imageListView1.AllowItemReorder;
            allowDragToolStripMenuItem.Checked = imageListView1.AllowDrag;
            allowDropToolStripMenuItem.Checked = imageListView1.AllowDrop;
            allowDuplicateFilenamesToolStripMenuItem.Checked = imageListView1.AllowDuplicateFileNames;
            continuousCacheModeToolStripMenuItem.Checked = (imageListView1.CacheMode == CacheMode.Continuous);

            usingWPFWICToolStripMenuItem.Checked = (imageListView1.UseWIC);

            ContentAlignment ca = imageListView1.CheckBoxAlignment;
            foreach (ToolStripMenuItem item in checkboxAlignmentToolStripMenuItem.DropDownItems)
                item.Checked = (ContentAlignment)item.Tag == ca;
            ContentAlignment ia = imageListView1.IconAlignment;
            foreach (ToolStripMenuItem item in iconAlignmentToolStripMenuItem.DropDownItems)
                item.Checked = (ContentAlignment)item.Tag == ia;

            if (string.IsNullOrEmpty(message))
            {
                toolStripStatusLabel1.Text = string.Format("{0} Items: {1} Selected, {2} Checked",
                    imageListView1.Items.Count, imageListView1.SelectedItems.Count, imageListView1.CheckedItems.Count);
            }
            else
            {
                toolStripStatusLabel1.Text = message;
            }

            groupAscendingToolStripMenuItem.Checked = imageListView1.GroupOrder == SortOrder.Ascending;
            groupDescendingToolStripMenuItem.Checked = imageListView1.GroupOrder == SortOrder.Descending;
            sortAscendingToolStripMenuItem.Checked = imageListView1.SortOrder == SortOrder.Ascending;
            sortDescendingToolStripMenuItem.Checked = imageListView1.SortOrder == SortOrder.Descending;
        }
        #endregion

        #region Set ImageListView options
        private void label1_Click(object sender, EventArgs e)
        {
            if (ofBrowseImage.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in ofBrowseImage.FileNames)
                {
                    imageListView1.Items.Add(file);
                }
            }
        }

        private void checkboxAlignmentToolStripButton_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            ContentAlignment aligment = (ContentAlignment)item.Tag;
            imageListView1.CheckBoxAlignment = aligment;
        }

        private void iconAlignmentToolStripButton_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            ContentAlignment aligment = (ContentAlignment)item.Tag;
            imageListView1.IconAlignment = aligment;
        }

        private void renderertoolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(ImageListView));
            RendererComboBoxItem item = (RendererComboBoxItem)renderertoolStripComboBox.SelectedItem;
            ImageListView.ImageListViewRenderer renderer = (ImageListView.ImageListViewRenderer)assembly.CreateInstance(item.FullName);
            if (renderer == null)
            {
                assembly = Assembly.GetExecutingAssembly();
                renderer = (ImageListView.ImageListViewRenderer)assembly.CreateInstance(item.FullName);
            }
            colorToolStripComboBox.Enabled = renderer.CanApplyColors;
            imageListView1.SetRenderer(renderer);
            imageListView1.Focus();
        }

        private void colorToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            PropertyInfo field = ((ColorComboBoxItem)colorToolStripComboBox.SelectedItem).Field;
            ImageListViewColor color = (ImageListViewColor)field.GetValue(null, null);
            imageListView1.Colors = color;
        }

        private void detailsToolStripButton_Click(object sender, EventArgs e)
        {
            imageListView1.View = View.Details;
        }

        private void thumbnailsToolStripButton_Click(object sender, EventArgs e)
        {
            imageListView1.View = View.Thumbnails;
        }

        private void galleryToolStripButton_Click(object sender, EventArgs e)
        {
            imageListView1.View = View.Gallery;
        }

        private void paneToolStripButton_Click(object sender, EventArgs e)
        {
            imageListView1.View = View.Pane;
        }

        private void horizontalStripToolStripButton_Click(object sender, EventArgs e)
        {
            imageListView1.View = View.HorizontalStrip;
        }

        private void verticalStripToolStripButton_Click(object sender, EventArgs e)
        {
            imageListView1.View = View.VerticalStrip;
        }

        private void clearThumbsToolStripButton_Click(object sender, EventArgs e)
        {
            imageListView1.ClearThumbnailCache();
        }

        private void x96ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.ThumbnailSize = new Size(96, 96);
        }

        private void x120ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.ThumbnailSize = new Size(120, 120);
        }

        private void x200ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.ThumbnailSize = new Size(200, 200);
        }

        private void showCheckboxesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.ShowCheckBoxes = !imageListView1.ShowCheckBoxes;
        }

        private void showFileIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.ShowFileIcons = !imageListView1.ShowFileIcons;
        }

        private void allowCheckBoxClickToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowCheckBoxClick = !imageListView1.AllowCheckBoxClick;
        }

        private void allowColumnClickToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowColumnClick = !imageListView1.AllowColumnClick;
        }

        private void allowColumnResizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowColumnResize = !imageListView1.AllowColumnResize;
        }

        private void allowPaneResizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowPaneResize = !imageListView1.AllowPaneResize;
        }

        private void multiSelectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.MultiSelect = !imageListView1.MultiSelect;
        }

        private void allowItemReorderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowItemReorder = !imageListView1.AllowItemReorder;
        }

        private void allowDragToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowDrag = !imageListView1.AllowDrag;
        }

        private void allowDropToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowDrop = !imageListView1.AllowDrop;
        }

        private void allowDuplicateFilenamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.AllowDuplicateFileNames = !imageListView1.AllowDuplicateFileNames;
        }

        private void continuousCacheModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (imageListView1.CacheMode == CacheMode.Continuous)
                imageListView1.CacheMode = CacheMode.OnDemand;
            else
                imageListView1.CacheMode = CacheMode.Continuous;
        }

        private void integralScrollToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.IntegralScroll = !imageListView1.IntegralScroll;
        }

        private void imageListView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Right) != MouseButtons.None)
            {
                // Group menu
                for (int j = groupByToolStripMenuItem.DropDownItems.Count - 1; j >= 0; j--)
                {
                    if (groupByToolStripMenuItem.DropDownItems[j].Tag != null)
                        groupByToolStripMenuItem.DropDownItems.RemoveAt(j);
                }
                int i = 0;
                foreach (ImageListView.ImageListViewColumnHeader col in imageListView1.Columns)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(col.Text);
                    item.Checked = (imageListView1.GroupColumn == i);
                    item.Tag = i;
                    item.Click += new EventHandler(groupColumnMenuItem_Click);
                    groupByToolStripMenuItem.DropDownItems.Insert(i, item);
                    i++;
                }
                if (i == 0)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem("None");
                    item.Enabled = false;
                    groupByToolStripMenuItem.DropDownItems.Insert(0, item);
                }

                // Sort menu
                for (int j = sortByToolStripMenuItem.DropDownItems.Count - 1; j >= 0; j--)
                {
                    if (sortByToolStripMenuItem.DropDownItems[j].Tag != null)
                        sortByToolStripMenuItem.DropDownItems.RemoveAt(j);
                }
                i = 0;
                foreach (ImageListView.ImageListViewColumnHeader col in imageListView1.Columns)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(col.Text);
                    item.Checked = (imageListView1.SortColumn == i);
                    item.Tag = i;
                    item.Click += new EventHandler(sortColumnMenuItem_Click);
                    sortByToolStripMenuItem.DropDownItems.Insert(i, item);
                    i++;
                }
                if (i == 0)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem("None");
                    item.Enabled = false;
                    sortByToolStripMenuItem.DropDownItems.Insert(0, item);
                }

                // Show menu
                columnContextMenu.Show(imageListView1, e.Location);
            }
        }

        private void groupColumnMenuItem_Click(object sender, EventArgs e)
        {
            int i = (int)((ToolStripMenuItem)sender).Tag;
            imageListView1.GroupColumn = i;
        }

        private void sortColumnMenuItem_Click(object sender, EventArgs e)
        {
            int i = (int)((ToolStripMenuItem)sender).Tag;
            imageListView1.SortColumn = i;
        }

        private void groupAscendingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.GroupOrder = SortOrder.Ascending;
        }

        private void sortAscendingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.SortOrder = SortOrder.Ascending;
        }

        private void groupDescendingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.GroupOrder = SortOrder.Descending;
        }

        private void sortDescendingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.SortOrder = SortOrder.Descending;
        }

        private void usingWPFWICToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageListView1.UseWIC = !imageListView1.UseWIC;
        }
        #endregion

        #region Set selected image to PropertyGrid
        private void imageListView1_SelectionChanged(object sender, EventArgs e)
        {
            ImageListViewItem sel = null;
            if (imageListView1.SelectedItems.Count > 0)
                sel = imageListView1.SelectedItems[0];
            propertyGrid1.SelectedObject = sel;
        }
        #endregion

        #region Change Selection/Checkboxes
        private void imageListView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.A)
                    imageListView1.SelectAll();
                else if (e.KeyCode == Keys.U)
                    imageListView1.ClearSelection();
                else if (e.KeyCode == Keys.I)
                    imageListView1.InvertSelection();
            }
            else if (e.Alt)
            {
                if (e.KeyCode == Keys.A)
                    imageListView1.CheckAll();
                else if (e.KeyCode == Keys.U)
                    imageListView1.UncheckAll();
                else if (e.KeyCode == Keys.I)
                    imageListView1.InvertCheckState();
            }
        }
        #endregion

        #region Update folder list asynchronously
        private void PopulateListView(DirectoryInfo path)
        {
            imageListView1.Items.Clear();
            imageListView1.SuspendLayout();
            Random rnd = new Random();
            FileInfo[] files = new FileInfo[0];
            try
            {
                files = path.GetFiles("*.*");
            }
            catch
            {
                files = new FileInfo[0];
            }
            foreach (FileInfo p in files)
            {
                if (p.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".cur", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".emf", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".wmf", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    ImageListViewItem item = new ImageListViewItem(p.FullName);
                    item.SubItems.Add("random", rnd.Next(0, 999).ToString("000"));
                    imageListView1.Items.Add(item);
                }
            }
            imageListView1.ResumeLayout();
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            KeyValuePair<DirectoryInfo, bool> ktag = (KeyValuePair<DirectoryInfo, bool>)node.Tag;
            if (ktag.Value == true)
                return;
            node.Nodes.Clear();
            node.Nodes.Add("", "Loading...", 3, 3);
            while (bw.IsBusy) ;
            bw.RunWorkerAsync(node);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null) return;
            KeyValuePair<DirectoryInfo, bool> ktag = (KeyValuePair<DirectoryInfo, bool>)e.Node.Tag;
            PopulateListView(ktag.Key);
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            KeyValuePair<TreeNode, List<TreeNode>> kv = (KeyValuePair<TreeNode, List<TreeNode>>)e.Result;
            TreeNode rootNode = kv.Key;
            List<TreeNode> nodes = kv.Value;
            if (rootNode.Tag == null)
            {
                treeView1.Nodes.Clear();
                foreach (TreeNode node in nodes)
                    treeView1.Nodes.Add(node);
            }
            else
            {
                KeyValuePair<DirectoryInfo, bool> ktag = (KeyValuePair<DirectoryInfo, bool>)rootNode.Tag;
                rootNode.Tag = new KeyValuePair<DirectoryInfo, bool>(ktag.Key, true);
                rootNode.Nodes.Clear();
                foreach (TreeNode node in nodes)
                    rootNode.Nodes.Add(node);
            }
        }

        private static void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            TreeNode rootNode = e.Argument as TreeNode;

            List<TreeNode> nodes = GetNodes(rootNode);

            e.Result = new KeyValuePair<TreeNode, List<TreeNode>>(rootNode, nodes);
        }

        private static List<TreeNode> GetNodes(TreeNode rootNode)
        {
            if (rootNode.Tag == null)
            {
                List<TreeNode> volNodes = new List<TreeNode>();
                foreach (DriveInfo info in System.IO.DriveInfo.GetDrives())
                {
                    if (info.IsReady && info.DriveType == DriveType.Fixed)
                    {
                        DirectoryInfo rootPath = info.RootDirectory;
                        TreeNode volNode = new TreeNode(info.VolumeLabel + " (" + info.Name + ")", 0, 0);
                        volNode.Tag = new KeyValuePair<DirectoryInfo, bool>(rootPath, false);
                        List<TreeNode> nodes = GetNodes(volNode);
                        volNode.Tag = new KeyValuePair<DirectoryInfo, bool>(rootPath, true);
                        volNode.Nodes.Clear();
                        foreach (TreeNode node in nodes)
                            volNode.Nodes.Add(node);

                        volNode.Expand();
                        volNodes.Add(volNode);
                    }
                }

                return volNodes;
            }
            else
            {
                KeyValuePair<DirectoryInfo, bool> kv = (KeyValuePair<DirectoryInfo, bool>)rootNode.Tag;
                bool done = kv.Value;
                if (done)
                    return new List<TreeNode>();

                DirectoryInfo rootPath = kv.Key;
                List<TreeNode> nodes = new List<TreeNode>();

                DirectoryInfo[] dirs = new DirectoryInfo[0];
                try
                {
                    dirs = rootPath.GetDirectories();
                }
                catch
                {
                    return new List<TreeNode>();
                }
                foreach (DirectoryInfo info in dirs)
                {
                    if ((info.Attributes & FileAttributes.System) != FileAttributes.System)
                    {
                        TreeNode aNode = new TreeNode(info.Name, 1, 2);
                        aNode.Tag = new KeyValuePair<DirectoryInfo, bool>(info, false);
                        GetDirectories(aNode);
                        nodes.Add(aNode);
                    }
                }
                return nodes;
            }
        }

        private static void GetDirectories(TreeNode node)
        {
            KeyValuePair<DirectoryInfo, bool> ktag = (KeyValuePair<DirectoryInfo, bool>)node.Tag;
            DirectoryInfo rootPath = ktag.Key;

            DirectoryInfo[] dirs = new DirectoryInfo[0];
            try
            {
                dirs = rootPath.GetDirectories();
            }
            catch
            {
                return;
            }
            foreach (DirectoryInfo info in dirs)
            {
                if ((info.Attributes & FileAttributes.System) != FileAttributes.System)
                {
                    TreeNode aNode = new TreeNode(info.Name, 1, 2);
                    aNode.Tag = new KeyValuePair<DirectoryInfo, bool>(info, false);
                    if (GetDirCount(info) != 0)
                    {
                        aNode.Nodes.Add("Dummy1");
                    }
                    node.Nodes.Add(aNode);
                }
            }
            node.Tag = new KeyValuePair<DirectoryInfo, bool>(ktag.Key, true);
        }

        private static int GetDirCount(DirectoryInfo rootPath)
        {
            DirectoryInfo[] dirs = new DirectoryInfo[0];
            try
            {
                dirs = rootPath.GetDirectories();
            }
            catch
            {
                return 0;
            }

            return dirs.Length;
        }
        #endregion

        #region Show Message on Item Click
        private void imageListView1_ItemClick(object sender, ItemClickEventArgs e)
        {
            DoHitTest(e.Location);
        }

        private void imageListView1_ItemCheckBoxClick(object sender, ItemEventArgs e)
        {
            DoHitTest(imageListView1.PointToClient(Cursor.Position));
        }

        private void DoHitTest(Point pt)
        {
            imageListView1.HitTest(pt, out var h);
            if (h.ItemHit)
            {
                if (h.CheckBoxHit)
                    message = string.Format("Checkbox of item {0} clicked.", h.ItemIndex);
                else if (h.FileIconHit)
                    message = string.Format("File icon of item {0} clicked.", h.ItemIndex);
                else
                    message = string.Format("Item {0} clicked.", h.ItemIndex);

                messageTimer.Enabled = true;
            }
        }

        private void messageTimer_Tick(object sender, EventArgs e)
        {
            message = "";
            messageTimer.Enabled = false;
        }
        #endregion
    }
}
