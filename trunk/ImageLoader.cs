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
    public class ImageLoader : Component
    {
        #region Member Variables
        private readonly object lockObject = new object();
        private bool stopping = false;
        private Thread thread;
        private Queue<WorkItem> items = new Queue<WorkItem>();
        private SynchronizationContext context;
        private readonly SendOrPostCallback loaderCompleted;
        #endregion

        #region WorkItem Class
        /// <summary>
        /// Represents a work item in the thread queue.
        /// </summary>
        private struct WorkItem
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
        /// Initializes a new instance of the <see cref="ImageLoader"/> class.
        /// </summary>
        public ImageLoader()
        {
            // The loader complete callback
            loaderCompleted = new SendOrPostCallback(this.ImageLoaderCompletedCallback);

            // Start the thread
            thread = new Thread(new ThreadStart(DoWork));
            thread.IsBackground = true;
            thread.Start();
            while (!thread.IsAlive) ;
        }
        #endregion

        #region Load Async
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
            if (context == null)
                context = SynchronizationContext.Current;

            lock (lockObject)
            {
                items.Enqueue(new WorkItem(key, filename, size, useEmbeddedThumbnails, autoRotate));
                Monitor.Pulse(lockObject);
            }
        }
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
            if (context == null)
                context = SynchronizationContext.Current;

            lock (lockObject)
            {
                items.Enqueue(new WorkItem(key, image, size, useEmbeddedThumbnails, autoRotate));
                Monitor.Pulse(lockObject);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether the image loader is being stopped.
        /// </summary>
        private bool Stopping { get { lock (lockObject) { return stopping; } } }
        #endregion

        #region Cancel
        /// <summary>
        /// Stops processing items and cancels pending operations.
        /// </summary>
        public void CancelAsync()
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
        private void ImageLoaderCompletedCallback(object arg)
        {
            OnImageLoaderCompleted((ImageLoaderCompletedEventArgs)arg);
        }
        /// <summary>
        /// Raises the ImageLoaderCompleted event.
        /// </summary>
        /// <param name="e">An ImageLoaderCompletedEventArgs that contains event data.</param>
        protected virtual void OnImageLoaderCompleted(ImageLoaderCompletedEventArgs e)
        {
            if (ImageLoaderCompleted != null)
                ImageLoaderCompleted(this, e);
        }
        #endregion

        #region Public Events
        /// <summary>
        /// Occurs when a load operation is completed.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when a load operation is completed.")]
        public event ImageLoaderCompletedEventHandler ImageLoaderCompleted;
        #endregion

        #region Worker Method
        /// <summary>
        /// Used by the worker thread to load images.
        /// </summary>
        private void DoWork()
        {
            while (!Stopping)
            {
                WorkItem request = new WorkItem();

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
                    lock (lockObject)
                    {
                        if (items.Count != 0)
                            request = items.Dequeue();
                    }

                    Image thumb = null;
                    Exception error = null;

                    // Read thumbnail image
                    try
                    {
                        if (!string.IsNullOrEmpty(request.FileName))
                        {
                            thumb = ThumbnailExtractor.FromFile(request.FileName,
                                request.Size, request.UseEmbeddedThumbnails, request.AutoRotate);
                        }
                        else if (request.Image != null)
                        {
                            thumb = ThumbnailExtractor.FromImage(request.Image,
                                request.Size, request.UseEmbeddedThumbnails, request.AutoRotate);
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    // Raise the image loaded event
                    ImageLoaderCompletedEventArgs result = new ImageLoaderCompletedEventArgs(request.Key, thumb, error);
                    context.Post(loaderCompleted, result);

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
