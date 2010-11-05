using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the built-in adaptors.
    /// </summary>
    public static class ImageListViewItemAdaptors
    {
        #region FileSystemAdaptor
        /// <summary>
        /// Represents a file system adaptor.
        /// </summary>
        public class FileSystemAdaptor : ImageListView.ImageListViewItemAdaptor
        {
            /// <summary>
            /// Returns the thumbnail image for the given item.
            /// </summary>
            /// <param name="key">Item key.</param>
            /// <param name="size">Requested image size.</param>
            /// <param name="useEmbeddedThumbnails">Embedded thumbnail usage.</param>
            /// <param name="useExifOrientation">true to automatically rotate images based on Exif orientation; otherwise false.</param>
            /// <param name="useWIC">true to use Windows Imaging Component; otherwise false.</param>
            /// <returns>The thumbnail image from the given item or null if an error occurs.</returns>
            public override Image GetThumbnail(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool useExifOrientation, bool useWIC)
            {
                string filename = (string)key;
                return ThumbnailExtractor.FromFile(filename, size, useEmbeddedThumbnails, useExifOrientation, useWIC);
            }
            /// <summary>
            /// Returns the path to the source image for use in drag operations.
            /// </summary>
            /// <param name="key">Item key.</param>
            public override string GetSourceImage(object key)
            {
                string filename = (string)key;
                return filename;
            }
            /// <summary>
            /// Returns the details for the given item.
            /// </summary>
            /// <param name="key">Item key.</param>
            /// <param name="useWIC">true to use Windows Imaging Component; otherwise false.</param>
            /// <returns>
            /// An <see cref="ImageListView.ImageListViewItemAdaptor.ItemDetails"/> containing item details or 
            /// null if an error occurs.
            /// </returns>
            public override ItemDetails GetDetails(object key, bool useWIC)
            {
                string filename = (string)key;
                MetadataExtractor extractor = MetadataExtractor.FromFile(filename);
                ItemDetails imageInfo = new ItemDetails();

                // Get file info
                FileInfo info = new FileInfo(filename);
                imageInfo.DateCreated = info.CreationTime;
                imageInfo.DateAccessed = info.LastAccessTime;
                imageInfo.DateModified = info.LastWriteTime;
                imageInfo.FileSize = info.Length;
                imageInfo.FilePath = info.DirectoryName;

                // Get metadata
                MetadataExtractor metadata = MetadataExtractor.FromFile(filename, useWIC);
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

                return imageInfo;
            }
            /// <summary>
            /// Performs application-defined tasks associated with freeing,
            /// releasing, or resetting unmanaged resources.
            /// </summary>
            public override void Dispose()
            {
                ;
            }
        }
        #endregion
    }
}
