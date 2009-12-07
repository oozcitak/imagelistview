using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the cache manager responsible for asynchronously loading
    /// item details.
    /// </summary>
    internal class ImageListViewItemCacheManager : IDisposable
    {
        #region Member Variables
        private readonly object lockObject;

        private ImageListView mImageListView;
        private Thread mThread;

        private Queue<CacheItem> toCache;
        private Dictionary<Guid, bool> editCache;

        private bool stopping;
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

        #region Constructor
        public ImageListViewItemCacheManager(ImageListView owner)
        {
            lockObject = new object();

            mImageListView = owner;

            toCache = new Queue<CacheItem>();
            editCache = new Dictionary<Guid, bool>();

            mThread = new Thread(new ThreadStart(DoWork));
            mThread.IsBackground = true;

            stopping = false;

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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (lockObject)
            {
                if (!stopping)
                {
                    stopping = true;
                    Monitor.Pulse(lockObject);
                }
            }
            mThread.Abort();
            mThread.Join();
        }
        /// <summary>
        /// Adds the item to the cache queue.
        /// </summary>
        public void Add(ImageListViewItem item)
        {
            lock (lockObject)
            {
                toCache.Enqueue(new CacheItem(item));
                Monitor.Pulse(lockObject);
            }
        }
        #endregion

        #region Worker Method
        /// <summary>
        /// Used by the worker thread to read item data.
        /// </summary>
        private void DoWork()
        {
            while (true)
            {
                lock (lockObject)
                {
                    if (stopping) return;
                }

                CacheItem item = null;
                lock (lockObject)
                {
                    // Wait until we have items waiting to be cached
                    if (toCache.Count == 0)
                        Monitor.Wait(lockObject);

                    if (stopping) return;

                    // Get an item from the queue
                    item = toCache.Dequeue();

                    // Is it being edited?
                    if (editCache.ContainsKey(item.Item.Guid))
                        item = null;
                }

                // Read file info
                if (item != null)
                {
                    Utility.ShellImageFileInfo info = new Utility.ShellImageFileInfo(item.FileName);
                    string path = Path.GetDirectoryName(item.FileName);
                    string name = Path.GetFileName(item.FileName);
                    // Update file info
                    mImageListView.BeginInvoke(new UpdateItemDetailsEventHandlerInternal(
                        mImageListView.UpdateItemDetailsInternal), item.Item,
                        info.LastAccessTime, info.CreationTime, info.LastWriteTime,
                        info.Size, info.TypeName, path, name, info.Dimension, info.Resolution);
                }
            }
        }
        #endregion
    }
}
