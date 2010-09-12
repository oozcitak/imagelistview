// ImageListView - A listview control for image files
// Copyright (C) 2009 Ozgur Ozcitak
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Ozgur Ozcitak (ozcitak@yahoo.com)

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Collections.Generic;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents an item in the image list view.
    /// </summary>
    public class ImageListViewItem : ICloneable
    {
        #region Member Variables
        // Property backing fields
        internal int mIndex;
        private Guid mGuid;
        internal ImageListView mImageListView;
        internal bool mChecked;
        internal bool mSelected;
        private string mText;
        private int mZOrder;
        // File info
        private DateTime mDateAccessed;
        private DateTime mDateCreated;
        private DateTime mDateModified;
        private string mFileType;
        private string mFileName;
        private string mFilePath;
        private long mFileSize;
        private Size mDimensions;
        private SizeF mResolution;
        // Exif tags
        private string mImageDescription;
        private string mEquipmentModel;
        private DateTime mDateTaken;
        private string mArtist;
        private string mCopyright;
        private string mExposureTime;
        private float mFNumber;
        private ushort mISOSpeed;
        private string mShutterSpeed;
        private string mAperture;
        private string mUserComment;
        private ushort mRating;
        // Used for virtual items
        internal bool isVirtualItem;
        internal object mVirtualItemKey;
        // Used for custom columns
        private Dictionary<Guid, string> subItems;

        internal ImageListView.ImageListViewItemCollection owner;
        internal bool isDirty;
        private bool editing;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the cache state of the item thumbnail.
        /// </summary>
        [Category("Behavior"), Browsable(false), Description("Gets the cache state of the item thumbnail.")]
        public CacheState ThumbnailCacheState { get { return mImageListView.cacheManager.GetCacheState(mGuid); } }
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
        /// Gets the unique identifier for this item.
        /// </summary>
        [Category("Behavior"), Browsable(false), Description("Gets the unique identifier for this item.")]
        internal Guid Guid { get { return mGuid; } private set { mGuid = value; } }
        /// <summary>
        /// Gets the virtual item key associated with this item.
        /// Returns null if the item is not a virtual item.
        /// </summary>
        [Category("Behavior"), Browsable(false), Description("Gets the virtual item key associated with this item.")]
        public object VirtualItemKey { get { return mVirtualItemKey; } }
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
        /// Gets or sets a value determining if the item is checked.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets or sets a value determining if the item is checked."), DefaultValue(false)]
        public bool Checked
        {
            get
            {
                return mChecked;
            }
            set
            {
                if (value != mChecked)
                {
                    mChecked = value;
                    if (mImageListView != null)
                        mImageListView.OnItemCheckBoxClickInternal(this);
                }
            }
        }
        /// <summary>
        /// Gets or sets a value determining if the item is selected.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets or sets a value determining if the item is selected."), DefaultValue(false)]
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
                    {
                        mImageListView.OnSelectionChangedInternal();
                        if (mImageListView.IsItemVisible(mGuid))
                            mImageListView.Refresh();
                    }
                }
            }
        }
        /// <summary>
        /// Gets or sets the user-defined data associated with the item.
        /// </summary>
        [Category("Data"), Browsable(false), Description("Gets or sets the user-defined data associated with the item."), TypeConverter(typeof(StringConverter))]
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
                return mText;
            }
            set
            {
                mText = value;
                if (mImageListView != null && mImageListView.IsItemVisible(mGuid))
                    mImageListView.Refresh();
            }
        }
        /// <summary>
        /// Gets or sets the name of the image file represented by this item.
        /// </summary>        
        [Category("File Properties"), Browsable(true), Description("Gets or sets the name of the image file represented by this item.")]
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
                    if (!isVirtualItem)
                    {
                        isDirty = true;
                        if (mImageListView != null)
                        {
                            mImageListView.cacheManager.Remove(mGuid, true);
                            mImageListView.itemCacheManager.Remove(mGuid);
                            mImageListView.itemCacheManager.Add(mGuid, mFileName);
                            if (mImageListView.IsItemVisible(mGuid))
                                mImageListView.Refresh();
                        }
                    }
                }
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

                Image img = null;
                CacheState state = ThumbnailCacheState;

                if (state == CacheState.Error)
                {
                    if (mImageListView.ShellIconFallback && mImageListView.ThumbnailSize.Width > 32 && mImageListView.ThumbnailSize.Height > 32)
                        img = LargeIcon;
                    if (img == null && mImageListView.ShellIconFallback)
                        img = SmallIcon;
                    if (img == null)
                        img = mImageListView.ErrorImage;
                    return img;
                }

                img = mImageListView.cacheManager.GetImage(Guid);

                if (state == CacheState.Cached)
                    return img;

                if (isVirtualItem)
                    mImageListView.cacheManager.Add(Guid, mVirtualItemKey, mImageListView.ThumbnailSize, mImageListView.UseEmbeddedThumbnails);
                else
                    mImageListView.cacheManager.Add(Guid, FileName, mImageListView.ThumbnailSize, mImageListView.UseEmbeddedThumbnails);

                if (img == null && mImageListView.ShellIconFallback && mImageListView.ThumbnailSize.Width > 16 && mImageListView.ThumbnailSize.Height > 16)
                    img = LargeIcon;
                if (img == null && mImageListView.ShellIconFallback)
                    img = SmallIcon;
                if (img == null)
                    img = mImageListView.DefaultImage;
                return img;
            }
        }
        /// <summary>
        /// Gets or sets the draw order of the item.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets or sets the draw order of the item."), DefaultValue(0)]
        public int ZOrder { get { return mZOrder; } set { mZOrder = value; } }
        #endregion

        #region Shell Properties
        /// <summary>
        /// Gets the small shell icon of the image file represented by this item.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets the small shell icon of the image file represented by this item.")]
        public Image SmallIcon { get { return mImageListView.itemCacheManager.GetSmallIcon(mGuid); } }
        /// <summary>
        /// Gets the large shell icon of the image file represented by this item.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets the large shell icon of the image file represented by this item.")]
        public Image LargeIcon { get { return mImageListView.itemCacheManager.GetLargeIcon(mGuid); } }
        /// <summary>
        /// Gets the last access date of the image file represented by this item.
        /// </summary>
        [Category("File Properties"), Browsable(true), Description("Gets the last access date of the image file represented by this item.")]
        public DateTime DateAccessed { get { UpdateFileInfo(); return mDateAccessed; } }
        /// <summary>
        /// Gets the creation date of the image file represented by this item.
        /// </summary>
        [Category("File Properties"), Browsable(true), Description("Gets the creation date of the image file represented by this item.")]
        public DateTime DateCreated { get { UpdateFileInfo(); return mDateCreated; } }
        /// <summary>
        /// Gets the modification date of the image file represented by this item.
        /// </summary>
        [Category("File Properties"), Browsable(true), Description("Gets the modification date of the image file represented by this item.")]
        public DateTime DateModified { get { UpdateFileInfo(); return mDateModified; } }
        /// <summary>
        /// Gets the shell type of the image file represented by this item.
        /// </summary>
        [Category("File Properties"), Browsable(true), Description("Gets the shell type of the image file represented by this item.")]
        public string FileType { get { UpdateFileInfo(); return mFileType; } }
        /// <summary>
        /// Gets the path of the image file represented by this item.
        /// </summary>        
        [Category("File Properties"), Browsable(true), Description("Gets the path of the image file represented by this item.")]
        public string FilePath { get { UpdateFileInfo(); return mFilePath; } }
        /// <summary>
        /// Gets file size in bytes.
        /// </summary>
        [Category("File Properties"), Browsable(true), Description("Gets file size in bytes.")]
        public long FileSize { get { UpdateFileInfo(); return mFileSize; } }
        #endregion

        #region Exif Properties
        /// <summary>
        /// Gets image dimensions.
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets image dimensions.")]
        public Size Dimensions { get { UpdateFileInfo(); return mDimensions; } }
        /// <summary>
        /// Gets image resolution in pixels per inch.
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets image resolution in pixels per inch.")]
        public SizeF Resolution { get { UpdateFileInfo(); return mResolution; } }
        /// <summary>
        /// Gets image description.
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets image description.")]
        public string ImageDescription { get { UpdateFileInfo(); return mImageDescription; } }
        /// <summary>
        /// Gets the camera model.
        /// </summary>
        [Category("Camera Properties"), Browsable(true), Description("Gets the camera model.")]
        public string EquipmentModel { get { UpdateFileInfo(); return mEquipmentModel; } }
        /// <summary>
        /// Gets the date and time the image was taken.
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets the date and time the image was taken.")]
        public DateTime DateTaken { get { UpdateFileInfo(); return mDateTaken; } }
        /// <summary>
        /// Gets the name of the artist.
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets the name of the artist.")]
        public string Artist { get { UpdateFileInfo(); return mArtist; } }
        /// <summary>
        /// Gets image copyright information.
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets image copyright information.")]
        public string Copyright { get { UpdateFileInfo(); return mCopyright; } }
        /// <summary>
        /// Gets the exposure time in seconds.
        /// </summary>
        [Category("Camera Properties"), Browsable(true), Description("Gets the exposure time in seconds.")]
        public string ExposureTime { get { UpdateFileInfo(); return mExposureTime; } }
        /// <summary>
        /// Gets the F number.
        /// </summary>
        [Category("Camera Properties"), Browsable(true), Description("Gets the F number.")]
        public float FNumber { get { UpdateFileInfo(); return mFNumber; } }
        /// <summary>
        /// Gets the ISO speed.
        /// </summary>
        [Category("Camera Properties"), Browsable(true), Description("Gets the ISO speed.")]
        public ushort ISOSpeed { get { UpdateFileInfo(); return mISOSpeed; } }
        /// <summary>
        /// Gets the shutter speed.
        /// </summary>
        [Category("Camera Properties"), Browsable(true), Description("Gets the shutter speed.")]
        public string ShutterSpeed { get { UpdateFileInfo(); return mShutterSpeed; } }
        /// <summary>
        /// Gets the lens aperture value.
        /// </summary>
        [Category("Camera Properties"), Browsable(true), Description("Gets the lens aperture value.")]
        public string Aperture { get { UpdateFileInfo(); return mAperture; } }
        /// <summary>
        /// Gets user comments.
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets user comments.")]
        public string UserComment { get { UpdateFileInfo(); return mUserComment; } }
        /// <summary>
        /// Gets rating in percent (Windows specific).
        /// </summary>
        [Category("Image Properties"), Browsable(true), Description("Gets rating in percent.")]
        public ushort Rating { get { UpdateFileInfo(); return mRating; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ImageListViewItem class.
        /// </summary>
        public ImageListViewItem()
        {
            mIndex = -1;
            owner = null;

            mZOrder = 0;

            Guid = Guid.NewGuid();
            ImageListView = null;
            Checked = false;
            Selected = false;

            isDirty = true;
            editing = false;

            mVirtualItemKey = null;
            isVirtualItem = false;

            subItems = new Dictionary<Guid, string>();
        }
        /// <summary>
        /// Initializes a new instance of the ImageListViewItem class.
        /// </summary>
        /// <param name="filename">The image filename representing the item.</param>
        public ImageListViewItem(string filename)
            : this()
        {
            mFileName = filename;
            mText = Path.GetFileName(filename);
        }
        /// <summary>
        /// Initializes a new instance of a virtual ImageListViewItem class.
        /// </summary>
        /// <param name="key">The key identifying this item.</param>
        /// <param name="text">Text of this item.</param>
        /// <param name="dimensions">Pixel dimensions of the source image.</param>
        public ImageListViewItem(object key, string text, Size dimensions)
            : this()
        {
            isVirtualItem = true;
            mVirtualItemKey = key;
            mText = text;
            mDimensions = dimensions;
        }
        /// <summary>
        /// Initializes a new instance of a virtual ImageListViewItem class.
        /// </summary>
        /// <param name="key">The key identifying this item.</param>
        /// <param name="text">Text of this item.</param>
        public ImageListViewItem(object key, string text)
            : this(key, text, Size.Empty)
        {
            ;
        }
        /// <summary>
        /// Initializes a new instance of a virtual ImageListViewItem class.
        /// </summary>
        /// <param name="key">The key identifying this item.</param>
        public ImageListViewItem(object key)
            : this(key, string.Empty, Size.Empty)
        {
            ;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Begins editing the item.
        /// This method must be used while editing the item
        /// to prevent collisions with the cache manager.
        /// </summary>
        public void BeginEdit()
        {
            if (editing == true)
                throw new InvalidOperationException("Already editing this item.");

            if (mImageListView == null)
                throw new InvalidOperationException("Owner control is null.");

            mImageListView.cacheManager.BeginItemEdit(mGuid);
            mImageListView.itemCacheManager.BeginItemEdit(mGuid);

            editing = true;
        }
        /// <summary>
        /// Ends editing and updates the item.
        /// </summary>
        /// <param name="update">If set to true, the item will be immediately updated.</param>
        public void EndEdit(bool update)
        {
            if (editing == false)
                throw new InvalidOperationException("This item is not being edited.");

            if (mImageListView == null)
                throw new InvalidOperationException("Owner control is null.");

            mImageListView.cacheManager.EndItemEdit(mGuid);
            mImageListView.itemCacheManager.EndItemEdit(mGuid);

            editing = false;
            if (update) Update();
        }
        /// <summary>
        /// Ends editing and updates the item.
        /// </summary>
        public void EndEdit()
        {
            EndEdit(true);
        }
        /// <summary>
        /// Updates item thumbnail and item details.
        /// </summary>
        public void Update()
        {
            isDirty = true;
            if (mImageListView != null)
            {
                mImageListView.cacheManager.Remove(mGuid, true);
                mImageListView.itemCacheManager.Remove(mGuid);
                if (isVirtualItem)
                    mImageListView.itemCacheManager.Add(mGuid, mVirtualItemKey);
                else
                    mImageListView.itemCacheManager.Add(mGuid, mFileName);
                mImageListView.Refresh();
            }
        }
        /// <summary>
        /// Returns the sub item item text corresponding to the custom column with the given index.
        /// </summary>
        /// <param name="index">Index of the custom column.</param>
        /// <returns>Sub item text text for the given custom column type.</returns>
        public string GetSubItemText(int index)
        {
            int i = 0;
            foreach (string val in subItems.Values)
            {
                if (i == index)
                    return val;
                i++;
            }

            throw new IndexOutOfRangeException();
        }
        /// <summary>
        /// Sets the sub item item text corresponding to the custom column with the given index.
        /// </summary>
        /// <param name="index">Index of the custom column.</param>
        /// <param name="text">New sub item text</param>
        public void SetSubItemText(int index, string text)
        {
            int i = 0;
            Guid found = Guid.Empty;
            foreach (Guid guid in subItems.Keys)
            {
                if (i == index)
                {
                    found = guid;
                    break;
                }

                i++;
            }

            if (found != Guid.Empty)
            {
                subItems[found] = text;
                if (mImageListView != null && mImageListView.IsItemVisible(mGuid))
                    mImageListView.Refresh();
            }
            else
                throw new IndexOutOfRangeException();
        }
        /// <summary>
        /// Returns the sub item item text corresponding to the specified column type.
        /// </summary>
        /// <param name="type">The type of information to return.</param>
        /// <returns>Formatted text for the given column type.</returns>
        internal string GetSubItemText(ColumnType type)
        {
            if (type == ColumnType.Name)
                return Text;
            else if (type == ColumnType.FileName)
                return FileName;
            else if (isDirty)
                return "";

            switch (type)
            {
                case ColumnType.Custom:
                    throw new ArgumentException("Column type is ambiguous. You must access custom columns by index.", "type");
                case ColumnType.DateAccessed:
                    if (DateAccessed == DateTime.MinValue)
                        return "";
                    else
                        return DateAccessed.ToString("g");
                case ColumnType.DateCreated:
                    if (DateCreated == DateTime.MinValue)
                        return "";
                    else
                        return DateCreated.ToString("g");
                case ColumnType.DateModified:
                    if (DateModified == DateTime.MinValue)
                        return "";
                    else
                        return DateModified.ToString("g");
                case ColumnType.FilePath:
                    return FilePath;
                case ColumnType.FileSize:
                    if (FileSize == 0)
                        return "";
                    else
                        return Utility.FormatSize(FileSize);
                case ColumnType.FileType:
                    return FileType;
                case ColumnType.Dimensions:
                    if (Dimensions == Size.Empty)
                        return "";
                    else
                        return string.Format("{0} x {1}", Dimensions.Width, Dimensions.Height);
                case ColumnType.Resolution:
                    if (Resolution == SizeF.Empty)
                        return "";
                    else
                        return string.Format("{0} x {1}", Resolution.Width, Resolution.Height);
                case ColumnType.ImageDescription:
                    return ImageDescription;
                case ColumnType.EquipmentModel:
                    return EquipmentModel;
                case ColumnType.DateTaken:
                    if (DateTaken == DateTime.MinValue)
                        return "";
                    else
                        return DateTaken.ToString("g");
                case ColumnType.Artist:
                    return Artist;
                case ColumnType.Copyright:
                    return Copyright;
                case ColumnType.ExposureTime:
                    return ExposureTime;
                case ColumnType.FNumber:
                    return FNumber.ToString("f2");
                case ColumnType.ISOSpeed:
                    if (ISOSpeed == 0)
                        return "";
                    else
                        return ISOSpeed.ToString();
                case ColumnType.ShutterSpeed:
                    return ShutterSpeed;
                case ColumnType.Aperture:
                    return Aperture;
                case ColumnType.UserComment:
                    return UserComment;
                case ColumnType.Rating:
                    return Rating.ToString();
                default:
                    throw new ArgumentException("Unknown column type", "type");
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Adds a new subitem for the specified custom column.
        /// </summary>
        /// <param name="guid">The Guid of the custom column.</param>
        internal void AddSubItemText(Guid guid)
        {
            subItems.Add(guid, "");
        }
        /// <summary>
        /// Returns the sub item item text corresponding to the specified custom column.
        /// </summary>
        /// <param name="guid">The Guid of the custom column.</param>
        /// <returns>Formatted text for the given column.</returns>
        internal string GetSubItemText(Guid guid)
        {
            return subItems[guid];
        }
        /// <summary>
        /// Sets the sub item item text corresponding to the specified custom column.
        /// </summary>
        /// <param name="guid">The Guid of the custom column.</param>
        /// <param name="text">The text of the subitem.</param>
        /// <returns>Formatted text for the given column.</returns>
        internal void SetSubItemText(Guid guid, string text)
        {
            subItems[guid] = text;
        }
        /// <summary>
        /// Removes the sub item item text corresponding to the specified custom column.
        /// </summary>
        /// <param name="guid">The Guid of the custom column.</param>
        /// <returns>true if the item was removed; otherwise false.</returns>
        internal bool RemoveSubItemText(Guid guid)
        {
            return subItems.Remove(guid);
        }
        /// <summary>
        /// Removes all sub item item texts.
        /// </summary>
        internal void RemoveAllSubItemTexts()
        {
            subItems.Clear();
        }
        /// <summary>
        /// Updates file info for the image file represented by this item.
        /// Item details will be updated synchronously without waiting for the
        /// cache thread.
        /// </summary>
        private void UpdateFileInfo()
        {
            if (!isDirty) return;

            if (isVirtualItem)
            {
                if (mImageListView != null)
                {
                    VirtualItemDetailsEventArgs e = new VirtualItemDetailsEventArgs(mVirtualItemKey);
                    mImageListView.RetrieveVirtualItemDetailsInternal(e);
                    UpdateDetailsInternal(e);
                }
            }
            else
            {
                Utility.ShellImageFileInfo info = new Utility.ShellImageFileInfo(mFileName);
                UpdateDetailsInternal(info);
            }
            isDirty = false;
        }
        /// <summary>
        /// Invoked by the worker thread to update item details.
        /// </summary>
        internal void UpdateDetailsInternal(Utility.ShellImageFileInfo info)
        {
            if (!isDirty) return;

            mDateAccessed = info.LastAccessTime;
            mDateCreated = info.CreationTime;
            mDateModified = info.LastWriteTime;
            mFileSize = info.Size;
            mFileType = info.TypeName;
            mFilePath = info.DirectoryName;
            mDimensions = info.Dimensions;
            mResolution = info.Resolution;
            // Exif tags
            mImageDescription = info.ImageDescription;
            mEquipmentModel = info.EquipmentModel;
            mDateTaken = info.DateTaken;
            mArtist = info.Artist;
            mCopyright = info.Copyright;
            mExposureTime = info.ExposureTime;
            mFNumber = info.FNumber;
            mISOSpeed = info.ISOSpeed;
            mShutterSpeed = info.ShutterSpeed;
            mAperture = info.ApertureValue;
            mUserComment = info.UserComment;
            mRating = info.RatingPercent;
            if (mRating == 0 && info.Rating != 0)
                mRating = (ushort)(info.Rating * 20);

            isDirty = false;
        }
        /// <summary>
        /// Invoked by the worker thread to update item details.
        /// </summary>
        internal void UpdateDetailsInternal(VirtualItemDetailsEventArgs info)
        {
            if (!isDirty) return;

            mDateAccessed = info.DateAccessed;
            mDateCreated = info.DateCreated;
            mDateModified = info.DateModified;
            mFileName = info.FileName;
            mFileSize = info.FileSize;
            mFileType = info.FileType;
            mFilePath = info.FilePath;
            mDimensions = info.Dimensions;
            mResolution = info.Resolution;
            // Exif tags
            mImageDescription = info.ImageDescription;
            mEquipmentModel = info.EquipmentModel;
            mDateTaken = info.DateTaken;
            mArtist = info.Artist;
            mCopyright = info.Copyright;
            mExposureTime = info.ExposureTime;
            mFNumber = info.FNumber;
            mISOSpeed = info.ISOSpeed;
            mShutterSpeed = info.ShutterSpeed;
            mAperture = info.Aperture;
            mUserComment = info.UserComment;
            mRating = info.Rating;

            isDirty = false;
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        public object Clone()
        {
            ImageListViewItem item = new ImageListViewItem();

            item.mText = mText;

            // File info
            item.mDateAccessed = mDateAccessed;
            item.mDateCreated = mDateCreated;
            item.mDateModified = mDateModified;
            item.mFileType = mFileType;
            item.mFileName = mFileName;
            item.mFilePath = mFilePath;
            item.mFileSize = mFileSize;
            item.mDimensions = mDimensions;
            item.mResolution = mResolution;

            // Exif tags
            item.mImageDescription = mImageDescription;
            item.mEquipmentModel = mEquipmentModel;
            item.mDateTaken = mDateTaken;
            item.mArtist = mArtist;
            item.mCopyright = mCopyright;
            item.mExposureTime = mExposureTime;
            item.mFNumber = mFNumber;
            item.mISOSpeed = mISOSpeed;
            item.mShutterSpeed = mShutterSpeed;
            item.mAperture = mAperture;
            item.mUserComment = mUserComment;
            item.mRating = mRating;

            // Virtual item properties
            item.isVirtualItem = isVirtualItem;
            item.mVirtualItemKey = mVirtualItemKey;

            // Sub items
            foreach (KeyValuePair<Guid, string> kv in subItems)
                item.subItems.Add(kv.Key, kv.Value);

            return item;
        }
        #endregion
    }
}
