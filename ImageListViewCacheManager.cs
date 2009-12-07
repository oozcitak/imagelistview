using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the cache manager responsible for asynchronously loading
    /// item thumbnails.
    /// </summary>
    internal class ImageListViewCacheManager : IDisposable
    {
        #region Constants
        private const int PropertyTagThumbnailData = 0x501B;
        private const int PropertyTagThumbnailImageWidth = 0x5020;
        private const int PropertyTagThumbnailImageHeight = 0x5021;
        private const float EmbeddedThumbnailSizeTolerance = 1.2f;
        #endregion

        #region Member Variables
        private readonly object lockObject;

        private ImageListView mImageListView;
        private Thread mThread;
        private int mCacheLimitAsItemCount;
        private long mCacheLimitAsMemory;

        private Dictionary<Guid, CacheItem> toCache;
        private Dictionary<Guid, CacheItem> thumbCache;
        private Dictionary<Guid, Image> editCache;

        private long memoryUsed;
        private long memoryUsedByRemoved;
        private List<Guid> removedItems;

        private bool stopping;
        private bool disposed;
        #endregion

        #region Private Classes
        /// <summary>
        /// Represents an item in the thumbnail cache.
        /// </summary>
        private class CacheItem : IDisposable
        {
            private string mFileName;
            private Size mSize;
            private Image mImage;
            private CacheState mState;
            private UseEmbeddedThumbnails mUseEmbeddedThumbnails;
            private bool disposed;

            /// <summary>
            /// Gets the name of the image file.
            /// </summary>
            public string FileName { get { return mFileName; } }
            /// <summary>
            /// Gets the size of the requested thumbnail.
            /// </summary>
            public Size Size { get { return mSize; } }
            /// <summary>
            /// Gets the cached image.
            /// </summary>
            public Image Image { get { return mImage; } }
            /// <summary>
            /// Gets the state of the cache item.
            /// </summary>
            public CacheState State { get { return mState; } }
            /// <summary>
            /// Gets embedded thumbnail extraction behavior.
            /// </summary>
            public UseEmbeddedThumbnails UseEmbeddedThumbnails { get { return mUseEmbeddedThumbnails; } }

            public CacheItem(string filename, Size size, Image image, CacheState state)
            {
                mFileName = filename;
                mSize = size;
                mImage = image;
                mState = state;
                disposed = false;
            }

            public CacheItem(string filename, Size size, Image image, CacheState state, UseEmbeddedThumbnails useEmbeddedThumbnails)
                : this(filename, size, image, state)
            {
                mUseEmbeddedThumbnails = useEmbeddedThumbnails;
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    if (mImage != null)
                    {
                        mImage.Dispose();
                        mImage = null;
                    }
                    disposed = true;
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the cache limit as count of items.
        /// </summary>
        public int CacheLimitAsItemCount
        {
            get { return mCacheLimitAsItemCount; }
            set { lock (lockObject) { mCacheLimitAsItemCount = value; mCacheLimitAsMemory = 0; } }
        }
        /// <summary>
        /// Gets or sets the cache limit as allocated memory in MB.
        /// </summary>
        public long CacheLimitAsMemory
        {
            get { return mCacheLimitAsMemory; }
            set { lock (lockObject) { mCacheLimitAsMemory = value; mCacheLimitAsItemCount = 0; } }
        }
        /// <summary>
        /// Gets the approximate amount of memory used by the cache.
        /// </summary>
        public long MemoryUsed { get { lock (lockObject) { return memoryUsed; } } }
        /// <summary>
        /// Returns the count of items in the cache.
        /// Requires a cache lock, use sparingly.
        /// </summary>
        public long CacheSize { get { lock (lockObject) { return thumbCache.Count; } } }
        #endregion

        #region Constructor
        public ImageListViewCacheManager(ImageListView owner)
        {
            lockObject = new object();

            mImageListView = owner;
            mCacheLimitAsItemCount = 0;
            mCacheLimitAsMemory = 10;

            toCache = new Dictionary<Guid, CacheItem>();
            thumbCache = new Dictionary<Guid, CacheItem>();
            editCache = new Dictionary<Guid, Image>();

            memoryUsed = 0;
            memoryUsedByRemoved = 0;
            removedItems = new List<Guid>();

            mThread = new Thread(new ThreadStart(DoWork));
            mThread.IsBackground = true;

            stopping = false;
            disposed = false;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Starts editing an item. While items are edited,
        /// their original images will be seperately cached
        /// instead of fetching them from the file.
        /// </summary>
        /// <param name="guid">The GUID of the item</param>
        /// <param name="filename">The image filename.</param>
        public void BeginItemEdit(Guid guid, string filename)
        {
            lock (lockObject)
            {
                using (Image img = Image.FromFile(filename))
                {
                    editCache.Add(guid, new Bitmap(img));
                }
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
                    editCache[guid].Dispose();
                    editCache.Remove(guid);
                }
            }
        }
        /// <summary>
        /// Gets the cache state of the specified item.
        /// </summary>
        public CacheState GetCacheState(Guid guid)
        {
            lock (lockObject)
            {
                CacheItem item = null;
                if (thumbCache.TryGetValue(guid, out item))
                    return item.State;
                else if (toCache.TryGetValue(guid, out item))
                    return item.State;
            }

            return CacheState.Unknown;
        }
        /// <summary>
        /// Starts the thumbnail generator thread.
        /// </summary>
        public void Start()
        {
            mThread.Start();
            while (!mThread.IsAlive) ;
        }
        /// <summary>
        /// Stops the thumbnail generator thread.
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
            mThread.Join();
        }
        /// <summary>
        /// Clears the thumbnail cache.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                foreach (CacheItem item in thumbCache.Values)
                    item.Dispose();
                thumbCache.Clear();

                memoryUsed = 0;
                memoryUsedByRemoved = 0;
                removedItems.Clear();
            }
        }
        /// <summary>
        /// Removes the given item from the cache.
        /// </summary>
        /// <param name="guid">The guid of the item to remove.</param>
        public void Remove(Guid guid)
        {
            lock (lockObject)
            {
                CacheItem item = null;
                if (!thumbCache.TryGetValue(guid, out item))
                    return;

                // Calculate the memory usage (approx. Width * Height * BitsPerPixel / 8)
                memoryUsedByRemoved += item.Size.Width * item.Size.Height * 24 / 8;
                removedItems.Add(guid);

                // Remove items now if we can free more than 25% of the cache limit
                if ((mCacheLimitAsMemory != 0 && memoryUsedByRemoved > mCacheLimitAsMemory / 4) ||
                    (mCacheLimitAsItemCount != 0 && removedItems.Count > mCacheLimitAsItemCount / 4))
                {
                    CacheItem itemToRemove = null;
                    foreach (Guid iguid in removedItems)
                    {
                        if (thumbCache.TryGetValue(iguid, out itemToRemove))
                        {
                            itemToRemove.Dispose();
                            thumbCache.Remove(iguid);
                        }
                    }
                    removedItems.Clear();
                    memoryUsed -= memoryUsedByRemoved;
                    memoryUsedByRemoved = 0;
                }
            }
        }
        /// <summary>
        /// Adds the image to the cache queue.
        /// </summary>
        public void Add(Guid guid, string filename, Size thumbSize, UseEmbeddedThumbnails useEmbeddedThumbnails)
        {
            lock (lockObject)
            {
                // Already cached?
                CacheItem item = null;
                if (thumbCache.TryGetValue(guid, out item))
                {
                    if (item.Size == thumbSize && item.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                        return;
                    else
                        thumbCache.Remove(guid);
                }
                // Add to cache
                if (!toCache.TryGetValue(guid, out item))
                {
                    toCache.Add(guid, new CacheItem(filename, thumbSize, null, CacheState.InQueue, useEmbeddedThumbnails));
                    Monitor.Pulse(lockObject);
                }
            }
        }
        /// <summary>
        /// Gets the image from the thumbnail cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        public Image GetImage(Guid guid)
        {
            lock (lockObject)
            {
                CacheItem item = null;
                if (thumbCache.TryGetValue(guid, out item))
                {
                    return item.Image;
                }
            }
            return null;
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (lockObject)
            {
                if (disposed) return;

                foreach (CacheItem item in thumbCache.Values)
                    item.Dispose();
                thumbCache.Clear();

                foreach (CacheItem item in toCache.Values)
                    item.Dispose();
                toCache.Clear();

                foreach (Image img in editCache.Values)
                    img.Dispose();
                editCache.Clear();

                memoryUsed = 0;
                memoryUsedByRemoved = 0;
                removedItems.Clear();

                disposed = true;
            }
        }

        #endregion

        #region Worker Method
        /// <summary>
        /// Used by the worker thread to generate image thumbnails.
        /// Once a thumbnail image is generated, the item will be redrawn
        /// to replace the placeholder image.
        /// </summary>
        private void DoWork()
        {
            while (true)
            {
                lock (lockObject)
                {
                    if (stopping) return;
                }

                Guid guid = new Guid();
                CacheItem request = null;

                lock (lockObject)
                {
                    // Wait until we have items waiting to be cached
                    if (toCache.Count == 0)
                        Monitor.Wait(lockObject);

                    if (stopping) return;

                    // Get an item from the queue
                    foreach (KeyValuePair<Guid, CacheItem> pair in toCache)
                    {
                        guid = pair.Key;
                        request = pair.Value;
                        break;
                    }
                    toCache.Remove(guid);

                    // Is it already cached?
                    CacheItem existing = null;
                    if (thumbCache.TryGetValue(guid, out existing))
                    {
                        if (existing.Size == request.Size)
                            request = null;
                        else
                            thumbCache.Remove(guid);
                    }
                }

                // Is it outside visible area?
                if (request != null)
                {
                    bool isvisible = (bool)mImageListView.Invoke(new CheckItemVisibleInternal(mImageListView.IsItemVisible), guid);
                    if (!isvisible)
                        request = null;
                }

                // Proceed if we have a filename
                if (request != null)
                {
                    bool thumbnailCreated = false;
                    bool cleanupRequired = false;

                    Image thumb = null;

                    // Is it in the edit cache?
                    lock (lockObject)
                    {
                        Image source = null;
                        if (editCache.TryGetValue(guid, out source))
                            thumb = Utility.ThumbnailFromImage(source, request.Size);
                    }

                    // Read from file
                    if (thumb == null)
                        thumb = Utility.ThumbnailFromFile(request.FileName, request.Size, request.UseEmbeddedThumbnails);

                    lock (lockObject)
                    {
                        if (!thumbCache.ContainsKey(guid) || thumbCache[guid].Size != request.Size)
                        {
                            if (thumbCache.ContainsKey(guid))
                            {
                                thumbCache[guid].Dispose();
                                thumbCache.Remove(guid);
                            }
                            if (thumb == null)
                                thumbCache.Add(guid, new CacheItem(request.FileName, request.Size, null, CacheState.Error));
                            else
                                thumbCache.Add(guid, new CacheItem(request.FileName, request.Size, thumb, CacheState.Cached));
                            thumbnailCreated = true;

                            // Did we exceed the cache limit?
                            memoryUsed += request.Size.Width * request.Size.Height * 24 / 8;
                            if ((mCacheLimitAsMemory != 0 && memoryUsed > mCacheLimitAsMemory) ||
                                (mCacheLimitAsItemCount != 0 && thumbCache.Count > mCacheLimitAsItemCount))
                                cleanupRequired = true;
                        }
                    }

                    // Clean up invisible items
                    if (cleanupRequired)
                    {
                        Dictionary<Guid, bool> visible = (Dictionary<Guid, bool>)mImageListView.Invoke(new GetVisibleItemsInternal(mImageListView.GetVisibleItems));
                        lock (lockObject)
                        {
                            foreach (KeyValuePair<Guid, CacheItem> item in thumbCache)
                            {
                                if (!visible.ContainsKey(item.Key))
                                {
                                    removedItems.Add(item.Key);
                                    memoryUsedByRemoved += item.Value.Size.Width * item.Value.Size.Height * 24 / 8;
                                }
                            }
                            foreach (Guid iguid in removedItems)
                            {
                                if (thumbCache.ContainsKey(iguid))
                                {
                                    thumbCache[iguid].Dispose();
                                    thumbCache.Remove(iguid);
                                }
                            }
                            removedItems.Clear();
                            memoryUsed -= memoryUsedByRemoved;
                            memoryUsedByRemoved = 0;
                        }
                    }

                    if (thumbnailCreated)
                    {
                        mImageListView.BeginInvoke(new ThumbnailCachedEventHandlerInternal(mImageListView.OnThumbnailCachedInternal), guid);
                        mImageListView.BeginInvoke(new RefreshEventHandlerInternal(mImageListView.OnRefreshInternal));
                    }
                }
            }
        }
        #endregion
    }
}
