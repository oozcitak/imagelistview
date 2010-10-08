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
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;

namespace Manina.Windows.Forms
{
    public partial class ImageListView
    {
        /// <summary>
        /// Represents the collection of items in the image list view.
        /// </summary>
        public class ImageListViewItemCollection : IList<ImageListViewItem>, ICollection, IList, IEnumerable
        {
            #region Member Variables
            private List<ImageListViewItem> mItems;
            internal ImageListView mImageListView;
            private ImageListViewItem mFocused;
            private Dictionary<Guid, ImageListViewItem> lookUp;
            internal bool collectionModified;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="ImageListViewItemCollection"/>  class.
            /// </summary>
            /// <param name="owner">The <see cref="ImageListView"/> owning this collection.</param>
            internal ImageListViewItemCollection(ImageListView owner)
            {
                mItems = new List<ImageListViewItem>();
                lookUp = new Dictionary<Guid, ImageListViewItem>();
                mFocused = null;
                mImageListView = owner;
                collectionModified = true;
            }
            #endregion

            #region Properties
            /// <summary>
            /// Gets the number of elements contained in the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            public int Count
            {
                get { return mItems.Count; }
            }
            /// <summary>
            /// Gets a value indicating whether the <see cref="ImageListViewItemCollection"/> is read-only.
            /// </summary>
            public bool IsReadOnly
            {
                get { return false; }
            }
            /// <summary>
            /// Gets or sets the focused item.
            /// </summary>
            public ImageListViewItem FocusedItem
            {
                get
                {
                    return mFocused;
                }
                set
                {
                    ImageListViewItem oldFocusedItem = mFocused;
                    mFocused = value;
                    // Refresh items
                    if (oldFocusedItem != mFocused && mImageListView != null)
                        mImageListView.Refresh();
                }
            }
            /// <summary>
            /// Gets the <see cref="ImageListView"/> owning this collection.
            /// </summary>
            [Category("Behavior"), Browsable(false), Description("Gets the ImageListView owning this collection.")]
            public ImageListView ImageListView { get { return mImageListView; } }
            /// <summary>
            /// Gets or sets the <see cref="ImageListViewItem"/> at the specified index.
            /// </summary>
            [Category("Behavior"), Browsable(false), Description("Gets or sets the item at the specified index.")]
            public ImageListViewItem this[int index]
            {
                get
                {
                    return mItems[index];
                }
                set
                {
                    ImageListViewItem item = value;
                    ImageListViewItem oldItem = mItems[index];

                    if (mItems[index] == mFocused)
                        mFocused = item;
                    bool oldSelected = mItems[index].Selected;
                    item.mIndex = index;
                    if (mImageListView != null)
                        item.mImageListView = mImageListView;
                    item.owner = this;
                    mItems[index] = item;
                    lookUp.Remove(oldItem.Guid);
                    lookUp.Add(item.Guid, item);
                    collectionModified = true;

                    if (mImageListView != null)
                    {
                        mImageListView.thumbnailCache.Remove(oldItem.Guid);
                        mImageListView.itemCacheManager.Remove(oldItem.Guid);
                        if (mImageListView.CacheMode == CacheMode.Continuous)
                        {
                            if (item.isVirtualItem)
                                mImageListView.thumbnailCache.Add(item.Guid, item.VirtualItemKey,
                                    mImageListView.ThumbnailSize, mImageListView.UseEmbeddedThumbnails,
                                    mImageListView.AutoRotateThumbnails,
                                    (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                            else
                                mImageListView.thumbnailCache.Add(item.Guid, item.FileName,
                                    mImageListView.ThumbnailSize, mImageListView.UseEmbeddedThumbnails, 
                                    mImageListView.AutoRotateThumbnails,
                                    (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                        }
                        if (item.isVirtualItem)
                            mImageListView.itemCacheManager.Add(item.Guid, item.VirtualItemKey);
                        else
                            mImageListView.itemCacheManager.Add(item.Guid, item.FileName);
                        if (item.Selected != oldSelected)
                            mImageListView.OnSelectionChanged(new EventArgs());
                    }
                }
            }
            /// <summary>
            /// Gets the <see cref="ImageListViewItem"/> with the specified Guid.
            /// </summary>
            [Category("Behavior"), Browsable(false), Description("Gets or sets the item with the specified Guid.")]
            internal ImageListViewItem this[Guid guid]
            {
                get
                {
                    return lookUp[guid];
                }
            }
            #endregion

            #region Instance Methods
            /// <summary>
            /// Adds an item to the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="item">The <see cref="ImageListViewItem"/> to add to the <see cref="ImageListViewItemCollection"/>.</param>
            public void Add(ImageListViewItem item)
            {
                AddInternal(item);

                if (mImageListView != null)
                {
                    if (item.Selected)
                        mImageListView.OnSelectionChangedInternal();
                    mImageListView.Refresh();
                }
            }
            /// <summary>
            /// Adds an item to the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="filename">The name of the image file.</param>
            public void Add(string filename)
            {
                Add(new ImageListViewItem(filename));
            }
            /// <summary>
            /// Adds an item to the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="filename">The name of the image file.</param>
            /// <param name="initialThumbnail">The initial thumbnail image for the item.</param>
            public void Add(string filename, Image initialThumbnail)
            {
                ImageListViewItem item = new ImageListViewItem(filename);
                if (mImageListView != null && initialThumbnail != null)
                {
                    mImageListView.thumbnailCache.Add(item.Guid, filename, mImageListView.ThumbnailSize,
                        initialThumbnail, mImageListView.UseEmbeddedThumbnails, mImageListView.AutoRotateThumbnails,
                        (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                }
                Add(item);
            }
            /// <summary>
            /// Adds a virtual item to the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="key">The key identifying the item.</param>
            /// <param name="text">Text of the item.</param>
            public void Add(object key, string text)
            {
                Add(key, text, null);
            }
            /// <summary>
            /// Adds a virtual item to the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="key">The key identifying the item.</param>
            /// <param name="text">Text of the item.</param>
            /// <param name="initialThumbnail">The initial thumbnail image for the item.</param>
            public void Add(object key, string text, Image initialThumbnail)
            {
                ImageListViewItem item = new ImageListViewItem(key, text);
                if (mImageListView != null && initialThumbnail != null)
                {
                    mImageListView.thumbnailCache.Add(item.Guid, key, mImageListView.ThumbnailSize,
                        initialThumbnail, mImageListView.UseEmbeddedThumbnails, mImageListView.AutoRotateThumbnails,
                        (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                }
                Add(item);
            }
            /// <summary>
            /// Adds a range of items to the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="items">An array of <see cref="ImageListViewItem"/> 
            /// to add to the <see cref="ImageListViewItemCollection"/>.</param>
            public void AddRange(ImageListViewItem[] items)
            {
                if (mImageListView != null)
                    mImageListView.SuspendPaint();

                foreach (ImageListViewItem item in items)
                    Add(item);

                if (mImageListView != null)
                {
                    mImageListView.Refresh();
                    mImageListView.ResumePaint();
                }
            }
            /// <summary>
            /// Adds a range of items to the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="filenames">The names or the image files.</param>
            public void AddRange(string[] filenames)
            {
                if (mImageListView != null)
                    mImageListView.SuspendPaint();

                for (int i = 0; i < filenames.Length; i++)
                {
                    Add(filenames[i]);
                }

                if (mImageListView != null)
                {
                    mImageListView.Refresh();
                    mImageListView.ResumePaint();
                }

            }
            /// <summary>
            /// Removes all items from the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            public void Clear()
            {
                mItems.Clear();

                mFocused = null;
                lookUp.Clear();
                collectionModified = true;

                if (mImageListView != null)
                {
                    mImageListView.itemCacheManager.Clear();
                    mImageListView.thumbnailCache.Clear();
                    mImageListView.SelectedItems.Clear();
                    mImageListView.Refresh();
                }
            }
            /// <summary>
            /// Determines whether the <see cref="ImageListViewItemCollection"/> 
            /// contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the 
            /// <see cref="ImageListViewItemCollection"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> is found in the 
            /// <see cref="ImageListViewItemCollection"/>; otherwise, false.
            /// </returns>
            public bool Contains(ImageListViewItem item)
            {
                return mItems.Contains(item);
            }
            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> 
            /// that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<ImageListViewItem> GetEnumerator()
            {
                return mItems.GetEnumerator();
            }
            /// <summary>
            /// Inserts an item to the <see cref="ImageListViewItemCollection"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
            /// <param name="item">The <see cref="ImageListViewItem"/> to 
            /// insert into the <see cref="ImageListViewItemCollection"/>.</param>
            public void Insert(int index, ImageListViewItem item)
            {
                InsertInternal(index, item);

                if (mImageListView != null)
                {
                    if (item.Selected)
                        mImageListView.OnSelectionChangedInternal();
                    mImageListView.Refresh();
                }
            }
            /// <summary>
            /// Inserts an item to the <see cref="ImageListViewItemCollection"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
            /// <param name="filename">The name of the image file.</param>
            public void Insert(int index, string filename)
            {
                Insert(index, new ImageListViewItem(filename));
            }
            /// <summary>
            /// Inserts an item to the <see cref="ImageListViewItemCollection"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
            /// <param name="filename">The name of the image file.</param>
            /// <param name="initialThumbnail">The initial thumbnail image for the item.</param>
            public void Insert(int index, string filename, Image initialThumbnail)
            {
                ImageListViewItem item = new ImageListViewItem(filename);
                if (mImageListView != null && initialThumbnail != null)
                {
                    mImageListView.thumbnailCache.Add(item.Guid, filename, mImageListView.ThumbnailSize,
                        initialThumbnail, mImageListView.UseEmbeddedThumbnails, mImageListView.AutoRotateThumbnails,
                        (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                }
                Insert(index, item);
            }
            /// <summary>
            /// Inserts a virtual item to the <see cref="ImageListViewItemCollection"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
            /// <param name="key">The key identifying the item.</param>
            /// <param name="text">Text of the item.</param>
            public void Insert(int index, object key, string text)
            {
                Insert(index, key, text, null);
            }
            /// <summary>
            /// Inserts a virtual item to the <see cref="ImageListViewItemCollection"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
            /// <param name="key">The key identifying the item.</param>
            /// <param name="text">Text of the item.</param>
            /// <param name="initialThumbnail">The initial thumbnail image for the item.</param>
            public void Insert(int index, object key, string text, Image initialThumbnail)
            {
                ImageListViewItem item = new ImageListViewItem(key, text);
                if (mImageListView != null && initialThumbnail != null)
                {
                    mImageListView.thumbnailCache.Add(item.Guid, key, mImageListView.ThumbnailSize,
                        initialThumbnail, mImageListView.UseEmbeddedThumbnails, mImageListView.AutoRotateThumbnails,
                        (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                }
                Insert(index, item);
            }

            /// <summary>
            /// Removes the first occurrence of a specific object 
            /// from the <see cref="ImageListViewItemCollection"/>.
            /// </summary>
            /// <param name="item">The <see cref="ImageListViewItem"/> to remove 
            /// from the <see cref="ImageListViewItemCollection"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> was successfully removed from the 
            /// <see cref="ImageListViewItemCollection"/>; otherwise, false. This method also 
            /// returns false if <paramref name="item"/> is not found in the original 
            /// <see cref="ImageListViewItemCollection"/>.
            /// </returns>
            public bool Remove(ImageListViewItem item)
            {
                for (int i = item.mIndex; i < mItems.Count; i++)
                    mItems[i].mIndex--;
                if (item == mFocused) mFocused = null;
                bool ret = mItems.Remove(item);
                lookUp.Remove(item.Guid);
                collectionModified = true;
                if (mImageListView != null)
                {
                    mImageListView.thumbnailCache.Remove(item.Guid);
                    mImageListView.itemCacheManager.Remove(item.Guid);
                    if (item.Selected)
                        mImageListView.OnSelectionChangedInternal();
                    mImageListView.Refresh();
                }
                return ret;
            }
            /// <summary>
            /// Removes the <see cref="ImageListViewItem"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index of the item to remove.</param>
            public void RemoveAt(int index)
            {
                ImageListViewItem item = mItems[index];
                Remove(item);
            }
            #endregion

            #region Helper Methods
            /// <summary>
            /// Adds the (empty) subitem to each item for the given custom column.
            /// </summary>
            /// <param name="guid">Custom column ID.</param>
            internal void AddCustomColumn(Guid guid)
            {
                foreach (ImageListViewItem item in mItems)
                    item.AddSubItemText(guid);
            }
            /// <summary>
            /// Determines whether the collection contains the given key.
            /// </summary>
            /// <param name="guid">The key of the item.</param>
            /// <returns>true if the collection contains the given key; otherwise false.</returns>
            internal bool ContainsKey(Guid guid)
            {
                return lookUp.ContainsKey(guid);
            }
            /// <summary>
            /// Gets the value associated with the specified key.
            /// </summary>
            /// <param name="guid">The key of the item.</param>
            /// <param name="item">the value associated with the specified key, 
            /// if the key is found; otherwise, the default value for the type 
            /// of the value parameter. This parameter is passed uninitialized.</param>
            /// <returns>true if the collection contains the given key; otherwise false.</returns>
            internal bool TryGetValue(Guid guid, out ImageListViewItem item)
            {
                return lookUp.TryGetValue(guid, out item);
            }
            /// <summary>
            /// Removes the subitem of each item for the given custom column.
            /// </summary>
            /// <param name="guid">Custom column ID.</param>
            internal void RemoveCustomColumn(Guid guid)
            {
                foreach (ImageListViewItem item in mItems)
                    item.RemoveSubItemText(guid);
            }
            /// <summary>
            /// Removes the subitem of each item for the given custom column.
            /// </summary>
            internal void RemoveAllCustomColumns()
            {
                foreach (ImageListViewItem item in mItems)
                    item.RemoveAllSubItemTexts();
            }
            /// <summary>
            /// Adds the given item without raising a selection changed event.
            /// </summary>
            internal void AddInternal(ImageListViewItem item)
            {
                InsertInternal(-1, item);
            }
            /// <summary>
            /// Inserts the given item without raising a selection changed event.
            /// </summary>
            /// <param name="index">Insertion index. If index is -1 the item is added to the end of the list.</param>
            /// <param name="item">The <see cref="ImageListViewItem"/> to add.</param>
            internal void InsertInternal(int index, ImageListViewItem item)
            {
                // Check if the file already exists
                if (mImageListView != null && !item.isVirtualItem && !mImageListView.AllowDuplicateFileNames)
                {
                    if (mItems.Exists(a => string.Compare(a.FileName, item.FileName, StringComparison.OrdinalIgnoreCase) == 0))
                        return;
                }
                item.owner = this;
                if (index == -1)
                {
                    item.mIndex = mItems.Count;
                    mItems.Add(item);
                }
                else
                {
                    item.mIndex = index;
                    for (int i = index; i < mItems.Count; i++)
                        mItems[i].mIndex++;
                    mItems.Insert(index, item);
                }
                lookUp.Add(item.Guid, item);
                collectionModified = true;
                if (mImageListView != null)
                {
                    item.mImageListView = mImageListView;

                    // Create sub item texts for custom columns
                    foreach (ImageListViewColumnHeader header in mImageListView.Columns)
                        if (header.Type == ColumnType.Custom)
                            item.AddSubItemText(header.columnID);

                    // Add to thumbnail cache
                    if (mImageListView.CacheMode == CacheMode.Continuous)
                    {
                        if (item.isVirtualItem)
                            mImageListView.thumbnailCache.Add(item.Guid, item.VirtualItemKey,
                                mImageListView.ThumbnailSize, mImageListView.UseEmbeddedThumbnails, mImageListView.AutoRotateThumbnails,
                                (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                        else
                            mImageListView.thumbnailCache.Add(item.Guid, item.FileName,
                                mImageListView.ThumbnailSize, mImageListView.UseEmbeddedThumbnails, mImageListView.AutoRotateThumbnails,
                                (mImageListView.UseWIC == UseWIC.Auto || mImageListView.UseWIC == UseWIC.ThumbnailsOnly));
                    }

                    // Add to details cache
                    if (item.isVirtualItem)
                        mImageListView.itemCacheManager.Add(item.Guid, item.VirtualItemKey);
                    else
                        mImageListView.itemCacheManager.Add(item.Guid, item.FileName);

                    // Add to shell info cache
                    if (!item.isVirtualItem)
                    {
                        string extension = item.extension;
                        CacheState state = mImageListView.shellInfoCache.GetCacheState(extension);
                        if (state == CacheState.Error && mImageListView.RetryOnError == true)
                        {
                            mImageListView.shellInfoCache.Remove(extension);
                            mImageListView.shellInfoCache.Add(extension);
                        }
                        else if (state == CacheState.Unknown)
                            mImageListView.shellInfoCache.Add(extension);
                    }
                }
            }
            /// <summary>
            /// Removes the given item without raising a selection changed event.
            /// </summary>
            /// <param name="item">The item to remove.</param>
            internal void RemoveInternal(ImageListViewItem item)
            {
                RemoveInternal(item, true);
            }
            /// <summary>
            /// Removes the given item without raising a selection changed event.
            /// </summary>
            /// <param name="item">The item to remove.</param>
            /// <param name="removeFromCache">true to remove item image from cache; otherwise false.</param>
            internal void RemoveInternal(ImageListViewItem item, bool removeFromCache)
            {
                for (int i = item.mIndex; i < mItems.Count; i++)
                    mItems[i].mIndex--;
                if (item == mFocused) mFocused = null;
                if (removeFromCache && mImageListView != null)
                {
                    mImageListView.thumbnailCache.Remove(item.Guid);
                    mImageListView.itemCacheManager.Remove(item.Guid);
                }
                mItems.Remove(item);
                lookUp.Remove(item.Guid);
                collectionModified = true;
            }
            /// <summary>
            /// Returns the index of the specified item.
            /// </summary>
            internal int IndexOf(ImageListViewItem item)
            {
                return item.Index;
            }
            /// <summary>
            /// Returns the index of the item with the specified Guid.
            /// </summary>
            internal int IndexOf(Guid guid)
            {
                ImageListViewItem item = null;
                if (lookUp.TryGetValue(guid, out item))
                    return item.Index;
                return -1;
            }
            /// <summary>
            /// Sorts the items by the sort order and sort column of the owner.
            /// </summary>
            internal void Sort()
            {
                if (mImageListView == null || mImageListView.SortOrder == SortOrder.None ||
                    mImageListView.SortColumn < 0 || mImageListView.SortColumn >= mImageListView.Columns.Count)
                    return;

                // Display wait cursor while sorting
                Cursor cursor = mImageListView.Cursor;
                mImageListView.Cursor = Cursors.WaitCursor;

                // Sort items
                mItems.Sort(new ImageListViewItemComparer(mImageListView.Columns[mImageListView.SortColumn], mImageListView.SortOrder));

                // Update item indices
                for (int i = 0; i < mItems.Count; i++)
                    mItems[i].mIndex = i;

                // Restore previous cusrsor
                mImageListView.Cursor = cursor;
                collectionModified = true;
            }
            #endregion

            #region ImageListViewItemComparer
            /// <summary>
            /// Compares items by the sort order and sort column of the owner.
            /// </summary>
            private class ImageListViewItemComparer : IComparer<ImageListViewItem>
            {
                private ImageListViewColumnHeader mSortColumn;
                private SortOrder mOrder;

                public ImageListViewItemComparer(ImageListViewColumnHeader sortColumn, SortOrder order)
                {
                    mSortColumn = sortColumn;
                    mOrder = order;
                }

                /// <summary>
                /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
                /// </summary>
                public int Compare(ImageListViewItem x, ImageListViewItem y)
                {
                    int sign = (mOrder == SortOrder.Ascending ? 1 : -1);
                    int result = 0;
                    switch (mSortColumn.Type)
                    {
                        case ColumnType.DateAccessed:
                            result = DateTime.Compare(x.DateAccessed, y.DateAccessed);
                            break;
                        case ColumnType.DateCreated:
                            result = DateTime.Compare(x.DateCreated, y.DateCreated);
                            break;
                        case ColumnType.DateModified:
                            result = DateTime.Compare(x.DateModified, y.DateModified);
                            break;
                        case ColumnType.Dimensions:
                            long ax = x.Dimensions.Width * x.Dimensions.Height;
                            long ay = y.Dimensions.Width * y.Dimensions.Height;
                            result = (ax < ay ? -1 : (ax > ay ? 1 : 0));
                            break;
                        case ColumnType.FileName:
                            result = string.Compare(x.FileName, y.FileName, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.FilePath:
                            result = string.Compare(x.FilePath, y.FilePath, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.FileSize:
                            result = (x.FileSize < y.FileSize ? -1 : (x.FileSize > y.FileSize ? 1 : 0));
                            break;
                        case ColumnType.FileType:
                            result = string.Compare(x.FileType, y.FileType, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.Name:
                            result = string.Compare(x.Text, y.Text, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.Resolution:
                            float rx = x.Resolution.Width * x.Resolution.Height;
                            float ry = y.Resolution.Width * y.Resolution.Height;
                            result = (rx < ry ? -1 : (rx > ry ? 1 : 0));
                            break;
                        case ColumnType.ImageDescription:
                            result = string.Compare(x.ImageDescription, y.ImageDescription, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.EquipmentModel:
                            result = string.Compare(x.EquipmentModel, y.EquipmentModel, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.DateTaken:
                            result = DateTime.Compare(x.DateTaken, y.DateTaken);
                            break;
                        case ColumnType.Artist:
                            result = string.Compare(x.Artist, y.Artist, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.Copyright:
                            result = string.Compare(x.Copyright, y.Copyright, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.ExposureTime:
                            result = (x.ExposureTime < y.ExposureTime ? -1 : (x.ExposureTime > y.ExposureTime ? 1 : 0));
                            break;
                        case ColumnType.FNumber:
                            result = (x.FNumber < y.FNumber ? -1 : (x.FNumber > y.FNumber ? 1 : 0));
                            break;
                        case ColumnType.ISOSpeed:
                            result = (x.ISOSpeed < y.ISOSpeed ? -1 : (x.ISOSpeed > y.ISOSpeed ? 1 : 0));
                            break;
                        case ColumnType.UserComment:
                            result = string.Compare(x.UserComment, y.UserComment, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.Rating:
                            result = (x.Rating < y.Rating ? -1 : (x.Rating > y.Rating ? 1 : 0));
                            break;
                        case ColumnType.Software:
                            result = string.Compare(x.Software, y.Software, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case ColumnType.FocalLength:
                            result = (x.FocalLength < y.FocalLength ? -1 : (x.FocalLength > y.FocalLength ? 1 : 0));
                            break;
                        case ColumnType.Custom:
                            result = string.Compare(x.GetSubItemText(mSortColumn.columnID), y.GetSubItemText(mSortColumn.columnID), StringComparison.InvariantCultureIgnoreCase);
                            break;
                        default:
                            result = 0;
                            break;
                    }
                    return sign * result;
                }
            }
            #endregion

            #region Unsupported Interface
            /// <summary>
            /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            void ICollection<ImageListViewItem>.CopyTo(ImageListViewItem[] array, int arrayIndex)
            {
                mItems.CopyTo(array, arrayIndex);
            }
            /// <summary>
            /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
            /// </summary>
            [Obsolete("Use ImageListViewItem.Index property instead.")]
            int IList<ImageListViewItem>.IndexOf(ImageListViewItem item)
            {
                return mItems.IndexOf(item);
            }
            /// <summary>
            /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            void ICollection.CopyTo(Array array, int index)
            {
                if (!(array is ImageListViewItem[]))
                    throw new ArgumentException("An array of ImageListViewItem is required.", "array");
                mItems.CopyTo((ImageListViewItem[])array, index);
            }
            /// <summary>
            /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            int ICollection.Count
            {
                get { return mItems.Count; }
            }
            /// <summary>
            /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
            /// </summary>
            bool ICollection.IsSynchronized
            {
                get { return false; }
            }
            /// <summary>
            /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
            /// </summary>
            object ICollection.SyncRoot
            {
                get { throw new NotSupportedException(); }
            }
            /// <summary>
            /// Adds an item to the <see cref="T:System.Collections.IList"/>.
            /// </summary>
            int IList.Add(object value)
            {
                if (!(value is ImageListViewItem))
                    throw new ArgumentException("An object of type ImageListViewItem is required.", "value");
                ImageListViewItem item = (ImageListViewItem)value;
                Add(item);
                return mItems.IndexOf(item);
            }
            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.IList"/> contains a specific value.
            /// </summary>
            bool IList.Contains(object value)
            {
                if (!(value is ImageListViewItem))
                    throw new ArgumentException("An object of type ImageListViewItem is required.", "value");
                return mItems.Contains((ImageListViewItem)value);
            }
            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return mItems.GetEnumerator();
            }
            /// <summary>
            /// Determines the index of a specific item in the <see cref="T:System.Collections.IList"/>.
            /// </summary>
            int IList.IndexOf(object value)
            {
                if (!(value is ImageListViewItem))
                    throw new ArgumentException("An object of type ImageListViewItem is required.", "value");
                return IndexOf((ImageListViewItem)value);
            }
            /// <summary>
            /// Inserts an item to the <see cref="T:System.Collections.IList"/> at the specified index.
            /// </summary>
            void IList.Insert(int index, object value)
            {
                if (!(value is ImageListViewItem))
                    throw new ArgumentException("An object of type ImageListViewItem is required.", "value");
                Insert(index, (ImageListViewItem)value);
            }
            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> has a fixed size.
            /// </summary>
            bool IList.IsFixedSize
            {
                get { return false; }
            }
            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList"/>.
            /// </summary>
            void IList.Remove(object value)
            {
                if (!(value is ImageListViewItem))
                    throw new ArgumentException("An object of type ImageListViewItem is required.", "value");
                ImageListViewItem item = (ImageListViewItem)value;
                Remove(item);
            }
            /// <summary>
            /// Gets or sets the <see cref="System.Object"/> at the specified index.
            /// </summary>
            object IList.this[int index]
            {
                get
                {
                    return this[index];
                }
                set
                {
                    if (!(value is ImageListViewItem))
                        throw new ArgumentException("An object of type ImageListViewItem is required.", "value");
                    this[index] = (ImageListViewItem)value;
                }
            }
            #endregion
        }
    }
}