using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.ComponentModel.Design.Serialization;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents an image list view control.
    /// </summary>
    [ToolboxBitmap(typeof(ImageListView))]
    [Description("Represents an image list view control.")]
    [DefaultEvent("ItemClick")]
    [DefaultProperty("Items")]
    [Designer(typeof(ImageListViewDesigner))]
    [DesignerSerializer(typeof(ImageListViewSerializer), typeof(CodeDomSerializer))]
    [Docking(DockingBehavior.Ask)]
    public partial class ImageListView : Control
    {
        #region Constants
        /// <summary>
        /// Default width of column headers in pixels.
        /// </summary>
        internal const int DefaultColumnWidth = 100;
        private const int WS_BORDER = 0x00800000;
        private const int WS_EX_CLIENTEDGE = 0x00000200;
        #endregion

        #region Member Variables
        private BorderStyle mBorderStyle;
        private int mCacheLimitAsItemCount;
        private long mCacheLimitAsMemory;
        private ImageListViewColumnHeaderCollection mColumns;
        private Image mDefaultImage;
        private Image mErrorImage;
        private Font mHeaderFont;
        private ImageListViewItemCollection mItems;
        private Size mItemMargin;
        internal ImageListViewRenderer mRenderer;
        internal ImageListViewSelectedItemCollection mSelectedItems;
        private ColumnType mSortColumn;
        private SortOrder mSortOrder;
        private Size mThumbnailSize;
        private UseEmbeddedThumbnails mUseEmbeddedThumbnails;
        private View mView;
        private Point mViewOffset;

        // Layout variables
        private System.Windows.Forms.Timer scrollTimer;
        internal System.Windows.Forms.HScrollBar hScrollBar;
        internal System.Windows.Forms.VScrollBar vScrollBar;
        internal ImageListViewLayoutManager layoutManager;
        private bool disposed;

        // Interaction variables
        internal NavInfo nav;

        // Cache thread
        internal ImageListViewCacheManager cacheManager;
        internal ImageListViewItemCacheManager itemCacheManager;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets whether column headers respond to mouse clicks.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether column headers respond to mouse clicks."), DefaultValue(true)]
        public bool AllowColumnClick { get; set; }
        /// <summary>
        /// Gets or sets whether column headers can be resized with the mouse.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether column headers can be resized with the mouse."), DefaultValue(true)]
        public bool AllowColumnResize { get; set; }
        /// <summary>
        /// Gets or sets whether the user can drag items for drag-and-drop operations.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether the user can drag items for drag-and-drop operations."), DefaultValue(false)]
        public bool AllowDrag { get; set; }
        /// <summary>
        /// Gets or sets whether duplicate items (image files pointing to the same path 
        /// on the file system) are allowed.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether duplicate items (image files pointing to the same path on the file system) are allowed."), DefaultValue(false)]
        public bool AllowDuplicateFileNames { get; set; }
        /// <summary>
        /// Gets or sets whether the user can reorder items by dragging.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether the user can reorder items by dragging."), DefaultValue(false)]
        public bool AllowItemDrag { get; set; }
        /// <summary>
        /// Gets or sets the background color of the control.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the background color of the control."), DefaultValue(typeof(Color), "Window")]
        public override Color BackColor { get { return base.BackColor; } set { base.BackColor = value; } }
        /// <summary>
        /// Gets or sets the border style of the control.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the border style of the control."), DefaultValue(typeof(BorderStyle), "Fixed3D")]
        public BorderStyle BorderStyle { get { return mBorderStyle; } set { mBorderStyle = value; UpdateStyles(); } }
        /// <summary>
        /// Gets or sets the cache limit as either the count of thumbnail images or the memory allocated for cache (e.g. 10MB).
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the cache limit as either the count of thumbnail images or the memory allocated for cache (e.g. 10MB)."), DefaultValue("20MB")]
        public string CacheLimit
        {
            get
            {
                if (mCacheLimitAsMemory != 0)
                    return (mCacheLimitAsMemory / 1024 / 1024).ToString() + "MB";
                else
                    return mCacheLimitAsItemCount.ToString();
            }
            set
            {
                string slimit = value;
                int limit = 0;
                if ((slimit.EndsWith("MB", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(slimit.Substring(0, slimit.Length - 2).Trim(), out limit)) ||
                    (slimit.EndsWith("MiB", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(slimit.Substring(0, slimit.Length - 3).Trim(), out limit)))
                {
                    mCacheLimitAsItemCount = 0;
                    mCacheLimitAsMemory = limit * 1024 * 1024;
                    if (cacheManager != null)
                        cacheManager.CacheLimitAsMemory = mCacheLimitAsMemory;
                }
                else if (int.TryParse(slimit, out limit))
                {
                    mCacheLimitAsMemory = 0;
                    mCacheLimitAsItemCount = limit;
                    if (cacheManager != null)
                        cacheManager.CacheLimitAsItemCount = mCacheLimitAsItemCount;
                }
                else
                    throw new ArgumentException("Cache limit must be specified as either the count of thumbnail images or the memory allocated for cache (eg 10MB)", "value");
            }
        }
        /// <summary>
        /// Gets or sets the collection of columns of the image list view.
        /// </summary>
        [Category("Appearance"), Description("Gets the collection of columns of the image list view."), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ImageListViewColumnHeaderCollection Columns { get { return mColumns; } internal set { mColumns = value; mRenderer.Refresh(); } }
        /// <summary>
        /// Gets or sets the placeholder image.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the placeholder image.")]
        public Image DefaultImage { get { return mDefaultImage; } set { mDefaultImage = value; } }
        /// <summary>
        /// Gets the rectangle that represents the display area of the control.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets the rectangle that represents the display area of the control.")]
        public override Rectangle DisplayRectangle
        {
            get
            {
                return layoutManager.ClientArea;
            }
        }
        /// <summary>
        /// Gets or sets the error image.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the error image.")]
        public Image ErrorImage { get { return mErrorImage; } set { mErrorImage = value; } }
        /// <summary>
        /// Gets or sets the font of the column headers.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the font of the column headers."), DefaultValue(typeof(Font), "Microsoft Sans Serif; 8.25pt")]
        public Font HeaderFont
        {
            get
            {
                return mHeaderFont;
            }
            set
            {
                if (mHeaderFont != null)
                    mHeaderFont.Dispose();
                mHeaderFont = (Font)value.Clone();
                mRenderer.Refresh();
            }
        }
        /// <summary>
        /// Gets the collection of items contained in the image list view.
        /// </summary>
        [Browsable(false), Category("Behavior"), Description("Gets the collection of items contained in the image list view.")]
        public ImageListView.ImageListViewItemCollection Items { get { return mItems; } }
        /// <summary>
        /// Gets or sets the spacing between items.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the spacing between items."), DefaultValue(typeof(Size), "4,4")]
        public Size ItemMargin { get { return mItemMargin; } set { mItemMargin = value; mRenderer.Refresh(); } }
        /// <summary>
        /// Gets the collection of selected items contained in the image list view.
        /// </summary>
        [Browsable(false), Category("Behavior"), Description("Gets the collection of selected items contained in the image list view.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ImageListView.ImageListViewSelectedItemCollection SelectedItems { get { return mSelectedItems; } }
        /// <summary>
        /// Gets or sets the sort column.
        /// </summary>
        [Category("Appearance"), DefaultValue(typeof(ColumnType), "Name"), Description("Gets or sets the sort column.")]
        public ColumnType SortColumn { get { return mSortColumn; } set { mSortColumn = value; Sort(); } }
        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        [Category("Appearance"), DefaultValue(typeof(SortOrder), "None"), Description("Gets or sets the sort order.")]
        public SortOrder SortOrder { get { return mSortOrder; } set { mSortOrder = value; Sort(); } }
        /// <summary>
        /// This property is not relevant for this class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false), Bindable(false), DefaultValue(null)]
        public override string Text { get; set; }
        /// <summary>
        /// Gets or sets the size of image thumbnails.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the size of image thumbnails."), DefaultValue(typeof(Size), "96,96")]
        public Size ThumbnailSize
        {
            get
            {
                return mThumbnailSize;
            }
            set
            {
                if (mThumbnailSize != value)
                {
                    mThumbnailSize = value;
                    cacheManager.Clear();
                    mRenderer.Refresh();
                }
            }
        }
        /// <summary>
        /// Gets or sets the embedded thumbnails extraction behavior.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the embedded thumbnails extraction behavior."), DefaultValue(typeof(UseEmbeddedThumbnails), "Auto")]
        public UseEmbeddedThumbnails UseEmbeddedThumbnails
        {
            get
            {
                return mUseEmbeddedThumbnails;
            }
            set
            {
                if (mUseEmbeddedThumbnails != value)
                {
                    mUseEmbeddedThumbnails = value;
                    cacheManager.Clear();
                    mRenderer.Refresh();
                }
            }
        }
        /// <summary>
        /// Gets or sets the view mode of the image list view.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the view mode of the image list view."), DefaultValue(typeof(View), "Thumbnails")]
        public View View
        {
            get
            {
                return mView;
            }
            set
            {
                mRenderer.SuspendPaint();
                int current = layoutManager.FirstVisible;
                mView = value;
                layoutManager.Update();
                EnsureVisible(current);
                mRenderer.Refresh();
                mRenderer.ResumePaint();
            }
        }
        /// <summary>
        /// Gets or sets the scroll offset.
        /// </summary>
        internal Point ViewOffset { get { return mViewOffset; } set { mViewOffset = value; } }
        /// <summary>
        /// Gets the scroll orientation.
        /// </summary>
        internal ScrollOrientation ScrollOrientation { get { return (mView == View.Gallery ? ScrollOrientation.HorizontalScroll : ScrollOrientation.VerticalScroll); } }
        /// <summary>
        /// Gets the required creation parameters when the control handle is created.
        /// </summary>
        /// <value></value>
        /// <returns>A CreateParams that contains the required creation parameters.</returns>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams p = base.CreateParams;
                p.Style &= ~WS_BORDER;
                p.ExStyle &= ~WS_EX_CLIENTEDGE;
                if (mBorderStyle == BorderStyle.Fixed3D)
                    p.ExStyle |= WS_EX_CLIENTEDGE;
                else if (mBorderStyle == BorderStyle.FixedSingle)
                    p.Style |= WS_BORDER;
                return p;
            }
        }
        #endregion

        #region Constructor
        public ImageListView()
        {
            SetRenderer(new ImageListViewRenderer());

            AllowColumnClick = true;
            AllowColumnResize = true;
            AllowDrag = false;
            AllowDuplicateFileNames = false;
            AllowItemDrag = false;
            BackColor = SystemColors.Window;
            mBorderStyle = BorderStyle.Fixed3D;
            mCacheLimitAsItemCount = 0;
            mCacheLimitAsMemory = 20 * 1024 * 1024;
            mColumns = new ImageListViewColumnHeaderCollection(this);
            DefaultImage = Utility.ImageFromBase64String(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAgY0hSTQAAeiYAAICEAAD6AAAAgOgAAHUwAADqYAAAOpgAABdwnLpRPAAAAdRJREFUOE+lk81LG1EUxeufVXFVUcSNlFIJ6ErUjaQUtYrfxrgQQewq2NKWVpN0nKdGRV1ELZRWdO9ClMyorUnTmMxnzAxicnrfWKIxUUEHfsN83Dn3zH3nlQF48qiDCzT0iROubhavdgtaeWsJmunZNSrbBO15pzDpNOcnVw/7G5HlnGGaMNMWDEInVAcbimkjZdg4dbAQV3TUvhbVvADvrBsmGj0+dH0KYuCLH72fGXqnw+ifCWPQH8ZwcB1eYQMtw5NI6Baq3EzLC1SQbd7Z41/DfMTAonyGkGQgJGcQOrSwdGRj+fgcK78vMMa+Ia6WEOCW+cvVaA6rJ9lb4TV/1Aw5EAsd8P/1BtawKJ2RA+ospcmNDnZgYo5g+wYWDlSMBlYQU9LFAopp4ZXvAz5uxzC19Qu+n4cY29zBSPgE3v+8+76LvvcBxFIlHKRouvVDQXSI8iWzEjoECe1fIwW8HPAXC/C1T9JkXSNzeDMfvRNeE73pgAucao8QeNoc1JIk8KzJ47i4C14TTWZQ2XZtFcodgQz24lkidw9ZErAKBWrcwnEiqUCjQWoUW9WBYkz3ShE2Ek6U2VWUX3SK43Xt7AcPB7d2H7QPNPrmbT7K/OKh/ANGwthSNAtyCAAAAABJRU5ErkJggg==");
            ErrorImage = Utility.ImageFromBase64String(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAgY0hSTQAAeiYAAICEAAD6AAAAgOgAAHUwAADqYAAAOpgAABdwnLpRPAAAAnpJREFUOE+lk/9LE3EYx+tf0TDtCyERhD9ofkHMvhg780t6zE3nZi2njExqN/dNckWihphEqJlDISiwgkpNJCQijLKIuozUbmtufpl3zpsF7+4+cDeViKAHnoODz/v1fp7n83x2AtjxXyEDNuev5rOJG54aJuYysusOA79mr+R5m46NXNIyyxfpxO3nt4glIRVzmfxL7loIvg6ID3tJ8r52BBkTQtZSf7C+iNoMUQExt4kSndVCpMuDn6NDEPuvIuo9R1K848XGyCDCHU34btYIczUFKoQARKcxIdpk4Fa63ES85qokqQRv14G3VSD2xIeF65fxtSqfY/V5CWR+8kfq0x52muNipx6CQ68CpP6x0qjFcgMN8dEAZupofKSz7SpAOsDKfYp9LUSoOCoEWbhkLUe4rgyRGy6Eb3rxriSdVQGLDWVR8XEfBI+RlKo4KgBZGKo9gwVzKYIWLSKDtzBFpUVVQLC+OLo+3ItVh0EtVXbc+DRNGLLwR00JAsZiBMw0IgPdeFVwKA7gzmvYlZ5WCN0etVTZMXK7Dfx9HxH6DUXg9KcR8jIItDdjMj813sKs6aT9m7UC68N31VJlRyVk4byuEHNaCqtDPXirO4WJ3P3xIX6pPJrwuSKX87c0Yu1Bv+q42OGV7r6FCGdpDRHPMBaM5+zlxrJS4tcoD+NDeRY1XZohzHsuQLjXh/A1aWmM5ZivLsPCFUYanCS2WfA8O0UYzdy9dZGU1XxTmEa91hz2v6/SINAmzaO3E4s9neBa3Ziij2M0M9n/LCPpz6usQF6eOJg4eSyVeZF3gJ3I3ceP5+zhx7KS2ZEjSczT9F1/f0zbX9q//P8GR0WnSFUgshMAAAAASUVORK5CYII=");
            HeaderFont = this.Font;
            mItems = new ImageListViewItemCollection(this);
            mItemMargin = new Size(4, 4);
            mSelectedItems = new ImageListViewSelectedItemCollection(this);
            mSortColumn = ColumnType.Name;
            mSortOrder = SortOrder.None;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.Selectable | ControlStyles.UserMouse, true);
            Size = new Size(120, 100);
            mThumbnailSize = new Size(96, 96);
            mUseEmbeddedThumbnails = UseEmbeddedThumbnails.Auto;
            mView = View.Thumbnails;

            scrollTimer = new System.Windows.Forms.Timer();
            scrollTimer.Interval = 100;
            scrollTimer.Enabled = false;
            scrollTimer.Tick += new EventHandler(scrollTimer_Tick);

            mViewOffset = new Point(0, 0);
            hScrollBar = new System.Windows.Forms.HScrollBar();
            vScrollBar = new System.Windows.Forms.VScrollBar();
            hScrollBar.Visible = false;
            vScrollBar.Visible = false;
            hScrollBar.Scroll += new ScrollEventHandler(hScrollBar_Scroll);
            vScrollBar.Scroll += new ScrollEventHandler(vScrollBar_Scroll);
            Controls.Add(hScrollBar);
            Controls.Add(vScrollBar);
            layoutManager = new ImageListViewLayoutManager(this);

            nav = new NavInfo();

            cacheManager = new ImageListViewCacheManager(this);
            itemCacheManager = new ImageListViewItemCacheManager(this);

            disposed = false;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Clears the thumbnail cache.
        /// </summary>
        public void ClearThumbnailCache()
        {
            cacheManager.Clear();
            mRenderer.Refresh();
        }
        /// <summary>
        /// Temporarily suspends the layout logic for the control.
        /// </summary>
        public new void SuspendLayout()
        {
            base.SuspendLayout();
            mRenderer.SuspendPaint(true);
        }
        /// <summary>
        /// Resumes usual layout logic.
        /// </summary>
        public new void ResumeLayout()
        {
            ResumeLayout(false);
        }
        /// <summary>
        /// Resumes usual layout logic, optionally forcing an immediate layout of pending layout requests.
        /// </summary>
        /// <param name="performLayout">true to execute pending layout requests; otherwise, false.</param>
        public new void ResumeLayout(bool performLayout)
        {
            base.ResumeLayout(performLayout);
            if (performLayout) mRenderer.Refresh();
            mRenderer.ResumePaint(true);
        }
        /// <summary>
        /// Sets the properties of the specified column header.
        /// </summary>
        /// <param name="type">The column header to modify.</param>
        /// <param name="text">Column header text.</param>
        /// <param name="width">Width (in pixels) of the column header.</param>
        /// <param name="displayIndex">Display index of the column header.</param>
        /// <param name="visible">true if the column header will be shown; otherwise false.</param>
        public void SetColumnHeader(ColumnType type, string text, int width, int displayIndex, bool visible)
        {
            mRenderer.SuspendPaint();
            ImageListViewColumnHeader col = Columns[type];
            col.Text = text;
            col.Width = width;
            col.DisplayIndex = displayIndex;
            col.Visible = visible;
            mRenderer.Refresh();
            mRenderer.ResumePaint();
        }
        /// <summary>
        /// Sets the properties of the specified column header.
        /// </summary>
        /// <param name="type">The column header to modify.</param>
        /// <param name="width">Width (in pixels) of the column header.</param>
        /// <param name="displayIndex">Display index of the column header.</param>
        /// <param name="visible">true if the column header will be shown; otherwise false.</param>
        public void SetColumnHeader(ColumnType type, int width, int displayIndex, bool visible)
        {
            mRenderer.SuspendPaint();
            ImageListViewColumnHeader col = Columns[type];
            col.Width = width;
            col.DisplayIndex = displayIndex;
            col.Visible = visible;
            mRenderer.Refresh();
            mRenderer.ResumePaint();
        }
        /// <summary>
        /// Sets the renderer for this instance.
        /// </summary>
        public void SetRenderer(ImageListViewRenderer renderer)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");

            if (mRenderer != null)
                mRenderer.Dispose();
            mRenderer = renderer;
            mRenderer.mImageListView = this;
            if (layoutManager != null)
                layoutManager.Update(true);
            mRenderer.Refresh(true);
        }
        /// <summary>
        /// Sorts the items.
        /// </summary>
        public void Sort()
        {
            mItems.Sort();
            mRenderer.Refresh();
        }
        /// <summary>
        /// Marks all items as selected.
        /// </summary>
        public void SelectAll()
        {
            mRenderer.SuspendPaint();

            foreach (ImageListViewItem item in Items)
                item.mSelected = true;

            OnSelectionChangedInternal();

            mRenderer.Refresh();
            mRenderer.ResumePaint();
        }
        /// <summary>
        /// Marks all items as unselected.
        /// </summary>
        public void ClearSelection()
        {
            mRenderer.SuspendPaint();
            mSelectedItems.Clear();
            mRenderer.Refresh();
            mRenderer.ResumePaint();
        }
        /// <summary>
        /// Determines the image list view element under the specified coordinates.
        /// </summary>
        /// <param name="pt">The client coordinates of the point to be tested.</param>
        /// <param name="hitInfo">Details of the hit test.</param>
        /// <returns>true if the point is over an item or column; false otherwise.</returns>
        public bool HitTest(Point pt, out HitInfo hitInfo)
        {
            int sepSize = 12;

            hitInfo = new HitInfo();
            hitInfo.ColumnHit = false;
            hitInfo.ItemHit = false;
            hitInfo.ColumnSeparatorHit = false;
            hitInfo.ColumnIndex = (ColumnType)(-1);
            hitInfo.ItemIndex = -1;
            hitInfo.ColumnSeparator = (ColumnType)(-1);
            int headerHeight = mRenderer.MeasureColumnHeaderHeight();

            if (View == View.Details && pt.Y <= headerHeight)
            {
                hitInfo.InHeaderArea = true;
                int i = 0;
                int x = layoutManager.ColumnHeaderBounds.Left;
                foreach (ImageListViewColumnHeader col in Columns.GetUIColumns())
                {
                    // Over a column?
                    if (pt.X >= x && pt.X < x + col.Width + sepSize / 2)
                    {
                        hitInfo.ColumnHit = true;
                        hitInfo.ColumnIndex = col.Type;
                    }
                    // Over a colummn separator?
                    if (pt.X > x + col.Width - sepSize / 2 && pt.X < x + col.Width + sepSize / 2)
                    {
                        hitInfo.ColumnSeparatorHit = true;
                        hitInfo.ColumnSeparator = col.Type;
                    }
                    if (hitInfo.ColumnHit) break;
                    x += col.Width;
                    i++;
                }
            }
            else if (ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                hitInfo.InItemArea = true;
                // Normalize to item area coordinates
                pt.X -= layoutManager.ItemAreaBounds.Left;
                pt.Y -= layoutManager.ItemAreaBounds.Top;

                if (pt.X > 0 && pt.Y > 0)
                {
                    int col = (pt.X + mViewOffset.X) / layoutManager.ItemSizeWithMargin.Width;
                    int row = (pt.Y + mViewOffset.Y) / layoutManager.ItemSizeWithMargin.Height;

                    if (col <= layoutManager.Cols)
                    {
                        int index = row * layoutManager.Cols + col;
                        if (index >= 0 && index <= Items.Count - 1)
                        {
                            Rectangle bounds = layoutManager.GetItemBounds(index);
                            if (bounds.Contains(pt.X + layoutManager.ItemAreaBounds.Left, pt.Y + layoutManager.ItemAreaBounds.Top))
                            {
                                hitInfo.ItemHit = true;
                                hitInfo.ItemIndex = index;
                            }
                        }
                    }
                }
            }
            else if (ScrollOrientation == ScrollOrientation.HorizontalScroll)
            {
                hitInfo.InItemArea = true;
                // Normalize to item area coordinates
                pt.X -= layoutManager.ItemAreaBounds.Left;
                pt.Y -= layoutManager.ItemAreaBounds.Top;

                if (pt.X > 0 && pt.Y > 0)
                {
                    int col = (pt.X + mViewOffset.X) / layoutManager.ItemSizeWithMargin.Width;
                    int row = (pt.Y + mViewOffset.Y) / layoutManager.ItemSizeWithMargin.Height;

                    int index = row * layoutManager.Cols + col;
                    if (index >= 0 && index < Items.Count)
                    {
                        Rectangle bounds = layoutManager.GetItemBounds(index);
                        if (bounds.Contains(pt.X + layoutManager.ItemAreaBounds.Left, pt.Y + layoutManager.ItemAreaBounds.Top))
                        {
                            hitInfo.ItemHit = true;
                            hitInfo.ItemIndex = index;
                        }
                    }
                }
            }

            return (hitInfo.ColumnHit || hitInfo.ColumnSeparatorHit || hitInfo.ItemHit);
        }
        /// <summary>
        /// Scrolls the image list view to ensure that the item with the specified 
        /// index is visible on the screen.
        /// </summary>
        /// <param name="itemIndex">The index of the item to make visible.</param>
        /// <returns>true if the item was made visible; otherwise false (item is already visible or the image list view is empty).</returns>
        public bool EnsureVisible(int itemIndex)
        {
            if (itemIndex == -1) return false;
            if (Items.Count == 0) return false;

            // Already visible?
            Rectangle bounds = layoutManager.ItemAreaBounds;
            Rectangle itemBounds = layoutManager.GetItemBounds(itemIndex);
            if (!bounds.Contains(itemBounds))
            {
                if (ScrollOrientation == ScrollOrientation.HorizontalScroll)
                {
                    int delta = 0;
                    if (itemBounds.Left < bounds.Left)
                        delta = bounds.Left - itemBounds.Left;
                    else
                    {
                        int topItemIndex = itemIndex - (layoutManager.Cols - 1) * layoutManager.Rows;
                        if (topItemIndex < 0) topItemIndex = 0;
                        delta = bounds.Left - layoutManager.GetItemBounds(topItemIndex).Left;
                    }
                    int newXOffset = mViewOffset.X - delta;
                    if (newXOffset > hScrollBar.Maximum - hScrollBar.LargeChange + 1)
                        newXOffset = hScrollBar.Maximum - hScrollBar.LargeChange + 1;
                    if (newXOffset < hScrollBar.Minimum)
                        newXOffset = hScrollBar.Minimum;
                    mViewOffset.X = newXOffset;
                    mViewOffset.Y = 0;
                    hScrollBar.Value = newXOffset;
                    vScrollBar.Value = 0;
                }
                else
                {
                    int delta = 0;
                    if (itemBounds.Top < bounds.Top)
                        delta = bounds.Top - itemBounds.Top;
                    else
                    {
                        int topItemIndex = itemIndex - (layoutManager.Rows - 1) * layoutManager.Cols;
                        if (topItemIndex < 0) topItemIndex = 0;
                        delta = bounds.Top - layoutManager.GetItemBounds(topItemIndex).Top;
                    }
                    int newYOffset = mViewOffset.Y - delta;
                    if (newYOffset > vScrollBar.Maximum - vScrollBar.LargeChange + 1)
                        newYOffset = vScrollBar.Maximum - vScrollBar.LargeChange + 1;
                    if (newYOffset < vScrollBar.Minimum)
                        newYOffset = vScrollBar.Minimum;
                    mViewOffset.X = 0;
                    mViewOffset.Y = newYOffset;
                    hScrollBar.Value = 0;
                    vScrollBar.Value = newYOffset;
                }
                mRenderer.Refresh();
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Determines whether the specified item is visible on the screen.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>An ItemVisibility value.</returns>
        public ItemVisibility IsItemVisible(ImageListViewItem item)
        {
            return IsItemVisible(mItems.IndexOf(item));
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Returns the item index after applying the given navigation key.
        /// </summary>
        private int ApplyNavKey(int index, System.Windows.Forms.Keys key)
        {
            if (key == Keys.Up && index >= layoutManager.Cols)
                index -= layoutManager.Cols;
            else if (key == Keys.Down && index < Items.Count - layoutManager.Cols)
                index += layoutManager.Cols;
            else if (key == Keys.Left && index > 0)
                index--;
            else if (key == Keys.Right && index < Items.Count - 1)
                index++;
            else if (key == Keys.PageUp && index >= layoutManager.Cols * (layoutManager.Rows - 1))
                index -= layoutManager.Cols * (layoutManager.Rows - 1);
            else if (key == Keys.PageDown && index < Items.Count - layoutManager.Cols * (layoutManager.Rows - 1))
                index += layoutManager.Cols * (layoutManager.Rows - 1);
            else if (key == Keys.Home)
                index = 0;
            else if (key == Keys.End)
                index = Items.Count - 1;

            if (index < 0)
                index = 0;
            else if (index > Items.Count - 1)
                index = Items.Count - 1;

            return index;
        }
        /// <summary>
        /// Determines whether the specified item is visible on the screen.
        /// </summary>
        /// <param name="item">The Guid of the item to test.</param>
        /// <returns>true if the item is visible or partially visible; otherwise false.</returns>
        internal bool IsItemVisible(Guid guid)
        {
            return layoutManager.IsItemVisible(guid);
        }
        /// <summary>
        /// Determines whether the specified item is visible on the screen.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>An ItemVisibility value.</returns>
        internal ItemVisibility IsItemVisible(int itemIndex)
        {
            if (mItems.Count == 0) return ItemVisibility.NotVisible;
            if (itemIndex < 0 || itemIndex > mItems.Count - 1) return ItemVisibility.NotVisible;

            if (itemIndex < layoutManager.FirstPartiallyVisible || itemIndex > layoutManager.LastPartiallyVisible)
                return ItemVisibility.NotVisible;
            else if (itemIndex >= layoutManager.FirstVisible && itemIndex <= layoutManager.LastVisible)
                return ItemVisibility.Visible;
            else
                return ItemVisibility.PartiallyVisible;
        }
        /// <summary>
        /// Gets the guids of visible items.
        /// </summary>
        internal Dictionary<Guid, bool> GetVisibleItems()
        {
            Dictionary<Guid, bool> visible = new Dictionary<Guid, bool>();
            if (layoutManager.FirstPartiallyVisible != -1 && layoutManager.LastPartiallyVisible != -1)
            {
                int start = layoutManager.FirstPartiallyVisible;
                int end = layoutManager.LastPartiallyVisible;

                start -= layoutManager.Cols * layoutManager.Rows;
                end += layoutManager.Cols * layoutManager.Rows;

                start = Math.Min(mItems.Count - 1, Math.Max(0, start));
                end = Math.Min(mItems.Count - 1, Math.Max(0, end));

                for (int i = start; i <= end; i++)
                    visible.Add(mItems[i].Guid, false);
            }
            return visible;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Raises the VisibleChanged event when the Visible property 
        /// value of the control's container changes.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnParentVisibleChanged(EventArgs e)
        {
            Form parent = this.FindForm();
            if (parent != null)
                ((Form)parent).FormClosing += new FormClosingEventHandler(ImageListView_ParentFormClosing);
        }
        /// <summary>
        /// Handles the FormClosing event of the parent form.
        /// </summary>
        void ImageListView_ParentFormClosing(object sender, FormClosingEventArgs e)
        {
            cacheManager.Stop();
            itemCacheManager.Stop();
        }
        /// <summary>
        /// Handles the DragOver event.
        /// </summary>
        protected override void OnDragOver(DragEventArgs e)
        {
            if (AllowItemDrag && nav.SelfDragging)
            {
                e.Effect = DragDropEffects.Move;

                // Calculate the location of the insertion cursor
                Point pt = new Point(e.X, e.Y);
                pt = PointToClient(pt);
                // Normalize to item area coordinates
                pt.X -= layoutManager.ItemAreaBounds.Left;
                pt.Y -= layoutManager.ItemAreaBounds.Top;
                // Row and column mouse is over
                bool dragCaretOnRight = false;
                int col = pt.X / layoutManager.ItemSizeWithMargin.Width;
                int row = (pt.Y + mViewOffset.Y) / layoutManager.ItemSizeWithMargin.Height;
                if (col > layoutManager.Cols - 1)
                {
                    col = layoutManager.Cols - 1;
                    dragCaretOnRight = true;
                }
                // Index of the item mouse is over
                int index = row * layoutManager.Cols + col;
                if (index < 0) index = 0;
                if (index > Items.Count - 1)
                {
                    index = Items.Count - 1;
                    dragCaretOnRight = true;
                }
                if (index != nav.DragIndex || dragCaretOnRight != nav.DragCaretOnRight)
                {
                    nav.DragIndex = index;
                    nav.DragCaretOnRight = dragCaretOnRight;
                    mRenderer.Refresh(true);
                }
            }
            else
                e.Effect = DragDropEffects.None;

            base.OnDragOver(e);
        }
        /// <summary>
        /// Handles the DragEnter event.
        /// </summary>
        protected override void OnDragEnter(DragEventArgs e)
        {
            if (!nav.SelfDragging && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;

            base.OnDragEnter(e);
        }
        /// <summary>
        /// Handles the DragLeave event.
        /// </summary>
        protected override void OnDragLeave(EventArgs e)
        {
            if (AllowItemDrag && nav.SelfDragging)
            {
                nav.DragIndex = -1;
                mRenderer.Refresh(true);
            }

            base.OnDragLeave(e);
        }

        /// <summary>
        /// Handles the DragDrop event.
        /// </summary>
        protected override void OnDragDrop(DragEventArgs e)
        {
            mRenderer.SuspendPaint();

            if (nav.SelfDragging)
            {
                // Reorder items
                List<ImageListViewItem> draggedItems = new List<ImageListViewItem>();
                int i = nav.DragIndex;
                foreach (ImageListViewItem item in mSelectedItems)
                {
                    if (item.Index <= i) i--;
                    draggedItems.Add(item);
                    mItems.RemoveInternal(item);
                }
                if (i < 0) i = 0;
                if (i > mItems.Count - 1) i = mItems.Count - 1;
                if (nav.DragCaretOnRight) i++;
                foreach (ImageListViewItem item in draggedItems)
                {
                    item.mSelected = false;
                    mItems.InsertInternal(i, item);
                    i++;
                }
                OnSelectionChanged(new EventArgs());
            }
            else
            {
                // Add items
                foreach (string filename in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    try
                    {
                        using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                        {
                            using (Image img = Image.FromStream(stream, false, false))
                            {
                                mItems.Add(filename);
                            }
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }

            nav.DragIndex = -1;
            nav.SelfDragging = false;

            mRenderer.ResumePaint();

            base.OnDragDrop(e);
        }
        /// <summary>
        /// Handles the Scroll event of the vScrollBar control.
        /// </summary>
        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            mViewOffset.Y = e.NewValue;
            mRenderer.Refresh();
        }
        /// <summary>
        /// Handles the Scroll event of the hScrollBar control.
        /// </summary>
        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            mViewOffset.X = e.NewValue;
            mRenderer.Refresh();
        }
        /// <summary>
        /// Handles the Tick event of the scrollTimer control.
        /// </summary>
        private void scrollTimer_Tick(object sender, EventArgs e)
        {
            int delta = (int)scrollTimer.Tag;
            if (nav.Dragging)
            {
                Point location = base.PointToClient(Control.MousePosition);
                OnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, location.X, location.Y, 0));
            }
            OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta));
        }
        /// <summary>
        /// Handles the Resize event.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (!disposed && mRenderer != null)
                mRenderer.RecreateBuffer();

            if (hScrollBar == null)
                return;

            layoutManager.Update();
            mRenderer.Refresh();
        }
        /// <summary>
        /// Handles the Paint event.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!disposed && mRenderer != null)
                mRenderer.Render(e.Graphics);
        }
        /// <summary>
        /// Handles the MouseDown event.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            nav.ClickedItem = null;
            nav.HoveredItem = null;
            nav.HoveredColumn = (ColumnType)(-1);
            nav.HoveredSeparator = (ColumnType)(-1);
            nav.SelSeperator = (ColumnType)(-1);

            HitInfo h;
            HitTest(e.Location, out h);

            if (h.ItemHit && (((e.Button & MouseButtons.Left) == MouseButtons.Left) || ((e.Button & MouseButtons.Right) == MouseButtons.Right)))
                nav.ClickedItem = mItems[h.ItemIndex];
            if (h.ItemHit)
                nav.HoveredItem = mItems[h.ItemIndex];
            if (h.ColumnHit)
                nav.HoveredColumn = h.ColumnIndex;
            if (h.ColumnSeparatorHit)
                nav.HoveredSeparator = h.ColumnSeparator;

            nav.MouseInColumnArea = h.InHeaderArea;
            nav.MouseInItemArea = h.InItemArea;

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Right) == MouseButtons.Right)
                nav.MouseClicked = true;

            mRenderer.SuspendPaint();

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left && AllowColumnResize && nav.HoveredSeparator != (ColumnType)(-1))
            {
                nav.DraggingSeperator = true;
                nav.SelSeperator = nav.HoveredSeparator;
                nav.SelStart = e.Location;
                mRenderer.Refresh();
            }
            else if ((e.Button & MouseButtons.Left) == MouseButtons.Left && AllowColumnClick && nav.HoveredColumn != (ColumnType)(-1))
            {
                if (SortColumn == nav.HoveredColumn)
                {
                    if (SortOrder == SortOrder.Descending)
                        SortOrder = SortOrder.Ascending;
                    else
                        SortOrder = SortOrder.Descending;
                }
                else
                {
                    SortColumn = nav.HoveredColumn;
                    SortOrder = SortOrder.Ascending;
                }
                mRenderer.Refresh();
            }
            else if (((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Right) == MouseButtons.Right) && nav.MouseInItemArea)
            {
                nav.SelStart = e.Location;
                nav.SelEnd = e.Location;
                mRenderer.Refresh();
            }

            mRenderer.ResumePaint();

            base.OnMouseDown(e);
        }
        /// <summary>
        /// Handles the MouseUp event.
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool suppressClick = nav.Dragging;
            nav.SelfDragging = false;

            scrollTimer.Enabled = false;
            mRenderer.SuspendPaint();

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left && nav.DraggingSeperator)
            {
                OnColumnWidthChanged(new ColumnEventArgs(Columns[nav.SelSeperator]));
                nav.DraggingSeperator = false;
            }
            else if (((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Right) == MouseButtons.Right) && nav.MouseClicked)
            {
                bool clear = true;
                if (nav.ControlDown) clear = false;
                if (nav.ShiftDown && nav.Dragging) clear = false;
                if (!nav.Dragging && ((e.Button & MouseButtons.Right) == MouseButtons.Right))
                {
                    if (nav.HoveredItem != null && nav.HoveredItem.Selected)
                        clear = false;
                }
                if (clear)
                    ClearSelection();

                if (nav.Dragging)
                {
                    if (nav.Highlight.Count != 0)
                    {
                        foreach (KeyValuePair<ImageListViewItem, bool> pair in nav.Highlight)
                            pair.Key.mSelected = pair.Value;
                        OnSelectionChanged(new EventArgs());
                        nav.Highlight.Clear();
                    }
                    nav.Dragging = false;
                }
                else if (nav.ControlDown && nav.HoveredItem != null)
                {
                    nav.HoveredItem.Selected = !nav.HoveredItem.Selected;
                }
                else if (nav.ShiftDown && nav.HoveredItem != null && Items.FocusedItem != null)
                {
                    int focusedIndex = mItems.IndexOf(mItems.FocusedItem);
                    int hoveredIndex = mItems.IndexOf(nav.HoveredItem);
                    int start = System.Math.Min(focusedIndex, hoveredIndex);
                    int end = System.Math.Max(focusedIndex, hoveredIndex);
                    for (int i = start; i <= end; i++)
                        Items[i].Selected = true;
                }
                else if (nav.HoveredItem != null)
                {
                    nav.HoveredItem.Selected = true;
                }

                // Move focus to the item under the cursor
                if (!(!nav.Dragging && nav.ShiftDown) && nav.HoveredItem != null)
                    Items.FocusedItem = nav.HoveredItem;

                nav.Dragging = false;
                nav.DraggingSeperator = false;

                mRenderer.Refresh();

                if (AllowColumnClick && nav.HoveredColumn != (ColumnType)(-1))
                {
                    OnColumnClick(new ColumnClickEventArgs(Columns[nav.HoveredColumn], e.Location, e.Button));
                }
            }

            if (!suppressClick && nav.HoveredItem != null)
                OnItemClick(new ItemClickEventArgs(nav.HoveredItem, e.Location, e.Button));

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Right) == MouseButtons.Right)
                nav.MouseClicked = false;

            mRenderer.ResumePaint();

            base.OnMouseUp(e);
        }
        /// <summary>
        /// Handles the MouseMove event.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            mRenderer.SuspendPaint();

            ImageListViewItem oldHoveredItem = nav.HoveredItem;
            ColumnType oldHoveredColumn = nav.HoveredColumn;
            ColumnType oldHoveredSeparator = nav.HoveredSeparator;
            ColumnType oldSelSeperator = nav.SelSeperator;
            ColumnType oldSelSep = nav.SelSeperator;
            nav.HoveredItem = null;
            nav.HoveredColumn = (ColumnType)(-1);
            nav.HoveredSeparator = (ColumnType)(-1);
            nav.SelSeperator = (ColumnType)(-1);

            HitInfo h;
            HitTest(e.Location, out h);

            if (h.ItemHit)
                nav.HoveredItem = mItems[h.ItemIndex];
            if (h.ColumnHit)
                nav.HoveredColumn = h.ColumnIndex;
            if (h.ColumnSeparatorHit)
                nav.HoveredSeparator = h.ColumnSeparator;

            nav.MouseInColumnArea = h.InHeaderArea;
            nav.MouseInItemArea = h.InItemArea;

            if (nav.DraggingSeperator)
            {
                nav.HoveredColumn = oldSelSep;
                nav.HoveredSeparator = oldSelSep;
                nav.SelSeperator = oldSelSep;
            }
            else if (nav.Dragging)
            {
                nav.HoveredColumn = (ColumnType)(-1);
                nav.HoveredSeparator = (ColumnType)(-1);
                nav.SelSeperator = (ColumnType)(-1);
            }

            if (nav.Dragging && ScrollOrientation == ScrollOrientation.VerticalScroll && e.Y > ClientRectangle.Bottom && !scrollTimer.Enabled)
            {
                scrollTimer.Tag = -120;
                scrollTimer.Enabled = true;
            }
            else if (nav.Dragging && ScrollOrientation == ScrollOrientation.VerticalScroll && e.Y < ClientRectangle.Top && !scrollTimer.Enabled)
            {
                scrollTimer.Tag = 120;
                scrollTimer.Enabled = true;
            }
            else if (nav.Dragging && ScrollOrientation == ScrollOrientation.HorizontalScroll && e.X > ClientRectangle.Right && !scrollTimer.Enabled)
            {
                scrollTimer.Tag = -120;
                scrollTimer.Enabled = true;
            }
            else if (nav.Dragging && ScrollOrientation == ScrollOrientation.HorizontalScroll && e.X < ClientRectangle.Left && !scrollTimer.Enabled)
            {
                scrollTimer.Tag = 120;
                scrollTimer.Enabled = true;
            }
            else if (scrollTimer.Enabled && ClientRectangle.Contains(e.Location))
            {
                scrollTimer.Enabled = false;
            }

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left && nav.DraggingSeperator)
            {
                int delta = e.Location.X - nav.SelStart.X;
                nav.SelStart = e.Location;
                int colwidth = Columns[nav.SelSeperator].Width + delta;
                colwidth = System.Math.Max(16, colwidth);
                Columns[nav.SelSeperator].Width = colwidth;
                mRenderer.Refresh();
            }
            else if (((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Right) == MouseButtons.Right) &&
                AllowDrag && !nav.SelfDragging &&
                nav.HoveredItem != null && nav.ClickedItem != null &&
                ReferenceEquals(nav.HoveredItem, nav.ClickedItem))
            {
                nav.Dragging = false;
                if (!nav.HoveredItem.Selected)
                    ClearSelection();
                if (mSelectedItems.Count == 0)
                {
                    nav.HoveredItem.Selected = true;
                    // Force a refresh
                    mRenderer.Refresh(true);
                }

                // Start drag-and-drop
                string[] filenames = new string[mSelectedItems.Count];
                for (int i = 0; i < mSelectedItems.Count; i++)
                    filenames[i] = mSelectedItems[i].FileName;
                DataObject data = new DataObject(DataFormats.FileDrop, filenames);
                nav.SelfDragging = true;
                nav.DragIndex = -1;
                DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Move);
                nav.SelfDragging = false;
            }
            else if (((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Right) == MouseButtons.Right) && nav.Dragging)
            {
                if (!nav.ShiftDown && !nav.ControlDown && SelectedItems.Count != 0)
                    ClearSelection();

                nav.SelEnd = e.Location;
                Rectangle sel = new Rectangle(System.Math.Min(nav.SelStart.X, nav.SelEnd.X), System.Math.Min(nav.SelStart.Y, nav.SelEnd.Y), System.Math.Abs(nav.SelStart.X - nav.SelEnd.X), System.Math.Abs(nav.SelStart.Y - nav.SelEnd.Y));
                nav.Highlight.Clear();
                Point pt1 = nav.SelStart;
                Point pt2 = nav.SelEnd;
                // Normalize to item area coordinates
                pt1.X -= layoutManager.ItemAreaBounds.Left;
                pt1.Y -= layoutManager.ItemAreaBounds.Top;
                pt2.X -= layoutManager.ItemAreaBounds.Left;
                pt2.Y -= layoutManager.ItemAreaBounds.Top;
                if ((ScrollOrientation == ScrollOrientation.HorizontalScroll && (pt1.Y > 0 || pt2.Y > 0)) ||
                    (ScrollOrientation == ScrollOrientation.VerticalScroll && (pt1.X > 0 || pt2.X > 0)))
                {
                    if (pt1.X < 0) pt1.X = 0;
                    if (pt1.Y < 0) pt1.Y = 0;
                    if (pt2.X < 0) pt2.X = 0;
                    if (pt2.Y < 0) pt2.Y = 0;
                    int startRow = (Math.Min(pt1.Y, pt2.Y) + ViewOffset.Y) / layoutManager.ItemSizeWithMargin.Height;
                    int endRow = (Math.Max(pt1.Y, pt2.Y) + ViewOffset.Y) / layoutManager.ItemSizeWithMargin.Height;
                    int startCol = (Math.Min(pt1.X, pt2.X) + ViewOffset.X) / layoutManager.ItemSizeWithMargin.Width;
                    int endCol = (Math.Max(pt1.X, pt2.X) + ViewOffset.X) / layoutManager.ItemSizeWithMargin.Width;
                    if (ScrollOrientation == ScrollOrientation.HorizontalScroll &&
                        (startRow <= layoutManager.Rows - 1 || endRow <= layoutManager.Rows - 1))
                    {
                        for (int row = startRow; row <= endRow; row++)
                        {
                            for (int col = startCol; col <= endCol; col++)
                            {
                                int i = row * layoutManager.Cols + col;
                                if (i >= 0 && i <= mItems.Count - 1 && !nav.Highlight.ContainsKey(mItems[i]))
                                    nav.Highlight.Add(mItems[i], (nav.ControlDown ? !Items[i].Selected : true));
                            }
                        }
                    }
                    else if (ScrollOrientation == ScrollOrientation.VerticalScroll &&
                        (startCol <= layoutManager.Cols - 1 || endCol <= layoutManager.Cols - 1))
                    {
                        startCol = Math.Min(layoutManager.Cols - 1, startCol);
                        endCol = Math.Min(layoutManager.Cols - 1, endCol);
                        for (int row = startRow; row <= endRow; row++)
                        {
                            for (int col = startCol; col <= endCol; col++)
                            {
                                int i = row * layoutManager.Cols + col;
                                if (i >= 0 && i <= mItems.Count - 1 && !nav.Highlight.ContainsKey(mItems[i]))
                                    nav.Highlight.Add(mItems[i], (nav.ControlDown ? !Items[i].Selected : true));
                            }
                        }
                    }
                }
                mRenderer.Refresh();
            }
            else if (nav.MouseClicked && ((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Right) == MouseButtons.Right) && nav.MouseInItemArea)
            {
                nav.SelEnd = e.Location;
                if (System.Math.Max(System.Math.Abs(nav.SelEnd.X - nav.SelStart.X), System.Math.Abs(nav.SelEnd.Y - nav.SelStart.Y)) > 2)
                    nav.Dragging = true;
            }

            if (Focused && AllowColumnResize && nav.HoveredSeparator != (ColumnType)(-1) && Cursor == Cursors.Default)
                Cursor = Cursors.VSplit;
            else if (Focused && nav.HoveredSeparator == (ColumnType)(-1) && Cursor != Cursors.Default)
                Cursor = Cursors.Default;

            if (oldHoveredItem != nav.HoveredItem ||
                oldHoveredColumn != nav.HoveredColumn ||
                oldHoveredSeparator != nav.HoveredSeparator ||
                oldSelSeperator != nav.SelSeperator)
                mRenderer.Refresh();

            mRenderer.ResumePaint();

            base.OnMouseMove(e);
        }
        /// <summary>
        /// Handles the MouseWheel event.
        /// </summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                int newYOffset = mViewOffset.Y - (e.Delta / 120) * vScrollBar.SmallChange;
                if (newYOffset > vScrollBar.Maximum - vScrollBar.LargeChange + 1)
                    newYOffset = vScrollBar.Maximum - vScrollBar.LargeChange + 1;
                if (newYOffset < 0)
                    newYOffset = 0;
                int delta = newYOffset - mViewOffset.Y;
                if (newYOffset < vScrollBar.Minimum) newYOffset = vScrollBar.Minimum;
                if (newYOffset > vScrollBar.Maximum) newYOffset = vScrollBar.Maximum;
                mViewOffset.Y = newYOffset;
                hScrollBar.Value = 0;
                vScrollBar.Value = newYOffset;
                if (nav.Dragging)
                    nav.SelStart = new Point(nav.SelStart.X, nav.SelStart.Y - delta);
            }
            else
            {
                int newXOffset = mViewOffset.X - (e.Delta / 120) * hScrollBar.SmallChange;
                if (newXOffset > hScrollBar.Maximum - hScrollBar.LargeChange + 1)
                    newXOffset = hScrollBar.Maximum - hScrollBar.LargeChange + 1;
                if (newXOffset < 0)
                    newXOffset = 0;
                int delta = newXOffset - mViewOffset.X;
                if (newXOffset < hScrollBar.Minimum) newXOffset = hScrollBar.Minimum;
                if (newXOffset > hScrollBar.Maximum) newXOffset = hScrollBar.Maximum;
                mViewOffset.X = newXOffset;
                vScrollBar.Value = 0;
                hScrollBar.Value = newXOffset;
                if (nav.Dragging)
                    nav.SelStart = new Point(nav.SelStart.X - delta, nav.SelStart.Y);
            }

            mRenderer.Refresh();

            base.OnMouseWheel(e);
        }
        /// <summary>
        /// Handles the MouseLeave event.
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            nav.MouseInItemArea = false;
            nav.MouseInColumnArea = false;

            mRenderer.SuspendPaint();
            if (nav.HoveredItem != null)
            {
                nav.HoveredItem = null;
                mRenderer.Refresh();
            }
            if (nav.HoveredColumn != (ColumnType)(-1))
            {
                nav.HoveredColumn = (ColumnType)(-1);
                mRenderer.Refresh();
            }
            if (nav.HoveredSeparator != (ColumnType)(-1))
                Cursor = Cursors.Default;

            mRenderer.ResumePaint();

            base.OnMouseLeave(e);
        }
        /// <summary>
        /// Handles the MouseDoubleClick event.
        /// </summary>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            mRenderer.SuspendPaint();
            if (nav.HoveredItem != null)
            {
                OnItemDoubleClick(new ItemClickEventArgs(nav.HoveredItem, e.Location, e.Button));
            }
            if (AllowColumnClick && nav.HoveredSeparator != (ColumnType)(-1))
            {
                Columns[nav.HoveredSeparator].AutoFit();
                mRenderer.Refresh();
            }
            mRenderer.ResumePaint();

            base.OnMouseDoubleClick(e);
        }
        /// <summary>
        /// Handles the IsInputKey event.
        /// </summary>
        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.ShiftKey) == Keys.ShiftKey || (keyData & Keys.ControlKey) == Keys.ControlKey)
            {
                ImageListViewItem item = this.Items.FocusedItem;
                int index = 0;
                if (item != null)
                    index = mItems.IndexOf(item);
                nav.SelStartKey = index;
            }

            if ((keyData & Keys.ShiftKey) == Keys.ShiftKey ||
                (keyData & Keys.ControlKey) == Keys.ControlKey ||
                (keyData & Keys.Left) == Keys.Left ||
                (keyData & Keys.Right) == Keys.Right ||
                (keyData & Keys.Up) == Keys.Up ||
                (keyData & Keys.Down) == Keys.Down)
                return true;
            else
                return base.IsInputKey(keyData);
        }
        /// <summary>
        /// Handles the KeyDown event.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            nav.ShiftDown = e.Shift;
            nav.ControlDown = e.Control;

            if (Items.Count == 0)
                return;

            ImageListViewItem item = this.Items.FocusedItem;
            int index = 0;
            if (item != null)
                index = mItems.IndexOf(item);

            int newindex = ApplyNavKey(index, e.KeyCode);
            if (index == newindex)
                return;

            mRenderer.SuspendPaint();
            index = newindex;
            if (nav.ControlDown)
            {
                nav.SelStartKey = index;
                Items.FocusedItem = Items[index];
                EnsureVisible(index);
            }
            else if (nav.ShiftDown)
            {
                ClearSelection();
                nav.SelEndKey = index;
                Items.FocusedItem = Items[index];
                int imin = System.Math.Min(nav.SelStartKey, nav.SelEndKey);
                int imax = System.Math.Max(nav.SelStartKey, nav.SelEndKey);
                for (int i = imin; i <= imax; i++)
                {
                    Items[i].Selected = true;
                }
                EnsureVisible(nav.SelEndKey);
            }
            else
            {
                ClearSelection();
                nav.SelStartKey = index;
                Items[index].Selected = true;
                Items.FocusedItem = Items[index];
                EnsureVisible(index);
            }
            mRenderer.ResumePaint();
        }
        /// <summary>
        /// Handles the KeyUp event.
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            nav.ShiftDown = e.Shift;
            nav.ControlDown = e.Control;
        }
        /// <summary>
        /// Handles the GotFocus event.
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            mRenderer.Refresh();
        }
        /// <summary>
        /// Handles the LostFocus event.
        /// </summary>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            mRenderer.Refresh();
        }
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.Control"/> and its child controls and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;

            disposed = true;
            if (disposing)
            {
                if (mRenderer != null)
                    mRenderer.Dispose();

                if (mHeaderFont != null)
                    mHeaderFont.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Virtual Functions
        /// <summary>
        /// Raises the ColumnWidthChanged event.
        /// </summary>
        /// <param name="e">A ColumnEventArgs that contains event data.</param>
        protected virtual void OnColumnWidthChanged(ColumnEventArgs e)
        {
            if (ColumnWidthChanged != null)
                ColumnWidthChanged(this, e);
        }
        /// <summary>
        /// Raises the ColumnClick event.
        /// </summary>
        /// <param name="e">A ColumnClickEventArgs that contains event data.</param>
        protected virtual void OnColumnClick(ColumnClickEventArgs e)
        {
            if (ColumnClick != null)
                ColumnClick(this, e);
        }
        /// <summary>
        /// Raises the ItemClick event.
        /// </summary>
        /// <param name="e">A ItemClickEventArgs that contains event data.</param>
        protected virtual void OnItemClick(ItemClickEventArgs e)
        {
            if (ItemClick != null)
                ItemClick(this, e);
        }
        /// <summary>
        /// Raises the ItemDoubleClick event.
        /// </summary>
        /// <param name="e">A ItemClickEventArgs that contains event data.</param>
        protected virtual void OnItemDoubleClick(ItemClickEventArgs e)
        {
            if (ItemDoubleClick != null)
                ItemDoubleClick(this, e);
        }
        /// <summary>
        /// Raises the SelectionChanged event.
        /// </summary>
        /// <param name="e">A EventArgs that contains event data.</param>
        protected virtual void OnSelectionChanged(EventArgs e)
        {
            if (SelectionChanged != null)
                SelectionChanged(this, e);
        }
        /// <summary>
        /// Raises the SelectionChanged event.
        /// </summary>
        /// <param name="e">A EventArgs that contains event data.</param>
        internal void OnSelectionChangedInternal()
        {
            OnSelectionChanged(new EventArgs());
        }
        /// <summary>
        /// Raises the ThumbnailCached event.
        /// </summary>
        /// <param name="e">A ItemEventArgs that contains event data.</param>
        protected virtual void OnThumbnailCached(ItemEventArgs e)
        {
            if (ThumbnailCached != null)
                ThumbnailCached(this, e);
        }
        /// <summary>
        /// Raises the ThumbnailCached event.
        /// This method is invoked from the thumbnail thread.
        /// </summary>
        /// <param name="e">The guid of the item whose thumbnail is cached.</param>
        internal void OnThumbnailCachedInternal(Guid guid)
        {
            int itemIndex = Items.IndexOf(guid);
            if (itemIndex != -1)
                OnThumbnailCached(new ItemEventArgs(Items[itemIndex]));
        }
        /// <summary>
        /// Raises the refresh event.
        /// This method is invoked from the thumbnail thread.
        /// </summary>
        internal void OnRefreshInternal()
        {
            mRenderer.Refresh();
        }
        /// <summary>
        /// Updates item details.
        /// This method is invoked from the item cache thread.
        /// </summary>
        internal void UpdateItemDetailsInternal(ImageListViewItem item,Utility.ShellImageFileInfo info)
        {
            item.UpdateDetailsInternal(info);
        }
        /// <summary>
        /// Raises the ThumbnailCaching event.
        /// </summary>
        /// <param name="e">A ItemEventArgs that contains event data.</param>
        protected virtual void OnThumbnailCaching(ItemEventArgs e)
        {
            if (ThumbnailCaching != null)
                ThumbnailCaching(this, e);
        }
        #endregion

        #region Exposed Events
        /// <summary>
        /// Occurs after the user successfully resized a column header.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs after the user successfully resized a column header.")]
        public event ColumnWidthChangedEventHandler ColumnWidthChanged;
        /// <summary>
        /// Occurs when the user clicks a column header.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user clicks a column header.")]
        public event ColumnClickEventHandler ColumnClick;
        /// <summary>
        /// Occurs when the user clicks an item.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user clicks an item.")]
        public event ItemClickEventHandler ItemClick;
        /// <summary>
        /// Occurs when the user double-clicks an item.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user double-clicks an item.")]
        public event ItemDoubleClickEventHandler ItemDoubleClick;
        /// <summary>
        /// Occurs when the selected items collection changes.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when the selected items collection changes.")]
        public event EventHandler SelectionChanged;
        /// <summary>
        /// Occurs after an item thumbnail is cached.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs after an item thumbnail is cached.")]
        public event ThumbnailCachedEventHandler ThumbnailCached;
        /// <summary>
        /// Occurs before an item thumbnail is cached.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs before an item thumbnail is cached.")]
        public event ThumbnailCachingEventHandler ThumbnailCaching;
        #endregion
    }
}
