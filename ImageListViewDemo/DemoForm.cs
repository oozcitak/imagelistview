using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Net;

namespace Manina.Windows.Forms
{
    public partial class DemoForm : Form
    {
        #region Member variables
        private BackgroundWorker bw = new BackgroundWorker();
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

            PopulateTreeView();
        }

        #endregion

        #region Update UI while idle
        void Application_Idle(object sender, EventArgs e)
        {
            detailsToolStripButton.Checked = (imageListView1.View == View.Details);
            thumbnailsToolStripButton.Checked = (imageListView1.View == View.Thumbnails);
            galleryToolStripButton.Checked = (imageListView1.View == View.Gallery);
            paneToolStripButton.Checked = (imageListView1.View == View.Pane);

            integralScrollToolStripMenuItem.Checked = imageListView1.IntegralScroll;

            showCheckboxesToolStripMenuItem.Checked = imageListView1.ShowCheckBoxes;
            showFileIconsToolStripMenuItem.Checked = imageListView1.ShowFileIcons;

            x96ToolStripMenuItem.Checked = imageListView1.ThumbnailSize == new Size(96, 96);
            x120ToolStripMenuItem.Checked = imageListView1.ThumbnailSize == new Size(120, 120);
            x200ToolStripMenuItem.Checked = imageListView1.ThumbnailSize == new Size(200, 200);

            allowColumnClickToolStripMenuItem.Checked = imageListView1.AllowColumnClick;
            allowColumnResizeToolStripMenuItem.Checked = imageListView1.AllowColumnResize;
            allowPaneResizeToolStripMenuItem.Checked = imageListView1.AllowPaneResize;
            multiSelectToolStripMenuItem.Checked = imageListView1.MultiSelect;
            allowDragToolStripMenuItem.Checked = imageListView1.AllowDrag;
            allowDropToolStripMenuItem.Checked = imageListView1.AllowDrop;
            allowDuplicateFilenamesToolStripMenuItem.Checked = imageListView1.AllowDuplicateFileNames;
            continuousCacheModeToolStripMenuItem.Checked = (imageListView1.CacheMode == CacheMode.Continuous);

            ContentAlignment ca = imageListView1.CheckBoxAlignment;
            foreach (ToolStripMenuItem item in checkboxAlignmentToolStripMenuItem.DropDownItems)
                item.Checked = (ContentAlignment)item.Tag == ca;
            ContentAlignment ia = imageListView1.IconAlignment;
            foreach (ToolStripMenuItem item in iconAlignmentToolStripMenuItem.DropDownItems)
                item.Checked = (ContentAlignment)item.Tag == ia;

            toolStripStatusLabel1.Text = string.Format("{0} Items: {1} Selected, {2} Checked",
                imageListView1.Items.Count, imageListView1.SelectedItems.Count, imageListView1.CheckedItems.Count);
        }
        #endregion

        #region Set ImageListView options
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
            Type t = typeof(ImageListViewColor);
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

        #region Change Selection
        private void imageListView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.A )
                    imageListView1.SelectAll();
                else if (e.KeyCode== Keys.U)
                    imageListView1.ClearSelection();
                else if (e.KeyCode== Keys.I)
                    imageListView1.InvertSelection();
            }
        }
        #endregion

        #region Update folder list asynchronously
        private void PopulateTreeView()
        {
            foreach (DriveInfo info in System.IO.DriveInfo.GetDrives())
            {
                if (info.IsReady)
                {
                    DirectoryInfo rootPath = info.RootDirectory;
                    TreeNode rootNode = new TreeNode(info.VolumeLabel + " (" + info.Name + ")", 0, 0);
                    rootNode.Tag = new KeyValuePair<DirectoryInfo, bool>(rootPath, false);
                    treeView1.Nodes.Add(rootNode);
                    List<TreeNode> nodes = GetNodes(rootNode);
                    rootNode.Tag = new KeyValuePair<DirectoryInfo, bool>(rootPath, true);
                    rootNode.Nodes.Clear();
                    foreach (TreeNode node in nodes)
                        rootNode.Nodes.Add(node);

                    rootNode.Expand();
                }
            }
            KeyValuePair<DirectoryInfo, bool> ktag = (KeyValuePair<DirectoryInfo, bool>)treeView1.Nodes[0].Tag;
            PopulateListView(ktag.Key);
        }

        private void PopulateListView(DirectoryInfo path)
        {
            imageListView1.Items.Clear();
            imageListView1.SuspendLayout();
            foreach (FileInfo p in path.GetFiles("*.*"))
            {
                if (p.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".cur", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".emf", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".wmf", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    imageListView1.Items.Add(p.FullName);
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

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            KeyValuePair<TreeNode, List<TreeNode>> kv = (KeyValuePair<TreeNode, List<TreeNode>>)e.Result;
            TreeNode rootNode = kv.Key;
            KeyValuePair<DirectoryInfo, bool> ktag = (KeyValuePair<DirectoryInfo, bool>)rootNode.Tag;
            rootNode.Tag = new KeyValuePair<DirectoryInfo, bool>(ktag.Key, true);
            List<TreeNode> nodes = kv.Value;
            rootNode.Nodes.Clear();
            foreach (TreeNode node in nodes)
                rootNode.Nodes.Add(node);
        }

        private static void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            TreeNode rootNode = e.Argument as TreeNode;

            List<TreeNode> nodes = GetNodes(rootNode);

            e.Result = new KeyValuePair<TreeNode, List<TreeNode>>(rootNode, nodes);
        }

        private static List<TreeNode> GetNodes(TreeNode rootNode)
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

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            KeyValuePair<DirectoryInfo, bool> ktag = (KeyValuePair<DirectoryInfo, bool>)e.Node.Tag;
            PopulateListView(ktag.Key);
        }
        #endregion
    }
}
