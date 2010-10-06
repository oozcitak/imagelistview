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
using System.IO;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the cache manager responsible for asynchronously loading
    /// item metdata.
    /// </summary>
    internal class ImageListViewCacheMetadata : IDisposable
    {
        #region Member Variables
        QueuedBackgroundWorker bw;
        private SynchronizationContext context;

        private ImageListView mImageListView;

        private Dictionary<Guid, bool> editCache;
        private Dictionary<Guid, bool> processing;
        private Dictionary<Guid, bool> removedItems;

        private bool disposed;
        #endregion

        #region Private Classes
        /// <summary>
        /// Represents an item in the cache.
        /// </summary>
        private class CacheItem
        {
            /// <summary>
            /// Gets the item guid.
            /// </summary>
            public Guid Guid { get; private set; }
            /// <summary>
            /// Gets the file name.
            /// </summary>
            public string FileName { get; private set; }
            /// <summary>
            /// Gets the virtual item key.
            /// </summary>
            public object VirtualItemKey { get; private set; }
            /// <summary>
            /// Gets whether this item is a virtual item.
            /// </summary>
            public bool IsVirtualItem { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CacheItem"/> class.
            /// </summary>
            /// <param name="guid">The guid of the item.</param>
            /// <param name="filename">The file name.</param>
            public CacheItem(Guid guid, string filename)
            {
                Guid = guid;
                FileName = filename;
                IsVirtualItem = false;
                VirtualItemKey = null;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="CacheItem"/> class.
            /// </summary>
            /// <param name="guid">The guid of the item.</param>
            /// <param name="virtualItemKey">The virtual item key.</param>
            public CacheItem(Guid guid, object virtualItemKey)
            {
                Guid = guid;
                FileName = null;
                IsVirtualItem = true;
                VirtualItemKey = virtualItemKey;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether the cache manager retries loading items on errors.
        /// </summary>
        public bool RetryOnError { get; internal set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListViewCacheShellInfo"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public ImageListViewCacheMetadata(ImageListView owner)
        {
            context = null;
            bw = new QueuedBackgroundWorker();
            bw.SetApartmentState(ApartmentState.STA);
            bw.IsBackground = true;
            bw.DoWork += new QueuedWorkerDoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunQueuedWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.WorkerFinished += new QueuedWorkerFinishedEventHandler(bw_WorkerFinished);

            mImageListView = owner;
            RetryOnError = false;

            editCache = new Dictionary<Guid, bool>();
            processing = new Dictionary<Guid, bool>();
            removedItems = new Dictionary<Guid, bool>();

            disposed = false;
        }
        #endregion

        #region Context Callbacks
        /// <summary>
        /// Determines if the item is in the edit cache on the UI thread.
        /// </summary>
        /// <param name="guid">The guid of the cache item.</param>
        /// <returns>true if item is in the edit cache; otherwise false.</returns>
        private bool IsEditing(Guid guid)
        {
            bool exists = false;
            SendOrPostCallback callback = delegate
            {
                exists = editCache.ContainsKey(guid);
            };
            context.Send(callback, guid);
            return exists;
        }
        /// <summary>
        /// Determines if the item was updated on the UI thread.
        /// </summary>
        /// <param name="guid">The guid of the item.</param>
        /// <returns>true if item is updated; otherwise false.</returns>
        private bool IsUpdated(Guid guid)
        {
            bool dirty = false;
            SendOrPostCallback callback = delegate
            {
                if (mImageListView != null)
                    dirty = mImageListView.IsItemDirty(guid);
            };
            context.Send(callback, guid);
            return !dirty;
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
            CacheItem request = e.UserState as CacheItem;

            // We are done processing
            processing.Remove(request.Guid);

            // Do not process the result if the cache operation
            // was cancelled.
            if (e.Cancelled)
                return;

            // Get result
            if (request.IsVirtualItem)
            {
                VirtualItemDetailsEventArgs info = e.Result as VirtualItemDetailsEventArgs;
                mImageListView.UpdateItemDetailsInternal(request.Guid, info);
            }
            else
            {
                ShellImageFileInfo info = e.Result as ShellImageFileInfo;
                mImageListView.UpdateItemDetailsInternal(request.Guid, info);
            }

            // Refresh the control lazily
            if (mImageListView != null)
                mImageListView.Refresh(false, true);

            // Raise the CacheError event
            if (e.Error != null && mImageListView != null)
                mImageListView.OnCacheErrorInternal(request.Guid, e.Error, CacheThread.Details);
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

            // Is it being edited?
            if (IsEditing(request.Guid))
            {
                e.Cancel = true;
                return;
            }

            // Was it fetched by the UI thread in the meantime?
            if (IsUpdated(request.Guid))
            {
                e.Cancel = true;
                return;
            }

            // Get item details
            if (request.IsVirtualItem)
            {
                VirtualItemDetailsEventArgs info = new VirtualItemDetailsEventArgs(request.VirtualItemKey);
                mImageListView.RetrieveVirtualItemDetailsInternal(info);
                e.Result = info;
            }
            else
            {
                ShellImageFileInfo info = ShellImageFileInfo.FromFile(request.FileName);
                if (info.Error != null)
                    throw info.Error;
                e.Result = info;
            }
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
                editCache.Add(guid, false);
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
        /// Removes the given item from the cache.
        /// </summary>
        /// <param name="guid">The guid of the item to remove.</param>
        public void Remove(Guid guid)
        {
            if (!removedItems.ContainsKey(guid))
                removedItems.Add(guid, false);
        }
        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            bw.CancelAsync();
            processing.Clear();
        }
        /// <summary>
        /// Adds the item to the cache queue.
        /// </summary>
        /// <param name="guid">Item guid.</param>
        /// <param name="filename">File name.</param>
        public void Add(Guid guid, string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("filename cannot be null", "extension");

            // Add to cache queue
            RunWorker(new CacheItem(guid, filename));
        }
        /// <summary>
        /// Adds the item to the cache queue.
        /// </summary>
        /// <param name="guid">Item guid.</param>
        /// <param name="virtualItemKey">The virtual item key.</param>
        public void Add(Guid guid, object virtualItemKey)
        {
            // Add to cache queue
            RunWorker(new CacheItem(guid, virtualItemKey));
        }
        #endregion

        #region RunWorker
        /// <summary>
        /// Pushes the given item to the worker queue.
        /// </summary>
        /// <param name="item">The cache item.</param>
        private void RunWorker(CacheItem item)
        {
            // Get the current synchronization context
            if (context == null)
                context = SynchronizationContext.Current;

            // Already being processed?
            if (processing.ContainsKey(item.Guid))
                return;
            else
                processing.Add(item.Guid, false);

            // Add the item to the queue for processing
            bw.RunWorkerAsync(item);
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
        ~ImageListViewCacheMetadata()
        {
            System.Diagnostics.Debug.Print("Finalizer of {0} called.", GetType());
            Dispose();
        }
#endif
        #endregion

        #region Utility for Reading Image Details
        /// <summary>
        /// A utility class for reading image details.
        /// </summary>
        internal class ShellImageFileInfo
        {
            public FileAttributes FileAttributes;
            public DateTime CreationTime;
            public DateTime LastAccessTime;
            public DateTime LastWriteTime;
            public string Extension;
            public string DirectoryName;
            public string DisplayName;
            public long Size;
            // Image info
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
            public string Software;
            public float FocalLength;
            // Error
            internal Exception Error;

            /// <summary>
            /// Gets image details for the given file.
            /// </summary>
            /// <param name="path">The path to an image file.</param>
            public static ShellImageFileInfo FromFile(string path)
            {
                ShellImageFileInfo imageInfo = new ShellImageFileInfo();

                if (string.IsNullOrEmpty(path))
                    return imageInfo;

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
                    imageInfo.Software = metadata.Software ?? "";
                    imageInfo.FocalLength = (float)metadata.FocalLength;
                    if (metadata.Error != null)
                        imageInfo.Error = metadata.Error;
                }
                catch (Exception e)
                {
                    imageInfo.Error = e;
                }

                return imageInfo;
            }
        }
        #endregion
    }
}
