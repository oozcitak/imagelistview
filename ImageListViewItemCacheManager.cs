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
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the cache manager responsible for asynchronously loading
    /// item details.
    /// </summary>
    internal class ImageListViewItemCacheManager : IDisposable
    {
        #region Constants
        /// <summary>
        /// The cache manager will clean up removed items
        /// if they exceed this amount.
        /// </summary>
        public const int CleanUpLimit = 200;
        #endregion

        #region Member Variables
        private readonly object lockObject;

        private ImageListView mImageListView;
        private bool mRetryOnError;
        private Thread mThread;

        private Queue<CacheItem> toCache;
        private Dictionary<Guid, CacheItem> itemCache;
        private Dictionary<Guid, bool> editCache;

        private List<Guid> removedItems;

        private Dictionary<string, CachedShellInfo> cachedShellInfo;

        private volatile bool stopping;
        private bool stopped;
        private bool disposed;
        #endregion

        #region Private Classes
        /// <summary>
        /// Represents cached shell properties.
        /// </summary>
        private class CachedShellInfo : IDisposable
        {
            private string mFileType;
            private Image mSmallIcon;
            private Image mLargeIcon;
            private bool disposed;

            /// <summary>
            /// Gets the mime type of the image file.
            /// </summary>
            public string FileType { get { return mFileType; } }
            /// <summary>
            /// Gets the small shell icon of the image file.
            /// </summary>
            public Image SmallIcon { get { return mSmallIcon; } }
            /// <summary>
            /// Gets the large shell icon of the image file.
            /// </summary>
            public Image LargeIcon { get { return mLargeIcon; } }

            /// <summary>
            /// Initializes a new instance of the CachedShellInfo class.
            /// </summary>
            /// <param name="fileType">Mime type of the file.</param>
            /// <param name="smallIcon">The small icon.</param>
            /// <param name="largeIcon">The large icon.</param>
            public CachedShellInfo(string fileType, Image smallIcon, Image largeIcon)
            {
                disposed = false;
                mFileType = fileType;
                mSmallIcon = smallIcon;
                mLargeIcon = largeIcon;
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, 
            /// releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (!disposed)
                {
                    if (mSmallIcon != null)
                        mSmallIcon.Dispose();
                    if (mLargeIcon != null)
                        mLargeIcon.Dispose();

                    mSmallIcon = null;
                    mLargeIcon = null;
                    disposed = true;
                }
            }
        }
        /// <summary>
        /// Represents an item in the thumbnail cache.
        /// </summary>
        private class CacheItem : IDisposable
        {
            private Guid mGuid;
            private string mFileName;
            private bool mIsVirtualItem;
            private object mVirtualItemKey;
            private Image mSmallIcon;
            private Image mLargeIcon;
            private bool disposed;

            /// <summary>
            /// Gets the guid of the item.
            /// </summary>
            public Guid Guid { get { return mGuid; } }
            /// <summary>
            /// Gets the name of the image file.
            /// </summary>
            public string FileName { get { return mFileName; } }
            /// <summary>
            /// Gets whether Item is a virtual item.
            /// </summary>
            public bool IsVirtualItem { get { return mIsVirtualItem; } }
            /// <summary>
            /// Gets the public key for the virtual item.
            /// </summary>
            public object VirtualItemKey { get { return mVirtualItemKey; } }
            /// <summary>
            /// Gets the cached small shell icon.
            /// </summary>
            public Image SmallIcon { get { return mSmallIcon; } }
            /// <summary>
            /// Gets the cached large shell icon.
            /// </summary>
            public Image LargeIcon { get { return mLargeIcon; } }

            /// <summary>
            /// Initializes a new instance of the CacheItem class.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem associated 
            /// with this request.</param>
            /// <param name="filename">The file system path to the image file.</param>
            public CacheItem(Guid guid, string filename)
                : this(guid, filename, null, null)
            {
                ;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem associated 
            /// with this request.</param>
            /// <param name="filename">The file system path to the image file.</param>
            /// <param name="smallIcon">The small shell icon.</param>
            /// <param name="largeIcon">The small shell icon.</param>
            public CacheItem(Guid guid, string filename, Image smallIcon, Image largeIcon)
            {
                disposed = false;
                mGuid = guid;
                mFileName = filename;
                mIsVirtualItem = false;
                mVirtualItemKey = null;
                mSmallIcon = smallIcon;
                mLargeIcon = largeIcon;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class
            /// for use with a virtual item.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem associated 
            /// with this request.</param>
            /// <param name="key">The public key for the virtual item.</param>
            public CacheItem(Guid guid, object key)
                : this(guid, key, null, null)
            {
                ;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class
            /// for use with a virtual item.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem associated 
            /// with this request.</param>
            /// <param name="key">The public key for the virtual item.</param>
            /// <param name="smallIcon">The small shell icon.</param>
            /// <param name="largeIcon">The small shell icon.</param>
            public CacheItem(Guid guid, object key, Image smallIcon, Image largeIcon)
            {
                disposed = false;
                mGuid = guid;
                mFileName = string.Empty;
                mIsVirtualItem = true;
                mVirtualItemKey = key;
                mSmallIcon = smallIcon;
                mLargeIcon = largeIcon;
            }

            /// <summary>
            /// Performs application-defined tasks associated with 
            /// freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (!disposed)
                {
                    if (mSmallIcon != null)
                        mSmallIcon.Dispose();
                    if (mLargeIcon != null)
                        mLargeIcon.Dispose();

                    mSmallIcon = null;
                    mLargeIcon = null;

                    disposed = true;
                    GC.SuppressFinalize(this);
                }
            }
#if DEBUG
            /// <summary>
            /// Releases unmanaged resources and performs other cleanup operations before the
            /// CacheItem is reclaimed by garbage collection.
            /// </summary>
            ~CacheItem()
            {
                if (mSmallIcon != null || mLargeIcon != null)
                    System.Diagnostics.Debug.Print("Finalizer of {0} called for non-empty cache item.", GetType());
                Dispose();
            }
#endif
        }
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether the cache manager retries loading items on errors.
        /// </summary>
        public bool RetryOnError { get { return mRetryOnError; } set { mRetryOnError = value; } }
        /// <summary>
        /// Determines whether the cache thread is being stopped.
        /// </summary>
        private bool Stopping { get { lock (lockObject) { return stopping; } } }
        /// <summary>
        /// Determines whether the cache thread is stopped.
        /// </summary>
        public bool Stopped { get { lock (lockObject) { return stopped; } } }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ImageListViewItemCacheManager class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public ImageListViewItemCacheManager(ImageListView owner)
        {
            lockObject = new object();

            mImageListView = owner;

            toCache = new Queue<CacheItem>();
            itemCache = new Dictionary<Guid, CacheItem>();
            editCache = new Dictionary<Guid, bool>();

            mThread = new Thread(new ThreadStart(DoWork));
            mThread.IsBackground = true;

            removedItems = new List<Guid>();

            cachedShellInfo = new Dictionary<string, CachedShellInfo>();

            stopping = false;
            stopped = false;
            disposed = false;

            mThread.Start();
            while (!mThread.IsAlive) ;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Starts editing an item. While items are edited,
        /// their original images will be seperately cached
        /// instead of fetching them from the file.
        /// </summary>
        /// <param name="guid">The GUID of the item</param>
        public void BeginItemEdit(Guid guid)
        {
            lock (lockObject)
            {
                if (!editCache.ContainsKey(guid))
                    editCache.Add(guid, false);
            }
        }
        /// <summary>
        /// Ends editing an item. After this call, item
        /// image will be continued to be fetched from the
        /// file.
        /// </summary>
        /// <param name="guid"></param>
        public void EndItemEdit(Guid guid)
        {
            lock (lockObject)
            {
                if (editCache.ContainsKey(guid))
                {
                    editCache.Remove(guid);
                }
            }
        }
        /// <summary>
        /// Adds an item to the cache queue.
        /// </summary>
        public void Add(Guid guid, string filename)
        {
            lock (lockObject)
            {
                toCache.Enqueue(new CacheItem(guid, filename));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Adds a virtual item to the cache queue.
        /// </summary>
        public void Add(Guid guid, object key)
        {
            lock (lockObject)
            {
                toCache.Enqueue(new CacheItem(guid, key));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Clears the thumbnail cache.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                foreach (CacheItem item in itemCache.Values)
                    item.Dispose();
                itemCache.Clear();

                foreach (CachedShellInfo item in cachedShellInfo.Values)
                    item.Dispose();
                cachedShellInfo.Clear();

                removedItems.Clear();
                toCache.Clear();
                editCache.Clear();
            }
        }
        /// <summary>
        /// Removes the given item from the cache.
        /// </summary>
        /// <param name="guid">The guid of the item to remove.</param>
        public void Remove(Guid guid)
        {
            Remove(guid, false);
        }
        /// <summary>
        /// Removes the given item from the cache.
        /// </summary>
        /// <param name="guid">The guid of the item to remove.</param>
        /// <param name="removeNow">true to remove the item now; false to remove the
        /// item later when the cache is purged.</param>
        public void Remove(Guid guid, bool removeNow)
        {
            lock (lockObject)
            {
                CacheItem item = null;
                if (!itemCache.TryGetValue(guid, out item))
                    return;

                if (removeNow)
                {
                    item.Dispose();
                    itemCache.Remove(guid);
                }
                else
                {
                    removedItems.Add(guid);

                    // Remove items now if we can free more than CleanUpLimit items
                    if (removedItems.Count > CleanUpLimit)
                    {
                        CacheItem itemToRemove = null;
                        foreach (Guid iguid in removedItems)
                        {
                            if (itemCache.TryGetValue(iguid, out itemToRemove))
                            {
                                itemToRemove.Dispose();
                                itemCache.Remove(iguid);
                            }
                        }
                        removedItems.Clear();
                    }
                }
            }
        }
        /// <summary>
        /// Gets the small shell icon from the cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="clone">true to return a cloned image; otherwise false.</param>
        public Image GetSmallIcon(Guid guid, bool clone)
        {
            lock (lockObject)
            {
                CacheItem item = null;
                if (itemCache.TryGetValue(guid, out item))
                {
                    Image img = item.SmallIcon;
                    if (clone && img != null)
                        img = (Image)img.Clone();
                    return img;
                }
            }
            return null;
        }
        /// <summary>
        /// Gets the small shell icon from the cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        public Image GetSmallIcon(Guid guid)
        {
            return GetSmallIcon(guid, false);
        }
        /// <summary>
        /// Gets the large shell icon from the cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="clone">true to return a cloned image; otherwise false.</param>
        public Image GetLargeIcon(Guid guid, bool clone)
        {
            lock (lockObject)
            {
                CacheItem item = null;
                if (itemCache.TryGetValue(guid, out item))
                {
                    Image img = item.LargeIcon;
                    if (clone && img != null)
                        img = (Image)img.Clone();
                    return img;
                }
            }
            return null;
        }
        /// <summary>
        /// Gets the large shell icon from the cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        public Image GetLargeIcon(Guid guid)
        {
            return GetLargeIcon(guid, false);
        }
        /// <summary>
        /// Adds the given item to the cache bypassing the worker thread.
        /// </summary>
        /// <param name="guid">Item guid.</param>
        /// <param name="key">Virtual item key.</param>
        /// <param name="info">File info.</param>
        public void ForceAddToCache(Guid guid, object key, ShellImageFileInfo info)
        {
            lock (lockObject)
            {
                // Is it already cached?
                CacheItem cacheItem = null;
                if (itemCache.TryGetValue(guid, out cacheItem))
                {
                    itemCache.Remove(guid);
                    cacheItem.Dispose();
                }

                itemCache.Add(guid, new CacheItem(guid, key, info.SmallIcon, info.LargeIcon));
            }
        }
        /// <summary>
        /// Stops the cache manager.
        /// </summary>
        public void Stop()
        {
            lock (lockObject)
            {
                if (!stopping)
                {
                    stopping = true;
                    Monitor.Pulse(lockObject);
                }
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                Clear();

                disposed = true;

                GC.SuppressFinalize(this);
            }
        }
#if DEBUG
        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// ImageListViewItemCacheManager is reclaimed by garbage collection.
        /// </summary>
        ~ImageListViewItemCacheManager()
        {
            System.Diagnostics.Debug.Print("Finalizer of {0} called.", GetType());
            Dispose();
        }
#endif
        #endregion

        #region Worker Method
        /// <summary>
        /// Used by the worker thread to read item data.
        /// </summary>
        private void DoWork()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            while (!Stopping)
            {
                CacheItem item = null;

                try
                {
                    lock (lockObject)
                    {
                        // Wait until we have items waiting to be cached
                        if (toCache.Count == 0)
                            Monitor.Wait(lockObject);

                        sw.Start();
                        // Get an item from the queue
                        if (toCache.Count != 0)
                        {
                            item = toCache.Dequeue();

                            // Is it being edited?
                            if (editCache.ContainsKey(item.Guid))
                                item = null;
                        }
                    }

                    // Was it fetched by the UI thread in the meantime?
                    bool isDirty = false;
                    if (item != null)
                    {
                        try
                        {
                            if (mImageListView != null && mImageListView.IsHandleCreated && !mImageListView.IsDisposed)
                            {
                                isDirty = (bool)mImageListView.Invoke(new CheckItemDirtyDelegateInternal(
                                    mImageListView.IsItemDirty), item.Guid);
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            if (!Stopping) throw;
                        }
                        catch (InvalidOperationException)
                        {
                            if (!Stopping) throw;
                        }
                    }
                    if (!isDirty)
                        item = null;

                    // Read file info
                    if (item != null)
                    {
                        if (item.IsVirtualItem)
                        {
                            VirtualItemDetailsEventArgs e = new VirtualItemDetailsEventArgs(item.VirtualItemKey);
                            mImageListView.RetrieveVirtualItemDetailsInternal(e);
                            // Add to cache
                            lock (lockObject)
                            {
                                // Is it already cached?
                                CacheItem cacheItem = null;
                                if (itemCache.TryGetValue(item.Guid, out cacheItem))
                                {
                                    itemCache.Remove(item.Guid);
                                    cacheItem.Dispose();
                                }

                                itemCache.Add(item.Guid, new CacheItem(item.Guid, item.VirtualItemKey, e.SmallIcon, e.LargeIcon));
                            }
                            try
                            {
                                if (mImageListView != null && mImageListView.IsHandleCreated && !mImageListView.IsDisposed)
                                {
                                    // Update item
                                    mImageListView.Invoke(new UpdateVirtualItemDetailsDelegateInternal(
                                        mImageListView.UpdateItemDetailsInternal), item.Guid, e);
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                if (!Stopping) throw;
                            }
                            catch (InvalidOperationException)
                            {
                                if (!Stopping) throw;
                            }
                        }
                        else
                        {
                            // Update file info
                            if (!Stopping)
                            {
                                ShellImageFileInfo info = this.GetImageFileInfo(item.FileName);
                                if (info.Error == null)
                                {
                                    // Add to cache
                                    lock (lockObject)
                                    {
                                        // Is it already cached?
                                        CacheItem cacheItem = null;
                                        if (itemCache.TryGetValue(item.Guid, out cacheItem))
                                        {
                                            itemCache.Remove(item.Guid);
                                            cacheItem.Dispose();
                                        }

                                        itemCache.Add(item.Guid, new CacheItem(item.Guid, item.FileName, info.SmallIcon, info.LargeIcon));
                                    }
                                }
                                else if (mRetryOnError)
                                {
                                    // Retry
                                    lock (lockObject)
                                    {
                                        toCache.Enqueue(item);
                                        CachedShellInfo cachedInfo;
                                        if (!string.IsNullOrEmpty(info.Extension))
                                        {
                                            if (cachedShellInfo.TryGetValue(info.Extension, out cachedInfo))
                                            {
                                                cachedShellInfo.Remove(info.Extension);
                                                cachedInfo.Dispose();
                                            }
                                        }
                                    }
                                }
                                try
                                {
                                    if (mImageListView != null && mImageListView.IsHandleCreated && !mImageListView.IsDisposed)
                                    {
                                        // Update item
                                        mImageListView.Invoke(new UpdateItemDetailsDelegateInternal(
                                            mImageListView.UpdateItemDetailsInternal), item.Guid, info);

                                        // Delegate errors to the parent control
                                        if (info.Error != null)
                                        {
                                            mImageListView.BeginInvoke(new CacheErrorEventHandlerInternal(
                                                mImageListView.OnCacheErrorInternal),
                                                (item == null ? Guid.Empty : item.Guid),
                                                info.Error, CacheThread.Details);
                                        }
                                    }
                                }
                                catch (ObjectDisposedException)
                                {
                                    if (!Stopping) throw;
                                }
                                catch (InvalidOperationException)
                                {
                                    if (!Stopping) throw;
                                }
                            }
                        }
                    }

                    // Check if the cache is exhausted
                    bool queueFull = true;
                    lock (lockObject)
                    {
                        if (toCache.Count == 0)
                            queueFull = false;
                    }

                    // Do we need a refresh?
                    sw.Stop();
                    if (!queueFull || sw.ElapsedMilliseconds > 100)
                    {
                        try
                        {
                            if (mImageListView != null && mImageListView.IsHandleCreated && !mImageListView.IsDisposed)
                            {
                                mImageListView.BeginInvoke(new RefreshDelegateInternal(
                                    mImageListView.OnRefreshInternal));
                            }
                            sw.Reset();
                        }
                        catch (ObjectDisposedException)
                        {
                            if (!Stopping) throw;
                        }
                        catch (InvalidOperationException)
                        {
                            if (!Stopping) throw;
                        }
                    }
                    if (queueFull)
                        sw.Start();
                    else
                    {
                        sw.Stop();
                        sw.Reset();
                    }
                }
                catch (Exception exception)
                {
                    // Delegate the exception to the parent control
                    try
                    {
                        if (mImageListView != null && mImageListView.IsHandleCreated && !mImageListView.IsDisposed)
                        {
                            mImageListView.BeginInvoke(new CacheErrorEventHandlerInternal(
                                mImageListView.OnCacheErrorInternal),
                                (item == null ? Guid.Empty : item.Guid),
                                exception, CacheThread.Details);
                        }
                    }
                    finally
                    {
                        ;
                    }
                }
                finally
                {
                    ;
                }
            }

            lock (lockObject)
            {
                stopped = true;
            }
        }
        #endregion

        #region Utility for Reading Image Details
        /// <summary>
        /// A utility class for reading image details.
        /// </summary>
        internal struct ShellImageFileInfo
        {
            public FileAttributes FileAttributes;
            public Image SmallIcon;
            public Image LargeIcon;
            public DateTime CreationTime;
            public DateTime LastAccessTime;
            public DateTime LastWriteTime;
            public string Extension;
            public string DirectoryName;
            public string DisplayName;
            public long Size;
            public string TypeName;
            public Size Dimensions;
            public SizeF Resolution;
            // Exif tags
            public string ImageDescription;
            public string EquipmentModel;
            public DateTime DateTaken;
            public string Artist;
            public string Copyright;
            public float ExposureTime;
            public float FNumber;
            public ushort ISOSpeed;
            public string UserComment;
            public ushort Rating;
            // Error
            internal Exception Error;
        }

        /// <summary>
        /// Gets image details for the given file.
        /// </summary>
        /// <param name="path">The path to an image file.</param>
        internal ShellImageFileInfo GetImageFileInfo(string path)
        {
            ShellImageFileInfo imageInfo = new ShellImageFileInfo();
            try
            {
                // Read file properties
                FileInfo info = new FileInfo(path);
                imageInfo.FileAttributes = info.Attributes;
                imageInfo.CreationTime = info.CreationTime;
                imageInfo.LastAccessTime = info.LastAccessTime;
                imageInfo.LastWriteTime = info.LastWriteTime;
                imageInfo.Size = info.Length;
                imageInfo.DirectoryName = info.DirectoryName;
                imageInfo.DisplayName = info.Name;
                imageInfo.Extension = info.Extension;

                // Read shell properties
                CachedShellInfo shellInfo = null;
                bool fileTypeCached = false;
                lock (lockObject)
                {
                    fileTypeCached = cachedShellInfo.TryGetValue(imageInfo.Extension, out shellInfo);
                }
                if (!fileTypeCached)
                {
                    ShellInfoExtractor shellEx = ShellInfoExtractor.FromFile(info.Extension);
                    shellInfo = new CachedShellInfo(shellEx.FileType, shellEx.SmallIcon, shellEx.LargeIcon);
                    if (!string.IsNullOrEmpty(info.Extension))
                    {
                        lock (lockObject)
                        {
                            if (!cachedShellInfo.ContainsKey(info.Extension))
                                cachedShellInfo.Add(info.Extension, shellInfo);
                        }
                    }
                    if (shellEx.Error != null)
                        imageInfo.Error = shellEx.Error;
                }
                imageInfo.TypeName = shellInfo.FileType;
                if (shellInfo.SmallIcon != null)
                    imageInfo.SmallIcon = (Image)shellInfo.SmallIcon.Clone();
                if (shellInfo.LargeIcon != null)
                    imageInfo.LargeIcon = (Image)shellInfo.LargeIcon.Clone();

                // Get metadata
                MetadataExtractor metadata = MetadataExtractor.FromFile(path);
                imageInfo.Dimensions = new Size(metadata.Width, metadata.Height);
                imageInfo.Resolution = new SizeF((float)metadata.DPIX, (float)metadata.DPIY);
                imageInfo.ImageDescription = metadata.ImageDescription ?? "";
                imageInfo.EquipmentModel = metadata.EquipmentModel ?? "";
                imageInfo.DateTaken = metadata.DateTaken;
                imageInfo.Artist = metadata.Artist ?? "";
                imageInfo.Copyright = metadata.Copyright ?? "";
                imageInfo.ExposureTime = (float)metadata.ExposureTime;
                imageInfo.FNumber = (float)metadata.FNumber;
                imageInfo.ISOSpeed = (ushort)metadata.ISOSpeed;
                imageInfo.UserComment = metadata.Comment ?? "";
                imageInfo.Rating = (ushort)(metadata.Rating);
                if (metadata.Error != null)
                    imageInfo.Error = metadata.Error;
            }
            catch (Exception e)
            {
                imageInfo.Error = e;
            }
            return imageInfo;
        }
        #endregion
    }
}
