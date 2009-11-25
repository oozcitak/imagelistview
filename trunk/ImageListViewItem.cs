using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents an item in the image list view.
    /// </summary>
    public class ImageListViewItem
    {
        #region Member Variables
        internal int mIndex;
        private Color mBackColor;
        private Color mForeColor;
        private Guid mGuid;
        internal ImageListView mImageListView;
        protected internal bool mSelected;
        private string mText;
        private int mZOrder;
        internal string defaultText;
        // File info
        internal DateTime mDateAccessed;
        internal DateTime mDateCreated;
        internal DateTime mDateModified;
        internal string mFileType;
        private string mFileName;
        internal string mFilePath;
        internal long mFileSize;
        internal Size mDimension;
        internal SizeF mResolution;

        internal ImageListView.ImageListViewItemCollection owner;
        internal bool isDirty;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the background color of the item.
        /// </summary>
        [Category("Appearance"), Browsable(true), Description("Gets or sets the background color of the item."), DefaultValue(typeof(Color), "Transparent")]
        public Color BackColor
        {
            get
            {
                return mBackColor;
            }
            set
            {
                if (value != mBackColor)
                {
                    mBackColor = value;
                    if (mImageListView != null)
                        mImageListView.Refresh();
                }
            }
        }
        /// <summary>
        /// Gets the cache state of the item thumbnail.
        /// </summary>
        [Category("Behavior"), Browsable(false), Description("Gets the cache state of the item thumbnail.")]
        public CacheState ThumbnailCacheState { get { return mImageListView.cacheManager.GetCacheState(Guid); } }
        /// <summary>
        /// Gets a value determining if the item is focused.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets a value determining if the item is focused.")]
        public bool Focused
        {
            get
            {
                if (owner == null || owner.FocusedItem == null) return false;
                return (this == owner.FocusedItem);
            }
            set
            {
                if (owner != null)
                    owner.FocusedItem = this;
            }
        }
        /// <summary>
        /// Gets or sets the foreground color of the item.
        /// </summary>
        [Category("Appearance"), Browsable(true), Description("Gets or sets the foreground color of the item."), DefaultValue(typeof(Color), "WindowText")]
        public Color ForeColor
        {
            get
            {
                return mForeColor;
            }
            set
            {
                if (value != mForeColor)
                {
                    mForeColor = value;
                    if (mImageListView != null)
                        mImageListView.Refresh();
                }
            }
        }
        /// <summary>
        /// Gets the unique identifier for this item.
        /// </summary>
        [Category("Behavior"), Browsable(false), Description("Gets the unique identifier for this item.")]
        internal Guid Guid { get { return mGuid; } private set { mGuid = value; } }
        /// <summary>
        /// Determines whether the mouse is currently hovered over the item.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Determines whether the mouse is currently hovered over the item.")]
        public bool Hovered { get { return (mImageListView.nav.HoveredItem == this); } }
        /// <summary>
        /// Gets the ImageListView owning this item.
        /// </summary>
        [Category("Behavior"), Browsable(false), Description("Gets the ImageListView owning this item.")]
        public ImageListView ImageListView { get { return mImageListView; } private set { mImageListView = value; } }
        /// <summary>
        /// Gets the index of the item.
        /// </summary>
        [Category("Behavior"), Browsable(false), Description("Gets the index of the item."), EditorBrowsable(EditorBrowsableState.Advanced)]
        public int Index { get { return mIndex; } }
        /// <summary>
        /// Gets or sets a value determining if the item is selected.
        /// </summary>
        [Category("Appearance"), Browsable(true), Description("Gets or sets a value determining if the item is selected."), DefaultValue(false)]
        public bool Selected
        {
            get
            {
                return mSelected;
            }
            set
            {
                if (value != mSelected)
                {
                    mSelected = value;
                    if (mImageListView != null)
                        mImageListView.OnSelectionChangedInternal();
                }
            }
        }
        /// <summary>
        /// Gets or sets the user-defined data associated with the item.
        /// </summary>
        [Category("Data"), Browsable(true), Description("Gets or sets the user-defined data associated with the item.")]
        public object Tag { get; set; }
        /// <summary>
        /// Gets or sets the text associated with this item. If left blank, item Text 
        /// reverts to the name of the image file.
        /// </summary>
        [Category("Appearance"), Browsable(true), Description("Gets or sets the text associated with this item. If left blank, item Text reverts to the name of the image file.")]
        public string Text
        {
            get
            {
                if (string.IsNullOrEmpty(mText))
                {
                    UpdateFileInfo();
                    return defaultText;
                }
                else
                    return mText;
            }
            set
            {
                mText = value;
                if (mImageListView != null)
                    mImageListView.Refresh();
            }
        }
        /// <summary>
        /// Gets the thumbnail image. If the thumbnail image is not cached, it will be 
        /// added to the cache queue and DefaultImage of the owner image list view will
        /// be returned. If the thumbnail could not be cached ErrorImage of the owner
        /// image list view will be returned.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets the thumbnail image.")]
        public Image ThumbnailImage
        {
            get
            {
                if (mImageListView == null)
                    throw new InvalidOperationException("Owner control is null.");

                CacheState state = ThumbnailCacheState;
                if (state == CacheState.Error)
                    return mImageListView.ErrorImage;
                else if (state == CacheState.InQueue)
                    return mImageListView.DefaultImage;
                else if (state == CacheState.Cached)
                {
                    Image img = mImageListView.cacheManager.GetImage(Guid);
                    if (img != null)
                        return img;
                    else
                    {
                        mImageListView.cacheManager.Add(Guid, FileName);
                        return mImageListView.DefaultImage;
                    }
                }
                else
                {
                    mImageListView.cacheManager.Add(Guid, FileName);
                    return mImageListView.DefaultImage;
                }
            }
        }
        /// <summary>
        /// Gets or sets the draw order of the item.
        /// </summary>
        [Category("Appearance"), Browsable(true), Description("Gets or sets the draw order of the item."), DefaultValue(0)]
        public int ZOrder { get { return mZOrder; } set { mZOrder = value; } }
        /// <summary>
        /// Gets the last access date of the image file represented by this item.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets the last access date of the image file represented by this item.")]
        public DateTime DateAccessed { get { UpdateFileInfo(); return mDateAccessed; } }
        /// <summary>
        /// Gets the creation date of the image file represented by this item.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets the creation date of the image file represented by this item.")]
        public DateTime DateCreated { get { UpdateFileInfo(); return mDateCreated; } }
        /// <summary>
        /// Gets the modification date of the image file represented by this item.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets the modification date of the image file represented by this item.")]
        public DateTime DateModified { get { UpdateFileInfo(); return mDateModified; } }
        /// <summary>
        /// Gets the shell type of the image file represented by this item.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets the shell type of the image file represented by this item.")]
        public string FileType { get { UpdateFileInfo(); return mFileType; } }
        /// <summary>
        /// Gets or sets the name of the image fie represented by this item.
        /// </summary>        
        [Category("Data"), Browsable(false), Description("Gets or sets the name of the image fie represented by this item.")]
        public string FileName
        {
            get
            {
                return mFileName;
            }
            set
            {
                if (mFileName != value)
                {
                    mFileName = value;
                    isDirty = true;
                    if (mImageListView != null)
                    {
                        mImageListView.cacheManager.Remove(Guid);
                        mImageListView.itemCacheManager.Add(this);
                        mImageListView.Refresh();
                    }
                }
            }
        }
        /// <summary>
        /// Gets the path of the image fie represented by this item.
        /// </summary>        
        [Category("Data"), Browsable(false), Description("Gets the path of the image fie represented by this item.")]
        public string FilePath { get { UpdateFileInfo(); return mFilePath; } }
        /// <summary>
        /// Gets file size in bytes.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets file size in bytes.")]
        public long FileSize { get { UpdateFileInfo(); return mFileSize; } }
        /// <summary>
        /// Gets image dimensions.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets image dimensions.")]
        public Size Dimension { get { UpdateFileInfo(); return mDimension; } }
        /// <summary>
        /// Gets image resolution in pixels per inch.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets image resolution in pixels per inch.")]
        public SizeF Resolution { get { UpdateFileInfo(); return mResolution; } }
        #endregion

        #region Constructors
        public ImageListViewItem()
        {
            mIndex = -1;
            owner = null;

            mBackColor = Color.Transparent;
            mForeColor = SystemColors.WindowText;
            mZOrder = 0;

            Guid = Guid.NewGuid();
            ImageListView = null;
            Selected = false;

            isDirty = true;
            defaultText = null;
        }
        public ImageListViewItem(string filename)
            : this()
        {
            mFileName = filename;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Gets the item image.
        /// </summary>
        /// <returns>The item image.</returns>
        public Image GetImage()
        {
            return Image.FromFile(mFileName);
        }
        /// <summary>
        /// Returns the sub item item text corresponding to the specified column type.
        /// </summary>
        /// <param name="type">The type of information to return.</param>
        /// <returns>Formatted text for the given column type.</returns>
        public string GetSubItemText(ColumnType type)
        {
            switch (type)
            {
                case ColumnType.DateAccessed:
                    return DateAccessed.ToString("g");
                case ColumnType.DateCreated:
                    return DateCreated.ToString("g");
                case ColumnType.DateModified:
                    return DateModified.ToString("g");
                case ColumnType.FileName:
                    return FileName;
                case ColumnType.Name:
                    return Text;
                case ColumnType.FilePath:
                    return FilePath;
                case ColumnType.FileSize:
                    return Utility.FormatSize(FileSize);
                case ColumnType.FileType:
                    return FileType;
                case ColumnType.Dimension:
                    return string.Format("{0} x {1}", Dimension.Width, Dimension.Height);
                case ColumnType.Resolution:
                    return string.Format("{0} x {1}", Resolution.Width, Resolution.Height);
                default:
                    throw new ArgumentException("Unknown column type", "type");
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Updates file info for the image file represented by this item.
        /// </summary>
        private void UpdateFileInfo()
        {
            if (!isDirty) return;
            isDirty = false;

            Utility.ShellFileInfo info = new Utility.ShellFileInfo(mFileName);
            if (info.Error) return;

            mDateAccessed = info.LastAccessTime;
            mDateCreated = info.CreationTime;
            mDateModified = info.LastWriteTime;
            mFileSize = info.Size;
            mFileType = info.TypeName;
            mFilePath = Path.GetDirectoryName(FileName);
            defaultText = Path.GetFileName(FileName);

            using (FileStream stream = new FileStream(mFileName, FileMode.Open, FileAccess.Read))
            {
                using (Image img = Image.FromStream(stream, false, false))
                {
                    mDimension = img.Size;
                    mResolution = new SizeF(img.HorizontalResolution, img.VerticalResolution);
                }
            }
        }
        /// <summary>
        /// Invoked by the worker thread to update item details.
        /// </summary>
        internal void UpdateDetailsInternal(DateTime dateAccessed, DateTime dateCreated, DateTime dateModified,
            long fileSize, string fileType, string filePath, string name, Size dimension, SizeF resolution)
        {
            if (!isDirty) return;
            isDirty = false;
            mDateAccessed = dateAccessed;
            mDateCreated = dateCreated;
            mDateModified = dateModified;
            mFileSize = fileSize;
            mFileType = fileType;
            mFilePath = filePath;
            defaultText = name;
            mDimension = dimension;
            mResolution = resolution;
        }
        #endregion
    }
}
