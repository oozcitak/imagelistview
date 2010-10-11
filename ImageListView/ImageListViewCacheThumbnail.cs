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
using System.Drawing;
using System.Threading;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the cache manager responsible for asynchronously loading
    /// item thumbnails.
    /// </summary>
    internal class ImageListViewCacheThumbnail : IDisposable
    {
        #region Member Variables
        private QueuedBackgroundWorker bw;
        private SynchronizationContext context;
        private SendOrPostCallback checkProcessingCallback;

        private ImageListView mImageListView;

        private Dictionary<Guid, CacheItem> thumbCache;
        private Dictionary<Guid, bool> processing;
        private Guid processingRendererItem;
        private Guid processingGalleryItem;
        private Dictionary<Guid, bool> editCache;
        private CacheItem rendererItem;
        private CacheItem galleryItem;

        private List<Guid> removedItems;

        private bool disposed;
        #endregion

        #region CacheItem Class
        /// <summary>
        /// Represents an item in the thumbnail cache.
        /// </summary>
        private class CacheItem : IDisposable
        {
            private bool disposed;

            /// <summary>
            /// Gets the guid of the item.
            /// </summary>
            public Guid Guid { get; private set; }
            /// <summary>
            /// Gets the name of the image file.
            /// </summary>
            public string FileName { get; private set; }
            /// <summary>
            /// Gets the size of the requested thumbnail.
            /// </summary>
            public Size Size { get; private set; }
            /// <summary>
            /// Gets the cached image.
            /// </summary>
            public Image Image { get; private set; }
            /// <summary>
            /// Gets or sets the state of the cache item.
            /// </summary>
            public CacheState State { get; set; }
            /// <summary>
            /// Gets embedded thumbnail extraction behavior.
            /// </summary>
            public UseEmbeddedThumbnails UseEmbeddedThumbnails { get; private set; }
            /// <summary>
            /// Gets Exif rotation behavior.
            /// </summary>
            public bool AutoRotate { get; private set; }
            /// <summary>
            /// Gets whether Windows Imaging Component will be used.
            /// </summary>
            public bool UseWIC { get; private set; }
            /// <summary>
            /// Gets whether this item represents a virtual ImageListViewItem.
            /// </summary>
            public bool IsVirtualItem { get; private set; }
            /// <summary>
            /// Gets the public key for the virtual item.
            /// </summary>
            public object VirtualItemKey { get; private set; }
            /// <summary>
            /// Gets or sets whether this is a renderer request.
            /// </summary>
            public bool IsRendererRequest { get; set; }
            /// <summary>
            /// Gets or sets whether this is a gallery request.
            /// </summary>
            public bool IsGalleryRequest { get; set; }

            /// <summary>
            /// Initializes a new instance of the CacheItem class
            /// for use with a virtual item.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="key">The public key for the virtual item.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            public CacheItem(Guid guid, object key, Size size, Image image, CacheState state)
                : this(guid, key, size, image, state, UseEmbeddedThumbnails.Auto, true, true)
            {
                ;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class
            /// for use with a virtual item.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="key">The public key for the virtual item.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
            /// <param name="autoRotate">AutoRotate property of the owner control.</param>
            /// <param name="useWIC">Whether to use WIC.</param>
            public CacheItem(Guid guid, object key, Size size, Image image, CacheState state, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
            {
                Guid = guid;
                VirtualItemKey = key;
                FileName = string.Empty;
                Size = size;
                Image = image;
                State = state;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
                UseWIC = useWIC;
                IsVirtualItem = true;
                disposed = false;

                IsRendererRequest = false;
                IsGalleryRequest = false;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="filename">The file system path to the image file.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            public CacheItem(Guid guid, string filename, Size size, Image image, CacheState state)
                : this(guid, filename, size, image, state, UseEmbeddedThumbnails.Auto, true, true)
            {
                ;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="filename">The file system path to the image file.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
            /// <param name="autoRotate">AutoRotate property of the owner control.</param>
            /// <param name="useWIC">Whether to use WIC.</param>
            public CacheItem(Guid guid, string filename, Size size, Image image, CacheState state, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
            {
                Guid = guid;
                FileName = filename;
                Size = size;
                Image = image;
                State = state;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
                UseWIC = useWIC;
                IsVirtualItem = false;
                disposed = false;

                IsRendererRequest = false;
                IsGalleryRequest = false;
            }

            /// <summary>
            /// Performs application-defined tasks associated with 
            /// freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (!disposed)
                {
                    if (Image != null)
                    {
                        Image.Dispose();
                        Image = null;
                    }

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
                if (Image != null)
                    System.Diagnostics.Debug.Print("Finalizer of {0} called for non-empty cache item.", GetType());
                Dispose();
            }
#endif
        }
        #endregion

        #region CanContinueProcessingEventArgs
        /// <summary>
        /// Represents the event arguments for the <see cref="CanContinueProcessing"/> callback.
        /// </summary>
        private class CanContinueProcessingEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the guid of the request.
            /// </summary>
            public Guid Guid { get; private set; }
            /// <summary>
            /// Gets whether this is a renderer request.
            /// </summary>
            public bool IsRendererRequest { get; private set; }
            /// <summary>
            /// Gets whether this is a gallery request.
            /// </summary>
            public bool IsGalleryRequest { get; private set; }
            /// <summary>
            /// Gets whether this item should be processed.
            /// </summary>
            public bool ContinueProcessing { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CanContinueProcessingEventArgs"/> class.
            /// </summary>
            /// <param name="guid">The guid of the request.</param>
            /// <param name="isRendererRequest">true if is a renderer request; otherwise false.</param>
            /// <param name="isGalleryRequest">true if is a gallery request; otherwise false.</param>
            public CanContinueProcessingEventArgs(Guid guid, bool isRendererRequest, bool isGalleryRequest)
            {
                Guid = guid;
                IsRendererRequest = isRendererRequest;
                IsGalleryRequest = isGalleryRequest;
                ContinueProcessing = true;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether the cache manager retries loading items on errors.
        /// </summary>
        public bool RetryOnError { get; internal set; }
        /// <summary>
        /// Gets or sets the cache mode.
        /// </summary>
        public CacheMode CacheMode { get; internal set; }
        /// <summary>
        /// Gets or sets the cache limit as count of items.
        /// </summary>
        public int CacheLimitAsItemCount { get; internal set; }
        /// <summary>
        /// Gets or sets the cache limit as allocated memory in MB.
        /// </summary>
        public long CacheLimitAsMemory { get; internal set; }
        /// <summary>
        /// Gets the approximate amount of memory used by the cache.
        /// </summary>
        public long MemoryUsed { get; private set; }
        /// <summary>
        /// Gets the approximate amount of memory used by removed items in the cache.
        /// This memory can be reclaimed by calling <see cref="Purge()"/>.
        /// </summary>
        public long MemoryUsedByRemoved { get; private set; }
        /// <summary>
        /// Returns the count of items in the cache.
        /// </summary>
        public long CacheSize { get { return thumbCache.Count; } }
        /// <summary>
        /// Gets or sets the current thumbnail size.
        /// </summary>
        public Size CurrentThumbnailSize { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListViewCacheThumbnail"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public ImageListViewCacheThumbnail(ImageListView owner)
        {
            context = null;
            bw = new QueuedBackgroundWorker();
            bw.ProcessingMode = ProcessingMode.LIFO;
            bw.IsBackground = true;
            bw.DoWork += bw_DoWork;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;

            checkProcessingCallback = new SendOrPostCallback(CanContinueProcessing);

            mImageListView = owner;
            CacheMode = CacheMode.OnDemand;
            CacheLimitAsItemCount = 0;
            CacheLimitAsMemory = 20 * 1024 * 1024;
            RetryOnError = false;

            thumbCache = new Dictionary<Guid, CacheItem>();
            editCache = new Dictionary<Guid, bool>();
            processing = new Dictionary<Guid, bool>();
            processingRendererItem = Guid.Empty;
            processingGalleryItem = Guid.Empty;

            rendererItem = null;
            galleryItem = null;

            MemoryUsed = 0;
            MemoryUsedByRemoved = 0;
            removedItems = new List<Guid>();

            disposed = false;
        }
        #endregion

        #region Context Callbacks
        /// <summary>
        /// Determines if the item should be processed.
        /// </summary>
        /// <param name="item">The <see cref="CacheItem"/> to check.</param>
        /// <returns>true if the item should be processed; otherwise false.</returns>
        private bool OnCanContinueProcessing(CacheItem item)
        {
            CanContinueProcessingEventArgs arg = new CanContinueProcessingEventArgs(
                item.Guid, item.IsRendererRequest, item.IsGalleryRequest);
            context.Send(checkProcessingCallback, arg);
            return arg.ContinueProcessing;
        }
        /// <summary>
        /// Determines if the item should be processed.
        /// </summary>
        /// <param name="argument">The event argument.</param>
        /// <returns>true if the item should be processed; otherwise false.</returns>
        private void CanContinueProcessing(object argument)
        {
            CanContinueProcessingEventArgs arg = argument as CanContinueProcessingEventArgs;
            bool canProcess = true;

            // Is it already cached?
            if (canProcess && !arg.IsRendererRequest && !arg.IsGalleryRequest)
            {
                CacheItem existing = null;
                thumbCache.TryGetValue(arg.Guid, out existing);
                if (existing != null && existing.Size == CurrentThumbnailSize)
                    canProcess = false;
            }

            // Is it in the edit cache?
            if (canProcess)
            {
                if (editCache.ContainsKey(arg.Guid))
                    canProcess = false;
            }

            // Is it outside the visible area?
            if (canProcess && !arg.IsRendererRequest && !arg.IsGalleryRequest && (CacheMode == CacheMode.OnDemand))
            {
                if (mImageListView != null && !mImageListView.IsItemVisible(arg.Guid))
                    canProcess = false;
            }

            arg.ContinueProcessing = canProcess;
        }
        #endregion

        #region QueuedBackgroundWorker Events
        /// <summary>
        /// Handles the RunWorkerCompleted event of the queued background worker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Manina.Windows.Forms.QueuedWorkerCompletedEventArgs"/> 
        /// instance containing the event data.</param>
        void bw_RunWorkerCompleted(object sender, QueuedWorkerCompletedEventArgs e)
        {
            CacheItem request = e.UserState as CacheItem;
            CacheItem result = e.Result as CacheItem;

            // We are done processing
            if (request.IsRendererRequest)
                processingRendererItem = Guid.Empty;
            else if (request.IsGalleryRequest)
                processingGalleryItem = Guid.Empty;
            else
                processing.Remove(request.Guid);

            // Do not process the result if the cache operation
            // was cancelled.
            if (e.Cancelled)
                return;

            // Dispose old item and add to cache
            if (request.IsRendererRequest)
            {
                if (rendererItem != null)
                    rendererItem.Dispose();

                rendererItem = result;
            }
            else if (request.IsGalleryRequest)
            {
                if (galleryItem != null)
                    galleryItem.Dispose();

                galleryItem = result;
            }
            else if (result != null)
            {
                CacheItem existing = null;
                if (thumbCache.TryGetValue(result.Guid, out existing))
                {
                    existing.Dispose();
                    thumbCache.Remove(result.Guid);
                }
                thumbCache.Add(result.Guid, result);

                if (result.Image != null)
                {
                    // Did the thumbnail size change while we were
                    // creating the thumbnail?                                    
                    if (result.Size != CurrentThumbnailSize)
                        result.State = CacheState.Unknown;

                    // Purge invisible items if we exceeded the cache limit?
                    MemoryUsed += GetImageMemorySize(result.Image);
                    if (IsCacheLimitExceeded())
                        PurgeInvisible(true);
                }
            }
            
            // Refresh the control lazily
            if (result != null && result.Image != null && mImageListView != null && mImageListView.IsItemVisible(request.Guid))
                mImageListView.Refresh(false, true);
            
            // Raise the ThumbnailCached event
            if (mImageListView != null)
                mImageListView.OnThumbnailCachedInternal(request.Guid, result.Image, request.Size, e.Error != null);
            
            // Raise the CacheError event
            if (e.Error != null && mImageListView != null)
                mImageListView.OnCacheErrorInternal(request.Guid, e.Error, CacheThread.Thumbnail);            
        }
        /// <summary>
        /// Handles the DoWork event of the queued background worker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Manina.Windows.Forms.QueuedWorkerDoWorkEventArgs"/> instance 
        /// containing the event data.</param>
        void bw_DoWork(object sender, QueuedWorkerDoWorkEventArgs e)
        {
            CacheItem request = e.Argument as CacheItem;

            // Should we continue processing this item?
            // The callback checks the following and returns false if
            //   the item is already cached -OR-
            //   the item is in the edit cache -OR-
            //   the item is outside the visible area (only if the CacheMode is OnDemand).
            if (!OnCanContinueProcessing(request))
            {
                e.Cancel = true;
                return;
            }

            // Read the thumbnail image
            Image thumb = null;
            if (request.IsVirtualItem)
            {
                // Ask the control for the virtual item thumbnail
                VirtualItemThumbnailEventArgs arg = new VirtualItemThumbnailEventArgs(
                    request.VirtualItemKey, request.Size);
                if (mImageListView != null && mImageListView.IsHandleCreated && !mImageListView.IsDisposed)
                    mImageListView.RetrieveVirtualItemThumbnailInternal(arg);
                if (arg.ThumbnailImage != null)
                    thumb = arg.ThumbnailImage;
            }
            else
            {
                // Extract the thumbnail from the source image.
                thumb = ThumbnailExtractor.FromFile(request.FileName,
                    request.Size, request.UseEmbeddedThumbnails, request.AutoRotate, request.UseWIC);
            }

            // Return the thumbnail
            CacheItem result = null;
            if (thumb == null && !RetryOnError)
            {
                result = new CacheItem(request.Guid, request.FileName,
                    request.Size, null, CacheState.Error,
                    request.UseEmbeddedThumbnails, request.AutoRotate, request.UseWIC);
            }
            else
            {
                result = new CacheItem(request.Guid, request.FileName,
                    request.Size, thumb, CacheState.Cached,
                    request.UseEmbeddedThumbnails, request.AutoRotate, request.UseWIC);
            }

            e.Result = result;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Starts editing an item. While items are edited,
        /// the cache thread will not work on them to prevent collisions.
        /// </summary>
        /// <param name="guid">The guid representing the item</param>
        public void BeginItemEdit(Guid guid)
        {
            if (!editCache.ContainsKey(guid))
            {
                editCache.Add(guid, false);
            }
        }
        /// <summary>
        /// Ends editing an item. After this call, item
        /// image will be continued to be fetched by the thread.
        /// </summary>
        /// <param name="guid">The guid representing the item.</param>
        public void EndItemEdit(Guid guid)
        {
            editCache.Remove(guid);
        }
        /// <summary>
        /// Gets the cache state of the specified item.
        /// </summary>
        /// <param name="guid">The guid representing the item.</param>
        public CacheState GetCacheState(Guid guid)
        {
            CacheItem item = null;
            if (thumbCache.TryGetValue(guid, out item))
                return item.State;

            return CacheState.Unknown;
        }
        /// <summary>
        /// Rebuilds the thumbnail cache.
        /// Old thumbnails will be kept until they are overwritten
        /// by new ones.
        /// </summary>
        public void Rebuild()
        {
            foreach (CacheItem item in thumbCache.Values)
                item.State = CacheState.Unknown;
        }
        /// <summary>
        /// Clears the thumbnail cache.
        /// </summary>
        public void Clear()
        {
            foreach (CacheItem item in thumbCache.Values)
                item.Dispose();
            thumbCache.Clear();

            if (rendererItem != null)
                rendererItem.Dispose();
            rendererItem = null;

            bw.CancelAsync();

            MemoryUsed = 0;
            MemoryUsedByRemoved = 0;
            removedItems.Clear();
            processing.Clear();
            processingRendererItem = Guid.Empty;
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
            CacheItem item = null;
            if (!thumbCache.TryGetValue(guid, out item))
                return;

            if (removeNow)
            {
                MemoryUsed -= GetImageMemorySize(item.Size.Width, item.Size.Height);
                item.Dispose();
                thumbCache.Remove(guid);
            }
            else
            {
                MemoryUsedByRemoved += GetImageMemorySize(item.Size.Width, item.Size.Height);
                removedItems.Add(guid);

                Purge();
            }
        }
        /// <summary>
        /// Purges removed items from the cache.
        /// </summary>
        /// <param name="force">true to purge the cache now, regardless of
        /// memory usage; otherwise false to automatically purge the cache
        /// depending on memory usage.</param>
        public void Purge(bool force)
        {
            // Remove items now if we can free more than 25% of the cache limit
            if (force || IsPurgeNeeded())
            {
                foreach (Guid guid in removedItems)
                {
                    CacheItem item = null;
                    if (thumbCache.TryGetValue(guid, out item))
                    {
                        item.Dispose();
                        thumbCache.Remove(guid);
                    }
                }
                removedItems.Clear();
                MemoryUsed -= MemoryUsedByRemoved;
                MemoryUsedByRemoved = 0;
            }
        }
        /// <summary>
        /// Purges removed items from the cache automatically
        /// depending on memory usage.
        /// </summary>
        public void Purge()
        {
            Purge(false);
        }
        /// <summary>
        /// Purges invisible items from the cache.
        /// </summary>
        /// <param name="force">true to purge the cache now, regardless of
        /// memory usage; otherwise false to automatically purge the cache
        /// depending on memory usage.</param>
        public void PurgeInvisible(bool force)
        {
            if (mImageListView == null)
                return;

            Dictionary<Guid, bool> visible = mImageListView.GetVisibleItems();

            if (visible.Count == 0)
                return;

            foreach (KeyValuePair<Guid, CacheItem> item in thumbCache)
            {
                if (!visible.ContainsKey(item.Key))
                {
                    removedItems.Add(item.Key);
                    MemoryUsedByRemoved += GetImageMemorySize(item.Value.Image);
                }
            }

            Purge(force);
        }
        /// <summary>
        /// Determines if removed items need to be purged. Removed items are purged
        /// if they take up more than 25% of the cache limit.
        /// </summary>
        /// <returns>true if removed items need to be purged; otherwise false.</returns>
        private bool IsPurgeNeeded()
        {
            return ((CacheLimitAsMemory != 0 && MemoryUsedByRemoved > CacheLimitAsMemory / 4) ||
                (CacheLimitAsItemCount != 0 && removedItems.Count > CacheLimitAsItemCount / 4));
        }
        /// <summary>
        /// Determines if the cache limit is exceeded.
        /// </summary>
        /// <returns>true if the cache limit is exceeded; otherwise false.</returns>
        private bool IsCacheLimitExceeded()
        {
            return ((CacheLimitAsMemory != 0 && MemoryUsedByRemoved > CacheLimitAsMemory) ||
                (CacheLimitAsItemCount != 0 && removedItems.Count > CacheLimitAsItemCount));
        }
        /// <summary>
        /// Returns the memory usage of an image.
        /// </summary>
        /// <param name="image">A image.</param>
        /// <returns>Memory size of the image.</returns>
        private int GetImageMemorySize(Image image)
        {
            if (image != null)
                return GetImageMemorySize(image.Width, image.Height);
            else
                return 0;
        }
        /// <summary>
        /// Returns the memory usage of an image in of given dimensions.
        /// The value is calculated aproximately as (Width * Height * BitsPerPixel / 8)
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <returns>Memory size of the image.</returns>
        private int GetImageMemorySize(int width, int height)
        {
            return width * height * 24 / 8;
        }
        /// <summary>
        /// Adds the image to the cache queue.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="filename">Filesystem path to the image file.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void Add(Guid guid, string filename, Size thumbSize,
            UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            CacheItem item = null;
            if (thumbCache.TryGetValue(guid, out item))
            {
                if (item.Size == thumbSize && item.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                    return;
            }

            // Already being processed?
            if (processing.ContainsKey(guid))
                return;

            // Add to cache queue
            RunWorker(new CacheItem(guid, filename,
                thumbSize, null, CacheState.Unknown,
                useEmbeddedThumbnails, autoRotate, useWIC));
        }
        /// <summary>
        /// Adds the image to the cache queue.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="filename">Filesystem path to the image file.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="thumb">Thumbnail image to add to cache.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void Add(Guid guid, string filename, Size thumbSize, Image thumb,
            UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            CacheItem item = null;
            if (thumbCache.TryGetValue(guid, out item))
            {
                if (item.Size == thumbSize && item.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                    return;
            }
            // Add to cache
            thumbCache.Add(guid, new CacheItem(guid, filename, thumbSize,
                ThumbnailExtractor.FromImage(thumb, thumbSize, useEmbeddedThumbnails, autoRotate, useWIC),
                CacheState.Cached, useEmbeddedThumbnails, autoRotate, useWIC));

            if (mImageListView != null)
            {
                mImageListView.OnThumbnailCachedInternal(guid, thumb, thumbSize, false);
                mImageListView.OnRefreshInternal();
            }
        }
        /// <summary>
        /// Adds a virtual item to the cache queue.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="key">The key of this item.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void Add(Guid guid, object key, Size thumbSize,
            UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            CacheItem item = null;
            if (thumbCache.TryGetValue(guid, out item))
            {
                if (item.Size == thumbSize && item.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                    return;
            }

            // Add to cache queue
            RunWorker(new CacheItem(guid, key, thumbSize, null,
                CacheState.Unknown, useEmbeddedThumbnails, autoRotate, useWIC));
        }
        /// <summary>
        /// Adds a virtual item to the cache.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="key">The key of this item.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="thumb">Thumbnail image to add to cache.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void Add(Guid guid, object key, Size thumbSize, Image thumb,
            UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            CacheItem item = null;
            if (thumbCache.TryGetValue(guid, out item))
            {
                if (item.Size == thumbSize && item.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                    return;
            }

            // Add to cache
            thumbCache.Add(guid, new CacheItem(guid, key, thumbSize,
                ThumbnailExtractor.FromImage(thumb, thumbSize, useEmbeddedThumbnails, autoRotate, useWIC),
                CacheState.Cached, useEmbeddedThumbnails, autoRotate, useWIC));

            // Raise the cache events
            if (mImageListView != null)
            {
                mImageListView.OnThumbnailCachedInternal(guid, thumb, thumbSize, false);
                mImageListView.OnRefreshInternal();
            }
        }
        /// <summary>
        /// Adds the image to the gallery cache queue.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="filename">Filesystem path to the image file.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void AddToGalleryCache(Guid guid, string filename,
            Size thumbSize, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            if (galleryItem != null && galleryItem.Guid == guid &&
                galleryItem.Size == thumbSize &&
                galleryItem.UseEmbeddedThumbnails == useEmbeddedThumbnails &&
                galleryItem.UseWIC == useWIC)
                return;

            // Add to cache queue
            RunWorker(new CacheItem(guid, filename,
                thumbSize, null, CacheState.Unknown, useEmbeddedThumbnails, autoRotate, useWIC), 2);
        }
        /// <summary>
        /// Adds the virtual item image to the gallery cache queue.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="key">The key of this item.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void AddToGalleryCache(Guid guid, object key, Size thumbSize,
            UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            if (galleryItem != null && galleryItem.Guid == guid &&
                galleryItem.Size == thumbSize &&
                galleryItem.UseEmbeddedThumbnails == useEmbeddedThumbnails &&
                galleryItem.UseWIC == useWIC)
                return;

            // Add to cache queue
            RunWorker(new CacheItem(guid, key, thumbSize,
                null, CacheState.Unknown, useEmbeddedThumbnails, autoRotate, useWIC), 2);
        }
        /// <summary>
        /// Adds the image to the renderer cache queue.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="filename">Filesystem path to the image file.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void AddToRendererCache(Guid guid, string filename,
            Size thumbSize, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            if (rendererItem != null && rendererItem.Guid == guid &&
                rendererItem.Size == thumbSize &&
                rendererItem.UseEmbeddedThumbnails == useEmbeddedThumbnails &&
                rendererItem.UseWIC == useWIC)
                return;

            // Add to cache queue
            RunWorker(new CacheItem(guid, filename,
                thumbSize, null, CacheState.Unknown, useEmbeddedThumbnails, autoRotate, useWIC), 1);
        }
        /// <summary>
        /// Adds the virtual item image to the renderer cache queue.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="key">The key of this item.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        /// <param name="autoRotate">AutoRotate property of the owner control.</param>
        /// <param name="useWIC">Whether to use WIC.</param>
        public void AddToRendererCache(Guid guid, object key, Size thumbSize,
            UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool useWIC)
        {
            // Already cached?
            if (rendererItem != null && rendererItem.Guid == guid &&
                rendererItem.Size == thumbSize &&
                rendererItem.UseEmbeddedThumbnails == useEmbeddedThumbnails &&
                rendererItem.UseWIC == useWIC)
                return;

            // Add to cache queue
            RunWorker(new CacheItem(guid, key, thumbSize,
                null, CacheState.Unknown, useEmbeddedThumbnails, autoRotate, useWIC), 1);
        }
        /// <summary>
        /// Gets the image from the renderer cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        public Image GetRendererImage(Guid guid, Size thumbSize,
            UseEmbeddedThumbnails useEmbeddedThumbnails)
        {
            if (rendererItem != null && rendererItem.Guid == guid &&
                rendererItem.Size == thumbSize &&
                rendererItem.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                return rendererItem.Image;
            else
                return null;
        }
        /// <summary>
        /// Gets the image from the gallery cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        /// <param name="thumbSize">Requested thumbnail size.</param>
        /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
        public Image GetGalleryImage(Guid guid, Size thumbSize,
            UseEmbeddedThumbnails useEmbeddedThumbnails)
        {
            if (galleryItem != null && galleryItem.Guid == guid &&
                galleryItem.Size == thumbSize &&
                galleryItem.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                return galleryItem.Image;
            else
                return null;
        }
        /// <summary>
        /// Gets the image from the thumbnail cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        /// <param name="guid">The guid representing this item.</param>
        public Image GetImage(Guid guid)
        {
            CacheItem item = null;
            if (thumbCache.TryGetValue(guid, out item))
                return item.Image;
            else
                return null;
        }
        #endregion

        #region RunWorker
        /// <summary>
        /// Pushes the given item to the worker queue. Items with high priority are renderer 
        /// or gallery items, ie. large images in gallery and pane views and images requested 
        /// by custom renderers. Items with 0 priority are regular thumbnails.
        /// </summary>
        /// <param name="item">The item to add to the worker queue.</param>
        /// <param name="priority">Priority of the item in the queue.</param>
        private void RunWorker(CacheItem item, int priority)
        {
            // Get the current synchronization context
            if (context == null)
                context = SynchronizationContext.Current;

            // Already being processed?
            if (priority == 0)
            {
                if (processing.ContainsKey(item.Guid))
                    return;
                else
                    processing.Add(item.Guid, false);
            }
            else if (priority == 1)
            {
                if (processingRendererItem == item.Guid)
                    return;
                else
                {
                    bw.CancelAsync(priority);
                    processingRendererItem = item.Guid;
                    item.IsRendererRequest = true;
                }
            }
            else if (priority == 2)
            {
                if (processingGalleryItem == item.Guid)
                    return;
                else
                {
                    bw.CancelAsync(priority);
                    processingGalleryItem = item.Guid;
                    item.IsGalleryRequest = true;
                }
            }

            // Raise the ThumbnailCaching event
            if (mImageListView != null)
                mImageListView.OnThumbnailCachingInternal(item.Guid, item.Size);

            // Add the item to the queue for processing
            bw.RunWorkerAsync(item, priority);
        }
        /// <summary>
        /// Pushes the given item to the worker queue.
        /// </summary>
        /// <param name="item">The item to add to the worker queue.</param>
        private void RunWorker(CacheItem item)
        {
            RunWorker(item, 0);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                bw.DoWork -= bw_DoWork;
                bw.RunWorkerCompleted -= bw_RunWorkerCompleted;

                Clear();
                bw.Dispose();

                disposed = true;

                GC.SuppressFinalize(this);
            }
        }
#if DEBUG
        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// ImageListViewCacheManager is reclaimed by garbage collection.
        /// </summary>
        ~ImageListViewCacheThumbnail()
        {
            System.Diagnostics.Debug.Print("Finalizer of {0} called.", GetType());
            Dispose();
        }
#endif
        #endregion
    }
}
