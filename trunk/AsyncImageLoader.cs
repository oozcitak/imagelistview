using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Threading;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Loads images on a separate thread.
    /// </summary>
    [ToolboxBitmap(typeof(AsyncImageLoader))]
    [Description("Loads images on a separate thread.")]
    [DefaultEvent("ImageLoaderCompleted")]
    public class AsyncImageLoader : Component
    {
        #region Member Variables
        private readonly object lockObject;

        private Thread thread;
        private SynchronizationContext context;
        private bool stopping;
        private bool started;

        private Queue<WorkItem> items;
        private Dictionary<object, bool> cancelledItems;

        private readonly SendOrPostCallback loaderCompleted;
        #endregion

        #region WorkItem Class
        /// <summary>
        /// Represents a work item in the thread queue.
        /// </summary>
        private class WorkItem
        {
            public object Key;
            public string FileName;
            public Image Image;
            public Size Size;
            public UseEmbeddedThumbnails UseEmbeddedThumbnails;
            public bool AutoRotate;

            public WorkItem(object key, string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
            {
                Key = key;
                FileName = filename;
                Image = null;
                Size = size;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
            }

            public WorkItem(object key, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
            {
                Key = key;
                FileName = null;
                Image = image;
                Size = size;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncImageLoader"/> class.
        /// </summary>
        public AsyncImageLoader()
        {
            lockObject = new object();
            stopping = false;
            started = false;

            // Work items
            items = new Queue<WorkItem>();
            cancelledItems = new Dictionary<object, bool>();

            // The loader complete callback
            loaderCompleted = new SendOrPostCallback(this.AsyncImageLoaderCompletedCallback);
        }
        #endregion

        #region Load Async From File
        /// <summary>
        /// Loads the image with the specified filename asynchronously.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="filename">The path of an image file.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        /// <param name="useEmbeddedThumbnails">Embedded Exif thumbnail extraction behaviour.
        /// This parameter is ignored if the size parameter is Size.Empty.</param>
        /// <param name="autoRotate">true to automatically rotate the image based on 
        /// orientation metadata; otherwise false.</param>
        public void LoadAsync(object key, string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
        {
            Start();

            lock (lockObject)
            {
                items.Enqueue(new WorkItem(key, filename, size, useEmbeddedThumbnails, autoRotate));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading embbedded thumbnails when possible. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="filename">The path of an image file.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        public void LoadAsync(object key, string filename, Size size)
        {
            LoadAsync(key, filename, size, UseEmbeddedThumbnails.Auto, true);
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading the entire image without resizing. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="filename">The path of an image file.</param>
        public void LoadAsync(object key, string filename)
        {
            LoadAsync(key, filename, Size.Empty, UseEmbeddedThumbnails.Auto, true);
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously.
        /// </summary>
        /// <param name="filename">The path of an image file.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        /// <param name="useEmbeddedThumbnails">Embedded Exif thumbnail extraction behaviour.
        /// This parameter is ignored if the size parameter is Size.Empty.</param>
        /// <param name="autoRotate">true to automatically rotate the image based on 
        /// orientation metadata; otherwise false.</param>
        public void LoadAsync(string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
        {
            LoadAsync(null, filename, size, useEmbeddedThumbnails, autoRotate);
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading embbedded thumbnails when possible. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="filename">The path of an image file.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        public void LoadAsync(string filename, Size size)
        {
            LoadAsync(null, filename, size, UseEmbeddedThumbnails.Auto, true);
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading the entire image without resizing. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="filename">The path of an image file.</param>
        public void LoadAsync(string filename)
        {
            LoadAsync(null, filename, Size.Empty, UseEmbeddedThumbnails.Auto, true);
        }
        #endregion

        #region Load Async From Image
        /// <summary>
        /// Loads the image from the given source image asynchronously.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="image">The source image.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        /// <param name="useEmbeddedThumbnails">Embedded Exif thumbnail extraction behaviour.
        /// This parameter is ignored if the size parameter is Size.Empty.</param>
        /// <param name="autoRotate">true to automatically rotate the image based on 
        /// orientation metadata; otherwise false.</param>
        public void LoadAsync(object key, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
        {
            Start();

            lock (lockObject)
            {
                items.Enqueue(new WorkItem(key, image, size, useEmbeddedThumbnails, autoRotate));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading embbedded thumbnails when possible. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="image">The source image.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        public void LoadAsync(object key, Image image, Size size)
        {
            LoadAsync(key, image, size, UseEmbeddedThumbnails.Auto, true);
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading the entire image without resizing. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="image">The source image.</param>
        public void LoadAsync(object key, Image image)
        {
            LoadAsync(key, image, Size.Empty, UseEmbeddedThumbnails.Auto, true);
        }
        /// <summary>
        /// Loads the image from the given source image asynchronously.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        /// <param name="useEmbeddedThumbnails">Embedded Exif thumbnail extraction behaviour.
        /// This parameter is ignored if the size parameter is Size.Empty.</param>
        /// <param name="autoRotate">true to automatically rotate the image based on 
        /// orientation metadata; otherwise false.</param>
        public void LoadAsync(Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
        {
            LoadAsync(null, image, size, useEmbeddedThumbnails, autoRotate);
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading embbedded thumbnails when possible. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        public void LoadAsync(Image image, Size size)
        {
            LoadAsync(null, image, size, UseEmbeddedThumbnails.Auto, true);
        }
        /// <summary>
        /// Loads the image with the specified filename asynchronously,
        /// reading the entire image without resizing. The image will
        /// be rotated automatically based on orientation metadata.
        /// </summary>
        /// <param name="image">The source image.</param>
        public void LoadAsync(Image image)
        {
            LoadAsync(null, image, Size.Empty, UseEmbeddedThumbnails.Auto, true);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether the image loader is being stopped.
        /// </summary>
        private bool Stopping { get { lock (lockObject) { return stopping; } } }
        #endregion

        #region Start, Cancel and Stop
        /// <summary>
        /// Starts the worker thread.
        /// </summary>
        private void Start()
        {
            if (started)
                return;

            // Get the synchronization context
            if (context == null)
                context = SynchronizationContext.Current;

            // Start the thread
            thread = new Thread(new ThreadStart(DoWork));
            thread.IsBackground = true;
            thread.Start();
            while (!thread.IsAlive) ;

            started = true;
        }
        /// <summary>
        /// Cancels all pending operations.
        /// </summary>
        public void CancelAllAsync()
        {
            lock (lockObject)
            {
                items.Clear();
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Cancels processing the item with the given key.
        /// </summary>
        public void CancelAsync(object key)
        {
            lock (lockObject)
            {
                cancelledItems.Add(key, false);
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Cancels all pending operations and stops the worker thread.
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
        #endregion

        #region Virtual Methods
        /// <summary>
        /// Used to call OnImageLoaderCompleted by the SynchronizationContext.
        /// </summary>
        /// <param name="arg">The argument.</param>
        private void AsyncImageLoaderCompletedCallback(object arg)
        {
            OnImageLoaderCompleted((AsyncImageLoaderCompletedEventArgs)arg);
        }
        /// <summary>
        /// Raises the ImageLoaderCompleted event.
        /// </summary>
        /// <param name="e">An ImageLoaderCompletedEventArgs that contains event data.</param>
        protected virtual void OnImageLoaderCompleted(AsyncImageLoaderCompletedEventArgs e)
        {
            if (ImageLoaderCompleted != null)
                ImageLoaderCompleted(this, e);
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Sets the apartment state of the worker thread. The apartment state
        /// cannot be changed after the worker thread is started.
        /// </summary>
        public void SetApartmentState(ApartmentState state)
        {
            thread.SetApartmentState(state);
        }
        #endregion

        #region Public Events
        /// <summary>
        /// Occurs when a load operation is completed.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when a load operation is completed.")]
        public event AsyncImageLoaderCompletedEventHandler ImageLoaderCompleted;
        #endregion

        #region Worker Method
        /// <summary>
        /// Used by the worker thread to load images.
        /// </summary>
        private void DoWork()
        {
            while (!Stopping)
            {
                lock (lockObject)
                {
                    // Wait until we have pending work items
                    if (items.Count == 0)
                        Monitor.Wait(lockObject);
                }

                // Loop until we exhaust the queue
                bool queueFull = true;
                while (queueFull && !Stopping)
                {
                    // Get an item from the queue
                    WorkItem request = null;
                    lock (lockObject)
                    {
                        if (items.Count != 0)
                        {
                            request = items.Dequeue();

                            // Check if the item was removed
                            if (cancelledItems.ContainsKey(request.Key))
                                request = null;
                        }
                    }

                    if (request != null)
                    {
                        Image image = null;
                        Exception error = null;

                        // Read the image
                        try
                        {
                            if (!string.IsNullOrEmpty(request.FileName))
                            {
                                image = ThumbnailExtractor.FromFile(request.FileName,
                                    request.Size, request.UseEmbeddedThumbnails, request.AutoRotate);
                            }
                            else if (request.Image != null)
                            {
                                image = ThumbnailExtractor.FromImage(request.Image,
                                    request.Size, request.UseEmbeddedThumbnails, request.AutoRotate);
                            }
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }

                        // Raise the image loaded event
                        AsyncImageLoaderCompletedEventArgs result = new AsyncImageLoaderCompletedEventArgs(request.Key, image, error);
                        context.Post(loaderCompleted, result);
                    }

                    // Check if the cache is exhausted
                    lock (lockObject)
                    {
                        if (items.Count == 0)
                            queueFull = false;
                    }

                }
            }
        }
        #endregion
    }
}
