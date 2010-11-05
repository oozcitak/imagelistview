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
            public override Utility.Tuple<ColumnType, object>[] GetDetails(object key, bool useWIC)
            {
                string filename = (string)key;
                List<Utility.Tuple<ColumnType, object>> details = new List<Utility.Tuple<ColumnType, object>>();

                // Get file info
                FileInfo info = new FileInfo(filename);
                details.Add(Utility.Tuple.Create(ColumnType.DateCreated, (object)info.CreationTime));
                details.Add(Utility.Tuple.Create(ColumnType.DateAccessed, (object)info.LastAccessTime));
                details.Add(Utility.Tuple.Create(ColumnType.DateModified, (object)info.LastWriteTime));
                details.Add(Utility.Tuple.Create(ColumnType.FileSize, (object)info.Length));
                details.Add(Utility.Tuple.Create(ColumnType.FilePath, (object)info.DirectoryName));

                // Get metadata
                MetadataExtractor metadata = MetadataExtractor.FromFile(filename, useWIC);
                details.Add(Utility.Tuple.Create(ColumnType.Dimensions, (object)(new Size(metadata.Width, metadata.Height))));
                details.Add(Utility.Tuple.Create(ColumnType.Resolution, (object)(new SizeF((float)metadata.DPIX, (float)metadata.DPIY))));
                details.Add(Utility.Tuple.Create(ColumnType.ImageDescription, (object)metadata.ImageDescription ?? ""));
                details.Add(Utility.Tuple.Create(ColumnType.EquipmentModel, (object)(metadata.EquipmentModel ?? "")));
                details.Add(Utility.Tuple.Create(ColumnType.DateTaken, (object)metadata.DateTaken));
                details.Add(Utility.Tuple.Create(ColumnType.Artist, (object)(metadata.Artist ?? "")));
                details.Add(Utility.Tuple.Create(ColumnType.Copyright, (object)(metadata.Copyright ?? "")));
                details.Add(Utility.Tuple.Create(ColumnType.ExposureTime, (object)(float)metadata.ExposureTime));
                details.Add(Utility.Tuple.Create(ColumnType.FNumber, (object)(float)metadata.FNumber));
                details.Add(Utility.Tuple.Create(ColumnType.ISOSpeed, (object)(ushort)metadata.ISOSpeed));
                details.Add(Utility.Tuple.Create(ColumnType.UserComment, (object)(metadata.Comment ?? "")));
                details.Add(Utility.Tuple.Create(ColumnType.Rating, (object)(ushort)metadata.Rating));
                details.Add(Utility.Tuple.Create(ColumnType.Software, (object)(metadata.Software ?? "")));
                details.Add(Utility.Tuple.Create(ColumnType.FocalLength, (object)(float)metadata.FocalLength));

                return details.ToArray();
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
