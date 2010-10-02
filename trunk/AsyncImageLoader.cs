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
        private bool stopping;
        private bool started;
        private SynchronizationContext context;

        private Queue<WorkItem> items;
        private Queue<WorkItem> priorityItems;
        private Dictionary<object, bool> cancelledItems;

        private readonly SendOrPostCallback loaderCompleted;
        #endregion

        #region WorkItem Class
        /// <summary>
        /// Represents a work item in the thread queue.
        /// </summary>
        private class WorkItem
        {
            #region Properties
            /// <summary>
            /// Gets the key identifying this item.
            /// </summary>
            public object Key { get; private set; }
            /// <summary>
            /// Gets the filename of the image file. Can be null if the
            /// image source is given in the <see cref="Image"/> property
            /// or the image is user supplied.
            /// </summary>
            public string FileName { get; private set; }
            /// <summary>
            /// Gets the source image. Can be null if the
            /// image source is given in the <see cref="FileName"/> property
            /// or the image is user supplied.
            /// </summary>
            public Image Image { get; private set; }
            /// <summary>
            /// Gets the size of the requested image.
            /// </summary>
            public Size Size { get; private set; }
            /// <summary>
            /// Gets embedded thumbnails usage behavior.
            /// </summary>
            public UseEmbeddedThumbnails UseEmbeddedThumbnails { get; private set; }
            /// <summary>
            /// Gets whether the image will be rotated based on Exif
            /// rotation metadata.
            /// </summary>
            public bool AutoRotate { get; private set; }
            /// <summary>
            /// Gets whether the image will be requested from the user by raising a
            /// <see cref="GetUserImage"/> event;
            /// </summary>
            public bool UserSupplied { get; private set; }
            /// <summary>
            /// Gets whether this item will be processed before other items without the
            /// <see cref="IsPriorityItem"/> flag set.
            /// </summary>
            public bool IsPriorityItem { get; private set; }
            #endregion

            #region Constructurs
            /// <summary>
            /// Initializes a new instance of the <see cref="WorkItem"/> class.
            /// </summary>
            /// <param name="key">The key identifying this item.</param>
            /// <param name="filename">The filename of the image file. This parameter can be null.</param>
            /// <param name="image">The source image. This parameter can be null.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnails usage behavior.</param>
            /// <param name="autoRotate">If true the image will be rotated based on Exif
            /// rotation metadata; if false original orientation will be kept.</param>
            /// <param name="isPriorityItem">If true this item will be processed before other items without the
            /// <see cref="IsPriorityItem"/> flag set; if false the item will have normal priority.</param>
            /// <param name="isUserSupplied">If true, the image will be requested from the user by raising a
            /// <see cref="GetUserImage"/> event; if false the image will be extracted from either the <see cref="FileName"/> or
            /// <see cref="Image"/> properties.</param>
            private WorkItem(object key, string filename, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isPriorityItem, bool isUserSupplied)
            {
                Key = key;
                FileName = filename;
                Image = image;
                Size = size;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
                IsPriorityItem = isPriorityItem;
                UserSupplied = isUserSupplied;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="WorkItem"/> class. Thee image will
            /// be sampled from the source image given by the filename parameter.
            /// </summary>
            /// <param name="key">The key identifying this item.</param>
            /// <param name="filename">The filename of the image file.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnails usage behavior.</param>
            /// <param name="autoRotate">If true the image will be rotated based on Exif
            /// rotation metadata; if false original orientation will be kept.</param>
            /// <param name="isPriorityItem">If true this item will be processed before other items without the
            /// <see cref="IsPriorityItem"/> flag set; if false the item will have normal priority.</param>
            public WorkItem(object key, string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isPriorityItem)
                : this(key, filename, null, size, useEmbeddedThumbnails, autoRotate, isPriorityItem, false)
            {
                if (string.IsNullOrEmpty(filename))
                    throw new ArgumentException();
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="WorkItem"/> class. Thee image will
            /// be sampled from the source image given by the image parameter.
            /// </summary>
            /// <param name="key">The key identifying this item.</param>
            /// <param name="image">The source image. This parameter can be null.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnails usage behavior.</param>
            /// <param name="autoRotate">If true the image will be rotated based on Exif
            /// rotation metadata; if false original orientation will be kept.</param>
            /// <param name="isPriorityItem">If true this item will be processed before other items without the
            /// <see cref="IsPriorityItem"/> flag set; if false the item will have normal priority.</param>
            public WorkItem(object key, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isPriorityItem)
                : this(key, null, image, size, useEmbeddedThumbnails, autoRotate, isPriorityItem, false)
            {
                if (image == null)
                    throw new ArgumentException();
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="WorkItem"/> class. Thee image will
            /// be requested from the user by raising a <see cref="GetUserImage"/> event.
            /// </summary>
            /// <param name="key">The key identifying this item.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnails usage behavior.</param>
            /// <param name="autoRotate">If true the image will be rotated based on Exif
            /// rotation metadata; if false original orientation will be kept.</param>
            /// <param name="isPriorityItem">If true this item will be processed before other items without the
            /// <see cref="IsPriorityItem"/> flag set; if false the item will have normal priority.</param>
            public WorkItem(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isPriorityItem)
                : this(key, null, null, size, useEmbeddedThumbnails, autoRotate, isPriorityItem, true)
            {
                ;
            }
            #endregion
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
            context = null;

            thread = new Thread(new ThreadStart(DoWork));
            thread.IsBackground = true;

            // Work items
            items = new Queue<WorkItem>();
            priorityItems = new Queue<WorkItem>();
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
        /// <param name="isPriorityItem">true if this image should be loaded before others in the queue.</param>
        public void LoadAsync(object key, string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isPriorityItem)
        {
            Start();

            lock (lockObject)
            {
                items.Enqueue(new WorkItem(key, filename, size, useEmbeddedThumbnails, autoRotate, isPriorityItem));
                Monitor.Pulse(lockObject);
            }
        }
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
            LoadAsync(key, filename, size, useEmbeddedThumbnails, autoRotate, false);
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
        /// <param name="isPriorityItem">true if this image should be loaded before others in the queue.</param>
        public void LoadAsync(object key, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isPriorityItem)
        {
            Start();

            lock (lockObject)
            {
                items.Enqueue(new WorkItem(key, image, size, useEmbeddedThumbnails, autoRotate, isPriorityItem));
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
            LoadAsync(key, image, size, useEmbeddedThumbnails, autoRotate, false);
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

        #region Load Async User
        /// <summary>
        /// Loads the image with the given key asynchronously.
        /// The user will receive a GetUserImage event and will be 
        /// responsible for returning an image. The event will run
        /// on the worker thread.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        /// <param name="useEmbeddedThumbnails">Embedded Exif thumbnail extraction behaviour.
        /// This parameter is ignored if the size parameter is Size.Empty.</param>
        /// <param name="autoRotate">true to automatically rotate the image based on 
        /// orientation metadata; otherwise false.</param>
        /// <param name="isPriorityItem">true if this image should be loaded before others in the queue.</param>
        public void LoadAsync(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isPriorityItem)
        {
            Start();

            lock (lockObject)
            {
                items.Enqueue(new WorkItem(key, size, useEmbeddedThumbnails, autoRotate, isPriorityItem));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Loads the image with the given key asynchronously.
        /// The user will receive a GetUserImage event and will be 
        /// responsible for returning an image. The event will run
        /// on the worker thread.
        /// </summary>
        /// <param name="key">A object identifying this worker item.</param>
        /// <param name="size">The size of the requested thumbnail. If this parameter is
        /// Size.Empty the original image will be loaded.</param>
        /// <param name="useEmbeddedThumbnails">Embedded Exif thumbnail extraction behaviour.
        /// This parameter is ignored if the size parameter is Size.Empty.</param>
        /// <param name="autoRotate">true to automatically rotate the image based on 
        /// orientation metadata; otherwise false.</param>
        public void LoadAsync(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
        {
            LoadAsync(key, size, useEmbeddedThumbnails, autoRotate, false);
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

            // Get the current synchronization context
            context = SynchronizationContext.Current;

            // Start the thread
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
                if (!cancelledItems.ContainsKey(key))
                {
                    cancelledItems.Add(key, false);
                    Monitor.Pulse(lockObject);
                }
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
        /// Raises the ImageLoaded event.
        /// </summary>
        /// <param name="e">An ImageLoaderCompletedEventArgs that contains event data.</param>
        protected virtual void OnImageLoaderCompleted(AsyncImageLoaderCompletedEventArgs e)
        {
            if (ImageLoaded != null)
                ImageLoaded(this, e);
        }
        /// <summary>
        /// Raises the GetUserImage event.
        /// </summary>
        /// <param name="e">An ImageLoaderCompletedEventArgs that contains event data.</param>
        protected virtual void OnGetUserImage(AsyncImageLoaderGetUserImageEventArgs e)
        {
            if (GetUserImage != null)
                GetUserImage(this, e);
            else
                e.Error = new InvalidOperationException("OnGetUserImage event not handled.");
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Gets the apartment state of the worker thread.
        /// </summary>
        public ApartmentState GetApartmentState()
        {
            return thread.GetApartmentState();
        }
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
        public event AsyncImageLoaderCompletedEventHandler ImageLoaded;
        /// <summary>
        /// Occurs when a user supplied image is requested. This event
        /// will run on the worker thread.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when a user supplied image is requested.")]
        public event AsyncImageLoaderGetUserImageEventHandler GetUserImage;
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
                    if (priorityItems.Count == 0 || items.Count == 0)
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
                        // Check priority queue first
                        if (priorityItems.Count != 0)
                            request = priorityItems.Dequeue();

                        // Check the normal queue
                        if (request == null && items.Count != 0)
                            request = items.Dequeue();

                        // Check if the item was removed
                        if (request != null && cancelledItems.ContainsKey(request.Key))
                            request = null;
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
                            else if (request.UserSupplied)
                            {
                                AsyncImageLoaderGetUserImageEventArgs arg = new AsyncImageLoaderGetUserImageEventArgs(
                                    request.Key, request.Size, request.UseEmbeddedThumbnails, request.AutoRotate);
                                OnGetUserImage(arg);
                                image = arg.Image;
                                error = arg.Error;
                            }
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }

                        // Raise the image loaded event
                        AsyncImageLoaderCompletedEventArgs result =
                            new AsyncImageLoaderCompletedEventArgs(
                                request.Key, request.Size, request.UseEmbeddedThumbnails,
                                request.AutoRotate, image, request.IsPriorityItem, error
                                );
                        context.Post(loaderCompleted, result);
                    }

                    // Check if the cache is exhausted
                    lock (lockObject)
                    {
                        if (priorityItems.Count == 0 && items.Count == 0)
                            queueFull = false;
                    }

                }
            }
        }
        #endregion
    }
}
