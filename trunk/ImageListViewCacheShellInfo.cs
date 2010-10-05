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
    /// shell info.
    /// </summary>
    internal class ImageListViewCacheShellInfo : IDisposable
    {
        #region Member Variables
        QueuedBackgroundWorker bw;
        private SynchronizationContext context;

        private ImageListView mImageListView;

        private Dictionary<string, CacheItem> shellCache;

        private bool disposed;
        #endregion

        #region Private Classes
        /// <summary>
        /// Represents an item in the cache.
        /// </summary>
        private class CacheItem : IDisposable
        {
            private bool disposed;

            /// <summary>
            /// Gets the file extension.
            /// </summary>
            public string Extension { get; private set; }
            /// <summary>
            /// Gets the small shell icon.
            /// </summary>
            public Image SmallIcon { get; private set; }
            /// <summary>
            /// Gets the large shell icon.
            /// </summary>
            public Image LargeIcon { get; private set; }
            /// <summary>
            /// Gets the shell file type.
            /// </summary>
            public string FileType { get; private set; }
            /// <summary>
            /// Gets or sets the state of the cache item.
            /// </summary>
            public CacheState State { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CacheItem"/> class.
            /// </summary>
            /// <param name="extension">The file extension.</param>
            /// <param name="smallIcon">The small shell icon.</param>
            /// <param name="largeIcon">The large shell icon.</param>
            /// <param name="filetype">The shell file type.</param>
            /// <param name="state">The cache state of the item.</param>
            public CacheItem(string extension, Image smallIcon, Image largeIcon, string filetype, CacheState state)
            {
                Extension = extension;
                SmallIcon = smallIcon;
                LargeIcon = largeIcon;
                FileType = filetype;
                State = state;
                disposed = false;
            }
            /// <summary>
            /// Initializes an empty instance of the <see cref="CacheItem"/> class.
            /// </summary>
            /// <param name="extension">The file extension.</param>
            public CacheItem(string extension)
                : this(extension, null, null, string.Empty, CacheState.Unknown)
            {
                ;
            }

            /// <summary>
            /// Performs application-defined tasks associated with 
            /// freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (!disposed)
                {
                    if (SmallIcon != null)
                    {
                        SmallIcon.Dispose();
                        SmallIcon = null;
                    }
                    if (LargeIcon != null)
                    {
                        LargeIcon.Dispose();
                        LargeIcon = null;
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
                if (SmallIcon != null || LargeIcon != null)
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
        public long CacheSize { get { return shellCache.Count; } }
        /// <summary>
        /// Gets or sets the current thumbnail size.
        /// </summary>
        public Size CurrentThumbnailSize { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListViewCacheShellInfo"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public ImageListViewCacheShellInfo(ImageListView owner)
        {
            context = null;
            bw = new QueuedBackgroundWorker();
            bw.DoWork += new QueuedWorkerDoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunQueuedWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.WorkerFinished += new QueuedWorkerFinishedEventHandler(bw_WorkerFinished);

            mImageListView = owner;
            CacheMode = CacheMode.OnDemand;
            CacheLimitAsItemCount = 0;
            CacheLimitAsMemory = 20 * 1024 * 1024;
            RetryOnError = false;

            shellCache = new Dictionary<string, CacheItem>();

            MemoryUsed = 0;
            MemoryUsedByRemoved = 0;

            disposed = false;
        }
        #endregion

        #region Context Callbacks
        /// <summary>
        /// Returns the item from the cache on the UI thread.
        /// </summary>
        /// <param name="extension">The file extension of the cache item.</param>
        /// <returns>The cache item; or null if the item was not found.</returns>
        private CacheItem GetFromCacheCallback(string extension)
        {
            CacheItem existing = null;
            SendOrPostCallback callback = delegate
            {
                shellCache.TryGetValue(extension, out existing);
            };
            context.Send(callback, extension);
            return existing;
        }
        #endregion

        #region QueuedBackgroundWorker Events
        /// <summary>
        /// Handles the WorkerFinished event of the queued background worker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void bw_WorkerFinished(object sender, EventArgs e)
        {
            mImageListView.Refresh();
        }
        /// <summary>
        /// Handles the RunWorkerCompleted event of the queued background worker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Manina.Windows.Forms.QueuedWorkerCompletedEventArgs"/> 
        /// instance containing the event data.</param>
        void bw_RunWorkerCompleted(object sender, QueuedWorkerCompletedEventArgs e)
        {
            CacheItem result = e.Result as CacheItem;

            if (result != null)
            {
                CacheItem existing = null;
                if (shellCache.TryGetValue(result.Extension, out existing))
                {
                    existing.Dispose();
                    shellCache.Remove(result.Extension);
                }
                shellCache.Add(result.Extension, result);
            }

            // Refresh the control lazily
            if (result != null && mImageListView != null)
                mImageListView.Refresh(false, true);
        }
        /// <summary>
        /// Handles the DoWork event of the queued background worker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Manina.Windows.Forms.QueuedWorkerDoWorkEventArgs"/> instance 
        /// containing the event data.</param>
        void bw_DoWork(object sender, QueuedWorkerDoWorkEventArgs e)
        {
            string extension = e.Argument as string;

            // Is it already cached?
            CacheItem existing = GetFromCacheCallback(extension);
            if (existing != null && existing.SmallIcon != null && existing.LargeIcon != null)
            {
                e.Cancel = true;
                return;
            }

            // Read shell info
            ShellInfoExtractor info = ShellInfoExtractor.FromFile(extension);

            // Return the info
            CacheItem result = null;
            if ((info.SmallIcon == null || info.LargeIcon == null) && !RetryOnError)
                result = new CacheItem(extension, info.SmallIcon, info.LargeIcon, info.FileType, CacheState.Error);
            else
                result = new CacheItem(extension, info.SmallIcon, info.LargeIcon, info.FileType, CacheState.Cached);

            e.Result = result;
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Gets the cache state of the specified item.
        /// </summary>
        /// <param name="extension">File extension.</param>
        public CacheState GetCacheState(string extension)
        {
            CacheItem item = null;
            if (shellCache.TryGetValue(extension, out item))
                return item.State;

            return CacheState.Unknown;
        }
        /// <summary>
        /// Rebuilds the cache.
        /// Old items will be kept until they are overwritten
        /// by new ones.
        /// </summary>
        public void Rebuild()
        {
            foreach (CacheItem item in shellCache.Values)
                item.State = CacheState.Unknown;
        }
        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            foreach (CacheItem item in shellCache.Values)
                item.Dispose();
            shellCache.Clear();
        }
        /// <summary>
        /// Removes the given item from the cache.
        /// </summary>
        /// <param name="extension">File extension.</param>
        public void Remove(string extension)
        {
            CacheItem item = null;
            if (shellCache.TryGetValue(extension, out item))
            {
                item.Dispose();
                shellCache.Remove(extension);
            }
        }
        /// <summary>
        /// Pushes the given item to the worker queue.
        /// </summary>
        /// <param name="item">The item to add to the worker queue.</param>
        private void RunWorker(CacheItem item)
        {
            // Get the current synchronization context
            if (context == null)
                context = SynchronizationContext.Current;

            // Add the item to the queue for processing
            bw.RunWorkerAsync(item);
        }
        /// <summary>
        /// Adds the item to the cache queue.
        /// </summary>
        /// <param name="extension">File extension.</param>
        public void Add(string extension)
        {
            // Already cached?
            CacheItem item = null;
            if (shellCache.TryGetValue(extension, out item))
                return;

            // Add to cache queue
            RunWorker(new CacheItem(extension));
        }
        /// <summary>
        /// Gets the small shell icon for the given file extension from the cache.
        /// If the item is not cached, null will be returned.
        /// </summary>
        /// <param name="extension">File extension.</param>
        public Image GetSmallIcon(string extension)
        {
            CacheItem item = null;
            if (shellCache.TryGetValue(extension, out item))
            {
                return item.SmallIcon;
            }
            return null;
        }
        /// <summary>
        /// Gets the large shell icon for the given file extension from the cache.
        /// If the item is not cached, null will be returned.
        /// </summary>
        /// <param name="extension">File extension.</param>
        public Image GetLargeIcon(string extension)
        {
            CacheItem item = null;
            if (shellCache.TryGetValue(extension, out item))
            {
                return item.LargeIcon;
            }
            return null;
        }
        /// <summary>
        /// Gets the shell file type for the given file extension from the cache.
        /// If the item is not cached, null will be returned.
        /// </summary>
        /// <param name="extension">File extension.</param>
        public string GetFileType(string extension)
        {
            CacheItem item = null;
            if (shellCache.TryGetValue(extension, out item))
            {
                return item.FileType;
            }
            return null;
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
        ~ImageListViewCacheShellInfo()
        {
            System.Diagnostics.Debug.Print("Finalizer of {0} called.", GetType());
            Dispose();
        }
#endif
        #endregion
    }
}
