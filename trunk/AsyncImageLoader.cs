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
        QueuedBackgroundWorker bw;
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
            /// <param name="isUserSupplied">If true, the image will be requested from the user by raising a
            /// <see cref="GetUserImage"/> event; if false the image will be extracted from either the <see cref="FileName"/> or
            /// <see cref="Image"/> properties.</param>
            private WorkItem(object key, string filename, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool isUserSupplied)
            {
                Key = key;
                FileName = filename;
                Image = image;
                Size = size;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
                UserSupplied = isUserSupplied;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="WorkItem"/> class. The image will
            /// be sampled from the source image given by the filename parameter.
            /// </summary>
            /// <param name="key">The key identifying this item.</param>
            /// <param name="filename">The filename of the image file.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnails usage behavior.</param>
            /// <param name="autoRotate">If true the image will be rotated based on Exif
            /// rotation metadata; if false original orientation will be kept.</param>
            public WorkItem(object key, string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
                : this(key, filename, null, size, useEmbeddedThumbnails, autoRotate, false)
            {
                if (string.IsNullOrEmpty(filename))
                    throw new ArgumentException();
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="WorkItem"/> class. The image will
            /// be sampled from the source image given by the image parameter.
            /// </summary>
            /// <param name="key">The key identifying this item.</param>
            /// <param name="image">The source image. This parameter can be null.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnails usage behavior.</param>
            /// <param name="autoRotate">If true the image will be rotated based on Exif
            /// rotation metadata; if false original orientation will be kept.</param>
            public WorkItem(object key, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
                : this(key, null, image, size, useEmbeddedThumbnails, autoRotate, false)
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
            public WorkItem(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
                : this(key, null, null, size, useEmbeddedThumbnails, autoRotate, true)
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
            bw = new QueuedBackgroundWorker();
            bw.SetApartmentState(ApartmentState.STA);

            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
        }
        #endregion

        #region QueuedBackgroundWorker Events
        /// <summary>
        /// Handles the RunWorkerCompleted event of the QueuedBackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Manina.Windows.Forms.QueuedWorkerCompletedEventArgs"/> 
        /// instance containing the event data.</param>
        void bw_RunWorkerCompleted(object sender, QueuedWorkerCompletedEventArgs e)
        {
            WorkItem request = e.UserState as WorkItem;
            Image image = e.Result as Image;

            // Raise the image loaded event
            AsyncImageLoaderCompletedEventArgs result =
                new AsyncImageLoaderCompletedEventArgs(
                    request.Key, request.Size, request.UseEmbeddedThumbnails,
                    request.AutoRotate, image, (e.Priority == 0 ? false : true), e.Error
                    );
            OnImageLoaderCompleted(result);
        }
        /// <summary>
        /// Handles the DoWork event of the QueuedBackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> 
        /// instance containing the event data.</param>
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkItem request = e.Argument as WorkItem;

            Image image = null;

            // Read the image
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
            }
            
            e.Result = image;
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
        /// <param name="hasPriority">true if this image should be loaded before others in the queue.</param>
        public void LoadAsync(object key, string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool hasPriority)
        {
            WorkItem item = new WorkItem(key, filename, size, useEmbeddedThumbnails, autoRotate);
            bw.RunWorkerAsync(item, (hasPriority ? 1 : 0));
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
        /// <param name="hasPriority">true if this image should be loaded before others in the queue.</param>
        public void LoadAsync(object key, Image image, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool hasPriority)
        {
            WorkItem item = new WorkItem(key, image, size, useEmbeddedThumbnails, autoRotate);
            bw.RunWorkerAsync(item, (hasPriority ? 1 : 0));
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
        /// <param name="hasPriority">true if this image should be loaded before others in the queue.</param>
        public void LoadAsync(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, bool hasPriority)
        {
            WorkItem item = new WorkItem(key, size, useEmbeddedThumbnails, autoRotate);
            bw.RunWorkerAsync(item, (hasPriority ? 1 : 0));
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

        #region Cancel and Stop
        /// <summary>
        /// Cancels all pending operations.
        /// </summary>
        public void CancelAllAsync()
        {
            bw.CancelAllAsync();
        }
        /// <summary>
        /// Cancels all pending operations and stops the worker thread.
        /// </summary>
        public void Stop()
        {
            bw.Stop();
        }
        #endregion

        #region Virtual Methods
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
    }
}
