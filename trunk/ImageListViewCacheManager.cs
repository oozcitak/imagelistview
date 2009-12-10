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

        private Stack<CacheItem> toCache;
        private Dictionary<Guid, CacheItem> thumbCache;
        private Dictionary<Guid, Image> editCache;

        private Stack<CacheItem> rendererToCache;
        private Guid rendererGuid;
        private CacheItem rendererItem;

        private long memoryUsed;
        private long memoryUsedByRemoved;
        private List<Guid> removedItems;

        private volatile bool stopping;
        private bool stopped;
        private bool disposed;
        #endregion

        #region Private Classes
        /// <summary>
        /// Represents an item in the thumbnail cache.
        /// </summary>
        private class CacheItem : IDisposable
        {
            private Guid mGuid;
            private string mFileName;
            private Size mSize;
            private Image mImage;
            private CacheState mState;
            private UseEmbeddedThumbnails mUseEmbeddedThumbnails;
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

            public CacheItem(Guid guid, string filename, Size size, Image image, CacheState state)
            {
                mGuid = guid;
                mFileName = filename;
                mSize = size;
                mImage = image;
                mState = state;
                disposed = false;
            }

            public CacheItem(Guid guid, string filename, Size size, Image image, CacheState state, UseEmbeddedThumbnails useEmbeddedThumbnails)
                : this(guid, filename, size, image, state)
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
        /// </summary>
        public long CacheSize { get { lock (lockObject) { return thumbCache.Count; } } }
        #endregion

        #region Constructor
        public ImageListViewCacheManager(ImageListView owner)
        {
            lockObject = new object();

            mImageListView = owner;
            mCacheLimitAsItemCount = 0;
            mCacheLimitAsMemory = 20 * 1024 * 1024;

            toCache = new Stack<CacheItem>();
            thumbCache = new Dictionary<Guid, CacheItem>();
            editCache = new Dictionary<Guid, Image>();

            rendererToCache = new Stack<CacheItem>();
            rendererGuid = new Guid();
            rendererItem = null;

            memoryUsed = 0;
            memoryUsedByRemoved = 0;
            removedItems = new List<Guid>();

            mThread = new Thread(new ThreadStart(DoWork));
            mThread.IsBackground = true;

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
            }

            return CacheState.Unknown;
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
        public void Add(Guid guid, string filename, Size thumbSize, 
            UseEmbeddedThumbnails useEmbeddedThumbnails)
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
                toCache.Push(new CacheItem(guid, filename, 
                    thumbSize, null, CacheState.Unknown, useEmbeddedThumbnails));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Adds the image to the renderer cache.
        /// </summary>
        public void AddToRendererCache(Guid guid, string filename, 
            Size thumbSize, UseEmbeddedThumbnails useEmbeddedThumbnails)
        {
            lock (lockObject)
            {
                // Already cached?
                if (rendererGuid == guid && rendererItem != null && 
                    rendererItem.Size == thumbSize && 
                    rendererItem.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                    return;

                // Renderer cache holds one item only.
                rendererToCache.Clear();

                rendererToCache.Push(new CacheItem(guid, filename, 
                    thumbSize, null, CacheState.Unknown, useEmbeddedThumbnails));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Gets the image from the renderer cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        public Image GetRendererImage(Guid guid, Size thumbSize, 
            UseEmbeddedThumbnails useEmbeddedThumbnails)
        {
            lock (lockObject)
            {
                if (rendererGuid == guid && rendererItem != null && 
                    rendererItem.Size == thumbSize && 
                    rendererItem.UseEmbeddedThumbnails == useEmbeddedThumbnails)
                    return rendererItem.Image;
            }
            return null;
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
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (lockObject)
            {
                if (!stopping)
                    Stop();
                if (!stopped)
                    return;
            }

            if (disposed) return;

            lock (lockObject)
            {

                foreach (CacheItem item in thumbCache.Values)
                    item.Dispose();
                thumbCache.Clear();

                foreach (CacheItem item in toCache)
                    item.Dispose();
                toCache.Clear();

                foreach (Image img in editCache.Values)
                    img.Dispose();
                editCache.Clear();

                foreach (CacheItem item in rendererToCache)
                    item.Dispose();
                rendererToCache.Clear();
                if (rendererItem != null)
                    rendererItem.Dispose();

                memoryUsed = 0;
                memoryUsedByRemoved = 0;
                removedItems.Clear();
            }

            disposed = true;
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
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            while (!stopping)
            {
                Guid guid = new Guid();
                CacheItem request = null;
                bool rendererRequest = false;
                lock (lockObject)
                {
                    // Wait until we have items waiting to be cached
                    if (toCache.Count == 0 && rendererToCache.Count == 0)
                        Monitor.Wait(lockObject);
                }

                // Set to true when we exceed the cache memory limit
                bool cleanupRequired = false;
                // Set to true when we fetch at least one thumbnail
                bool thumbnailCreated = false;

                // Loop until we exhaust the queue
                bool queueFull = true;
                while (queueFull)
                {
                    lock (lockObject)
                    {
                        sw.Start();
                        // Get an item from the queue
                        if (toCache.Count != 0)
                        {
                            request = toCache.Pop();
                            guid = request.Guid;

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
                        else if (rendererToCache.Count != 0)
                        {
                            request = rendererToCache.Pop();
                            guid = request.Guid;
                            rendererToCache.Clear();
                            rendererRequest = true;
                        }
                    }

                    // Is it outside visible area?
                    bool isvisible = false;
                    if (request != null)
                    {
                        if (!stopping)
                        {
                            isvisible = (bool)mImageListView.Invoke(
                                new CheckItemVisibleDelegateInternal(mImageListView.IsItemVisible), guid);
                        }
                    }

                    lock (lockObject)
                    {
                        if (!rendererRequest && !isvisible)
                            request = null;
                    }

                    // Proceed if we have a filename
                    CacheItem result = null;
                    if (request != null)
                    {
                        Image thumb = null;

                        // Is it in the edit cache?
                        Image editSource = null;
                        lock (lockObject)
                        {
                            if (!editCache.TryGetValue(guid, out editSource))
                                editSource = null;
                        }
                        if (editSource != null)
                            thumb = Utility.ThumbnailFromImage(editSource,
                                request.Size, Color.White);

                        // Read from file
                        if (thumb == null)
                            thumb = Utility.ThumbnailFromFile(request.FileName,
                                request.Size, request.UseEmbeddedThumbnails, Color.White);

                        // Create the cache item
                        if (thumb == null)
                            result = new CacheItem(guid, request.FileName,
                                request.Size, null, CacheState.Error, request.UseEmbeddedThumbnails);
                        else
                            result = new CacheItem(guid, request.FileName,
                                request.Size, thumb, CacheState.Cached, request.UseEmbeddedThumbnails);
                        thumbnailCreated = true;

                        if (rendererRequest)
                        {
                            lock (lockObject)
                            {
                                if (rendererItem != null)
                                    rendererItem.Dispose();

                                rendererGuid = guid;
                                rendererItem = result;
                                rendererRequest = false;
                            }
                        }
                        else
                        {
                            lock (lockObject)
                            {
                                thumbCache.Add(guid, result);

                                if (thumb != null)
                                {
                                    // Did we exceed the cache limit?
                                    memoryUsed += thumb.Width * thumb.Height * 24 / 8;
                                    if ((mCacheLimitAsMemory != 0 && memoryUsed > mCacheLimitAsMemory) ||
                                        (mCacheLimitAsItemCount != 0 && thumbCache.Count > mCacheLimitAsItemCount))
                                        cleanupRequired = true;
                                }
                            }
                        }

                        if (!stopping)
                        {
                            mImageListView.Invoke(new ThumbnailCachedEventHandlerInternal(
                                mImageListView.OnThumbnailCachedInternal), guid);
                        }
                    }

                    // Check if the cache is exhausted
                    lock (lockObject)
                    {
                        if (toCache.Count == 0 && rendererToCache.Count == 0)
                            queueFull = false;
                    }

                    // Do we need a refresh?
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 100)
                    {
                        if (!stopping)
                        {
                            mImageListView.Invoke(
                                new RefreshDelegateInternal(mImageListView.OnRefreshInternal));
                        }
                        sw.Reset();
                    }
                    if (queueFull)
                        sw.Start();
                    else
                    {
                        sw.Reset();
                        sw.Stop();
                    }
                }

                // Clean up invisible items
                if (cleanupRequired)
                {
                    Dictionary<Guid, bool> visible = new Dictionary<Guid, bool>();
                    if (!stopping)
                    {

                        visible = (Dictionary<Guid, bool>)mImageListView.Invoke(
                            new GetVisibleItemsDelegateInternal(mImageListView.GetVisibleItems));
                    }

                    if (visible.Count != 0)
                    {
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
                }

                if (thumbnailCreated)
                {
                    if (!stopping)
                    {
                        mImageListView.Invoke(
                            new RefreshDelegateInternal(mImageListView.OnRefreshInternal));
                    }
                }
            }

            lock (lockObject)
            {
                stopped = true;
            }
            Dispose();
        }
        #endregion
    }
}
