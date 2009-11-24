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
    internal class ImageListViewCacheManager
    {
        #region Constants
        private const int PropertyTagThumbnailData = 0x501B;
        private const int PropertyTagThumbnailImageWidth = 0x5020;
        private const int PropertyTagThumbnailImageHeight = 0x5021;
        private const float EmbeddedThumbnailSizeTolerance = 1.2f;
        #endregion

        #region Member Variables
        private ImageListView mImageListView;
        private Thread mThread;

        private Dictionary<Guid, CacheItem> toCache;
        private Dictionary<Guid, CacheItem> thumbCache;
        #endregion

        #region Private Classes
        /// <summary>
        /// Represents an item in the thumbnail cache.
        /// </summary>
        private class CacheItem
        {
            private string mFileName;
            private Size mSize;
            private Image mImage;
            private CacheState mState;
            private UseEmbeddedThumbnails mUseEmbeddedThumbnails;

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
            }

            public CacheItem(string filename, Size size, Image image, CacheState state, UseEmbeddedThumbnails useEmbeddedThumbnails)
                : this(filename, size, image, state)
            {
                mUseEmbeddedThumbnails = useEmbeddedThumbnails;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the owner image list view.
        /// </summary>
        public ImageListView ImageListView { get { return mImageListView; } }
        /// <summary>
        /// Gets the thumbnail generator thread.
        /// </summary>
        public Thread Thread { get { return mThread; } }
        #endregion

        #region Constructor
        public ImageListViewCacheManager(ImageListView owner)
        {
            mImageListView = owner;

            toCache = new Dictionary<Guid, CacheItem>();
            thumbCache = new Dictionary<Guid, CacheItem>();

            mThread = new Thread(new ParameterizedThreadStart(DoWork));
            mThread.IsBackground = true;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Gets the cache state of the specified item.
        /// </summary>
        public CacheState GetCacheState(Guid guid)
        {
            CacheState state = CacheState.Unknown;

            lock (thumbCache)
            {
                if (thumbCache.ContainsKey(guid))
                    state = thumbCache[guid].State;
            }
            if (state == CacheState.Unknown)
            {
                lock (toCache)
                {
                    if (toCache.ContainsKey(guid))
                        state = toCache[guid].State;
                }
            }

            return state;
        }
        /// <summary>
        /// Starts the thumbnail generator thread.
        /// </summary>
        public void Start()
        {
            mThread.Start(this);
            while (!mThread.IsAlive) ;
        }
        /// <summary>
        /// Stops the thumbnail generator thread.
        /// </summary>
        public void Stop()
        {
            if (mThread.IsAlive)
            {
                mThread.Abort();
                mThread.Join();
            }
        }
        /// <summary>
        /// Cleans the thumbnail cache.
        /// </summary>
        public void Clean()
        {
            lock (thumbCache)
            {
                thumbCache.Clear();
            }
        }
        /// <summary>
        /// Adds the image to the cache queue.
        /// </summary>
        public void AddToCache(Guid guid, string filename)
        {
            Size thumbSize = mImageListView.ThumbnailSize;
            UseEmbeddedThumbnails useEmbeddedThumbnails = mImageListView.UseEmbeddedThumbnails;

            bool isCached = false;
            lock (thumbCache)
            {
                if (thumbCache.ContainsKey(guid))
                {
                    if (thumbCache[guid].Size == thumbSize)
                        isCached = true;
                    else
                        thumbCache.Remove(guid);
                }
            }
            if (!isCached)
            {
                lock (toCache)
                {
                    if (!toCache.ContainsKey(guid))
                    {
                        toCache.Add(guid, new CacheItem(filename, thumbSize, null, CacheState.InQueue, useEmbeddedThumbnails));
                        Monitor.Pulse(toCache);
                    }
                }
            }
        }
        /// <summary>
        /// Removes the given item from the cache.
        /// </summary>
        public bool RemoveFromCache(Guid guid)
        {
            bool ret = false;
            lock (thumbCache)
            {
                if (thumbCache.ContainsKey(guid))
                {
                    ret = thumbCache.Remove(guid);
                }
            }
            lock (toCache)
            {
                if (toCache.ContainsKey(guid))
                {
                    toCache.Remove(guid);
                }
            }
            return ret;
        }
        /// <summary>
        /// Gets the image from the thumbnail cache. If the image is not cached,
        /// null will be returned.
        /// </summary>
        public Image GetImage(Guid guid)
        {
            // Default to null.
            Image img = null;

            lock (thumbCache)
            {
                if (thumbCache.ContainsKey(guid))
                {
                    img = thumbCache[guid].Image;
                }
            }
            return img;
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Used by the worker thread to generate image thumbnails.
        /// Once a thumbnail image is generated, the item will be redrawn
        /// to replace the placeholder image.
        /// </summary>
        private static void DoWork(object data)
        {
            ImageListViewCacheManager owner = (ImageListViewCacheManager)data;

            while (true)
            {
                UseEmbeddedThumbnails useEmbedded = UseEmbeddedThumbnails.Auto;
                Size thumbsize = new Size();
                Guid guid = new Guid();
                string filename = "";
                lock (owner.toCache)
                {
                    // Wait until we have items waiting to be cached
                    if (owner.toCache.Count == 0)
                        Monitor.Wait(owner.toCache);

                    if (owner.toCache.Count != 0)
                    {
                        // Get an item from the queue
                        foreach (KeyValuePair<Guid, CacheItem> pair in owner.toCache)
                        {
                            guid = pair.Key;
                            CacheItem request = pair.Value;
                            filename = request.FileName;
                            thumbsize = request.Size;
                            useEmbedded = request.UseEmbeddedThumbnails;
                            break;
                        }
                        owner.toCache.Remove(guid);
                    }
                }
                // Is it already cached?
                if (filename != "")
                {
                    lock (owner.thumbCache)
                    {
                        if (owner.thumbCache.ContainsKey(guid))
                        {
                            if (owner.thumbCache[guid].Size == thumbsize)
                                filename = "";
                            else
                                owner.thumbCache.Remove(guid);
                        }
                    }
                }
                // Is it outside visible area?
                if (filename != "")
                {
                    bool isvisible = (bool)owner.ImageListView.Invoke(new CheckItemVisibleInternal(owner.mImageListView.IsItemVisible), guid);
                    if (!isvisible)
                        filename = "";
                }

                // Proceed if we have a filename
                if (filename != "")
                {
                    bool thumbnailCreated = false;
                    Image thumb = ThumbnailFromFile(filename, thumbsize, useEmbedded);
                    lock (owner.thumbCache)
                    {
                        if (!owner.thumbCache.ContainsKey(guid) || owner.thumbCache[guid].Size != thumbsize)
                        {
                            owner.thumbCache.Remove(guid);
                            if (thumb == null)
                                owner.thumbCache.Add(guid, new CacheItem(filename, thumbsize, null, CacheState.Error));
                            else
                                owner.thumbCache.Add(guid, new CacheItem(filename, thumbsize, thumb, CacheState.Cached));
                            thumbnailCreated = true;
                        }
                    }
                    if (thumbnailCreated)
                    {
                        owner.mImageListView.Invoke(new ThumbnailCachedEventHandlerInternal(owner.mImageListView.OnThumbnailCachedInternal), guid);
                        owner.mImageListView.Invoke(new RefreshEventHandlerInternal(owner.mImageListView.OnRefreshInternal));
                    }
                }
            }
        }
        /// <summary>
        /// Creates a thumbnail image of given size for the specified image file.
        /// </summary>
        private static Image ThumbnailFromFile(string filename, Size thumbSize, UseEmbeddedThumbnails useEmbedded)
        {
            Bitmap thumb = null;
            try
            {
                if (thumbSize.Width <= 0 || thumbSize.Height <= 0)
                    throw new ArgumentException();

                Image sourceImage = null;
                if (useEmbedded != UseEmbeddedThumbnails.Never)
                {
                    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        sourceImage = Image.FromStream(stream, false, false);
                        bool hasTag = false;
                        // Try to get the embedded thumbnail.
                        foreach (int index in sourceImage.PropertyIdList)
                        {
                            if (index == PropertyTagThumbnailData)
                            {
                                hasTag = true;
                                byte[] rawImage = sourceImage.GetPropertyItem(PropertyTagThumbnailData).Value;
                                sourceImage.Dispose();
                                using (MemoryStream memStream = new MemoryStream(rawImage))
                                {
                                    sourceImage = Image.FromStream(memStream);
                                }
                                if (useEmbedded == UseEmbeddedThumbnails.Auto)
                                {
                                    // Check that the embedded thumbnail is large enough.
                                    float aspectRatio = (float)sourceImage.Width / (float)sourceImage.Height;
                                    Size actualThumbSize = Size.Empty;
                                    if (aspectRatio > 1.0f)
                                        actualThumbSize = new Size(thumbSize.Width, (int)(((float)thumbSize.Height) / aspectRatio));
                                    else
                                        actualThumbSize = new Size((int)(((float)thumbSize.Width) * aspectRatio), thumbSize.Height);

                                    if (System.Math.Max((float)actualThumbSize.Width / (float)sourceImage.Width, (float)actualThumbSize.Height / (float)sourceImage.Height) > EmbeddedThumbnailSizeTolerance)
                                    {
                                        sourceImage.Dispose();
                                        sourceImage = null;
                                    }
                                }
                            }
                        }
                        if (!hasTag)
                        {
                            sourceImage.Dispose();
                            sourceImage = null;
                        }
                    }
                }

                // If the source image does not have an embedded thumbnail or if the
                // embedded thumbnail is too small, read and scale the entire image.
                if (sourceImage == null)
                    sourceImage = Image.FromFile(filename);

                float f = System.Math.Max((float)sourceImage.Width / (float)thumbSize.Width, (float)sourceImage.Height / (float)thumbSize.Height);
                if (f < 1.0f) f = 1.0f; // Do not upsize small images
                int x = (int)System.Math.Round((float)sourceImage.Width / f);
                int y = (int)System.Math.Round((float)sourceImage.Height / f);
                thumb = new Bitmap(x, y);
                using (Graphics g = Graphics.FromImage(thumb))
                {
                    g.FillRectangle(Brushes.White, 0, 0, x, y);
                    g.DrawImage(sourceImage, 0, 0, x, y);
                }
                sourceImage.Dispose();
            }
            catch
            {
                thumb = null;
            }
            return thumb;
        }
        #endregion
    }
}
