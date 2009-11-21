using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the cache manager responsible for asynchronously loading
    /// item details.
    /// </summary>
    internal class ImageListViewItemCacheManager
    {
        #region Member Variables
        private ImageListView mImageListView;
        private Thread mThread;

        private Queue<CacheItem> toCache;
        #endregion

        #region Private Classes
        /// <summary>
        /// Represents an item in the item cache.
        /// </summary>
        private class CacheItem
        {
            private ImageListViewItem mItem;
            private string mFileName;

            /// <summary>
            /// Gets the item.
            /// </summary>
            public ImageListViewItem Item { get { return mItem; } }
            /// <summary>
            /// Gets the name of the image file.
            /// </summary>
            public string FileName { get { return mFileName; } }

            public CacheItem(ImageListViewItem item)
            {
                mItem = item;
                mFileName = item.FileName;
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
        public ImageListViewItemCacheManager(ImageListView owner)
        {
            mImageListView = owner;

            toCache = new Queue<CacheItem>();

            mThread = new Thread(new ParameterizedThreadStart(DoWork));
            mThread.IsBackground = true;
        }
        #endregion

        #region Instance Methods
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
        /// Adds the item to the cache queue.
        /// </summary>
        public void AddToCache(ImageListViewItem item)
        {
            if (!item.isDirty)
                return;

            lock (toCache)
            {
                toCache.Enqueue(new CacheItem(item));
                Monitor.Pulse(toCache);
            }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Used by the worker thread to read item data.
        /// </summary>
        private static void DoWork(object data)
        {
            ImageListViewItemCacheManager owner = (ImageListViewItemCacheManager)data;

            while (true)
            {
                CacheItem item = null;
                lock (owner.toCache)
                {
                    // Wait until we have items waiting to be cached
                    if (owner.toCache.Count == 0)
                        Monitor.Wait(owner.toCache);

                    // Get an item from the queue
                    item = owner.toCache.Dequeue();
                }
                // Read file info
                string filename = item.FileName;
                Utility.ShellFileInfo info = new Utility.ShellFileInfo(filename);
                string path = Path.GetDirectoryName(filename);
                string name = Path.GetFileName(filename);
                Size dimension;
                SizeF resolution;
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    using (Image img = Image.FromStream(stream, false, false))
                    {
                        dimension = img.Size;
                        resolution = new SizeF(img.HorizontalResolution, img.VerticalResolution);
                    }
                }
                // Update file info

                if (!info.Error)
                {
                    lock (item.Item)
                    {
                        item.Item.UpdateDetailsInternal(info.LastAccessTime, info.CreationTime, info.LastWriteTime,
                        info.Size, info.TypeName, path, name, dimension, resolution);
                    }
                }
            }
        }
        #endregion
    }
}
