using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Manina.Windows.Forms
{
    public partial class ImageListView
    {
        /// <summary>
        /// Represents the abstract case class for adaptors.
        /// </summary>
        public abstract class ImageListViewItemAdaptor : IDisposable
        {
            #region Abstract Methods
            /// <summary>
            /// Returns the thumbnail image for the given item.
            /// </summary>
            /// <param name="key">Item key.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnail usage.</param>
            /// <param name="useExifOrientation">true to automatically rotate images based on Exif orientation; otherwise false.</param>
            /// <param name="useWIC">true to use Windows Imaging Component; otherwise false.</param>
            /// <returns>The thumbnail image from the given item or null if an error occurs.</returns>
            public abstract Image GetThumbnail(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool useExifOrientation, bool useWIC);
            /// <summary>
            /// Returns the path to the source image for use in drag operations.
            /// </summary>
            /// <param name="key">Item key.</param>
            public abstract string GetSourceImage(object key);
            /// <summary>
            /// Returns the details for the given item.
            /// </summary>
            /// <param name="key">Item key.</param>
            /// <param name="useWIC">true to use Windows Imaging Component; otherwise false.</param>
            /// <returns>An <see cref="ItemDetails"/> containing item details or null if an error occurs.</returns>
            public abstract ItemDetails GetDetails(object key, bool useWIC);
            /// <summary>
            /// Performs application-defined tasks associated with freeing,
            /// releasing, or resetting unmanaged resources.
            /// </summary>
            public abstract void Dispose();
            #endregion

            #region ItemDetails Class
            /// <summary>
            /// Represents item details.
            /// </summary>
            public class ItemDetails
            {
                /// <summary>
                /// Gets or sets the last access date of the image file represented by this item.
                /// </summary>
                public DateTime DateAccessed { get; set; }
                /// <summary>
                /// Gets or sets the creation date of the image file represented by this item.
                /// </summary>
                public DateTime DateCreated { get; set; }
                /// <summary>
                /// Gets or sets the modification date of the image file represented by this item.
                /// </summary>
                public DateTime DateModified { get; set; }
                /// <summary>
                /// Gets or sets the shell type of the image file represented by this item.
                /// </summary>
                public string FileType { get; set; }
                /// <summary>
                /// Gets or sets the name of the image fie represented by this item.
                /// </summary>        
                public string FileName { get; set; }
                /// <summary>
                /// Gets or sets the path of the image fie represented by this item.
                /// </summary>        
                public string FilePath { get; set; }
                /// <summary>
                /// Gets or sets file size in bytes.
                /// </summary>
                public long FileSize { get; set; }
                /// <summary>
                /// Gets or sets image dimensions.
                /// </summary>
                public Size Dimensions { get; set; }
                /// <summary>
                /// Gets or sets image resolution in pixels per inch.
                /// </summary>
                public SizeF Resolution { get; set; }
                /// <summary>
                /// Gets or sets image deascription.
                /// </summary>
                public string ImageDescription { get; set; }
                /// <summary>
                /// Gets or sets the camera model.
                /// </summary>
                public string EquipmentModel { get; set; }
                /// <summary>
                /// Gets or sets the date and time the image was taken.
                /// </summary>
                public DateTime DateTaken { get; set; }
                /// <summary>
                /// Gets or sets the name of the artist.
                /// </summary>
                public string Artist { get; set; }
                /// <summary>
                /// Gets or sets image copyright information.
                /// </summary>
                public string Copyright { get; set; }
                /// <summary>
                /// Gets or sets the exposure time in seconds.
                /// </summary>
                public float ExposureTime { get; set; }
                /// <summary>
                /// Gets or sets the F number.
                /// </summary>
                public float FNumber { get; set; }
                /// <summary>
                /// Gets or sets the ISO speed.
                /// </summary>
                public ushort ISOSpeed { get; set; }
                /// <summary>
                /// Gets or sets the shutter speed.
                /// </summary>
                public string ShutterSpeed { get; set; }
                /// <summary>
                /// Gets or sets user comments.
                /// </summary>
                public string UserComment { get; set; }
                /// <summary>
                /// Gets or sets the rating between 0-100.
                /// </summary>
                public ushort Rating { get; set; }
                /// <summary>
                /// Gets the name of the application that created this file.
                /// </summary>
                public string Software { get; set; }
                /// <summary>
                /// Gets focal length of the lens in millimeters.
                /// </summary>
                public float FocalLength { get; set; }
            }
            #endregion
        }
    }
}
