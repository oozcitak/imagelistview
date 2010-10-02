using System;
using System.Drawing;

namespace Manina.Windows.Forms
{
    public partial class ImageListView
    {
        /// <summary>
        /// Represents an item in the thumbnail cache.
        /// </summary>
        private class ThumbnailCacheItem : IDisposable
        {
            #region Member Variables
            private bool disposed;
            #endregion

            #region Properties
            /// <summary>
            /// Gets the guid of the item.
            /// </summary>
            public Guid Guid { get; private set; }
            /// <summary>
            /// Gets the name of the image file.
            /// </summary>
            public string FileName { get; private set; }
            /// <summary>
            /// Gets the size of the requested thumbnail.
            /// </summary>
            public Size Size { get; private set; }
            /// <summary>
            /// Gets the cached image.
            /// </summary>
            public Image Image { get; private set; }
            /// <summary>
            /// Gets or sets the state of the cache item.
            /// </summary>
            public CacheState State { get; private set; }
            /// <summary>
            /// Gets embedded thumbnail extraction behavior.
            /// </summary>
            public UseEmbeddedThumbnails UseEmbeddedThumbnails { get; private set; }
            /// <summary>
            /// Gets Exif rotation behavior.
            /// </summary>
            public bool AutoRotate { get; private set; }
            /// <summary>
            /// Gets whether this item represents a virtual ImageListViewItem.
            /// </summary>
            public bool IsVirtualItem { get; private set; }
            /// <summary>
            /// Gets the public key for the virtual item.
            /// </summary>
            public object VirtualItemKey { get; private set; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the CacheItem class
            /// for use with a virtual item.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="key">The public key for the virtual item.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            public ThumbnailCacheItem(Guid guid, object key, Size size, Image image, CacheState state)
                : this(guid, key, size, image, state, UseEmbeddedThumbnails.Auto, true)
            {
                ;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class
            /// for use with a virtual item.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="key">The public key for the virtual item.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
            /// <param name="autoRotate">AutoRotate property of the owner control.</param>
            public ThumbnailCacheItem(Guid guid, object key, Size size, Image image, CacheState state, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
            {
                Guid = guid;
                VirtualItemKey = key;
                FileName = string.Empty;
                Size = size;
                Image = image;
                State = state;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
                IsVirtualItem = true;
                disposed = false;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="filename">The file system path to the image file.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            public ThumbnailCacheItem(Guid guid, string filename, Size size, Image image, CacheState state)
                : this(guid, filename, size, image, state, UseEmbeddedThumbnails.Auto, true)
            {
                ;
            }
            /// <summary>
            /// Initializes a new instance of the CacheItem class.
            /// </summary>
            /// <param name="guid">The guid of the ImageListViewItem.</param>
            /// <param name="filename">The file system path to the image file.</param>
            /// <param name="size">The size of the requested thumbnail.</param>
            /// <param name="image">The thumbnail image.</param>
            /// <param name="state">The cache state of the item.</param>
            /// <param name="useEmbeddedThumbnails">UseEmbeddedThumbnails property of the owner control.</param>
            /// <param name="autoRotate">AutoRotate property of the owner control.</param>
            public ThumbnailCacheItem(Guid guid, string filename, Size size, Image image, CacheState state, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
            {
                Guid = guid;
                FileName = filename;
                Size = size;
                Image = image;
                State = state;
                UseEmbeddedThumbnails = useEmbeddedThumbnails;
                AutoRotate = autoRotate;
                IsVirtualItem = false;
                disposed = false;
            }
            #endregion

            #region Dispose
            /// <summary>
            /// Performs application-defined tasks associated with 
            /// freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (!disposed)
                {
                    if (Image != null)
                    {
                        Image.Dispose();
                        Image = null;
                    }

                    disposed = true;
                    GC.SuppressFinalize(this);
                }
            }
            #endregion
        }
    }
}
