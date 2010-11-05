using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Net;

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
            private bool disposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="FileSystemAdaptor"/> class.
            /// </summary>
            public FileSystemAdaptor()
            {
                disposed = false;
            }

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
                if (disposed)
                    return null;

                string filename = (string)key;
                return ThumbnailExtractor.FromFile(filename, size, useEmbeddedThumbnails, useExifOrientation, useWIC);
            }
            /// <summary>
            /// Returns the path to the source image for use in drag operations.
            /// </summary>
            /// <param name="key">Item key.</param>
            public override string GetSourceImage(object key)
            {
                if (disposed)
                    return null;

                string filename = (string)key;
                return filename;
            }
            /// <summary>
            /// Returns the details for the given item.
            /// </summary>
            /// <param name="key">Item key.</param>
            /// <param name="useWIC">true to use Windows Imaging Component; otherwise false.</param>
            /// <returns>An array of tuples containing item details or null if an error occurs.</returns>
            public override Utility.TupleBase[] GetDetails(object key, bool useWIC)
            {
                if (disposed)
                    return null;

                string filename = (string)key;
                List<Utility.TupleBase> details = new List<Utility.TupleBase>();

                // Get file info
                FileInfo info = new FileInfo(filename);
                details.Add(Utility.Tuple.Create(ColumnType.DateCreated, info.CreationTime));
                details.Add(Utility.Tuple.Create(ColumnType.DateAccessed, info.LastAccessTime));
                details.Add(Utility.Tuple.Create(ColumnType.DateModified, info.LastWriteTime));
                details.Add(Utility.Tuple.Create(ColumnType.FileSize, info.Length));
                details.Add(Utility.Tuple.Create(ColumnType.FilePath, info.DirectoryName));

                // Get metadata
                MetadataExtractor metadata = MetadataExtractor.FromFile(filename, useWIC);
                details.Add(Utility.Tuple.Create(ColumnType.Dimensions, new Size(metadata.Width, metadata.Height)));
                details.Add(Utility.Tuple.Create(ColumnType.Resolution, new SizeF((float)metadata.DPIX, (float)metadata.DPIY)));
                details.Add(Utility.Tuple.Create(ColumnType.ImageDescription, metadata.ImageDescription ?? ""));
                details.Add(Utility.Tuple.Create(ColumnType.EquipmentModel, metadata.EquipmentModel ?? ""));
                details.Add(Utility.Tuple.Create(ColumnType.DateTaken, metadata.DateTaken));
                details.Add(Utility.Tuple.Create(ColumnType.Artist, metadata.Artist ?? ""));
                details.Add(Utility.Tuple.Create(ColumnType.Copyright, metadata.Copyright ?? ""));
                details.Add(Utility.Tuple.Create(ColumnType.ExposureTime, (float)metadata.ExposureTime));
                details.Add(Utility.Tuple.Create(ColumnType.FNumber, (float)metadata.FNumber));
                details.Add(Utility.Tuple.Create(ColumnType.ISOSpeed, (ushort)metadata.ISOSpeed));
                details.Add(Utility.Tuple.Create(ColumnType.UserComment, metadata.Comment ?? ""));
                details.Add(Utility.Tuple.Create(ColumnType.Rating, (ushort)metadata.Rating));
                details.Add(Utility.Tuple.Create(ColumnType.Software, metadata.Software ?? ""));
                details.Add(Utility.Tuple.Create(ColumnType.FocalLength, (float)metadata.FocalLength));

                return details.ToArray();
            }
            /// <summary>
            /// Performs application-defined tasks associated with freeing,
            /// releasing, or resetting unmanaged resources.
            /// </summary>
            public override void Dispose()
            {
                disposed = true;
            }
        }
        #endregion

        #region URIAdaptor
        /// <summary>
        /// Represents a URI adaptor.
        /// </summary>
        public class URIAdaptor : ImageListView.ImageListViewItemAdaptor
        {
            private bool disposed;
            private WebClient client;

            /// <summary>
            /// Returns the <see cref="WebClient"/>.
            /// </summary>
            private WebClient Client
            {
                get
                {
                    if (client == null)
                        client = new WebClient();
                    return client;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="URIAdaptor"/> class.
            /// </summary>
            public URIAdaptor()
            {
                disposed = false;
                client = null;
            }

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
                if (disposed)
                    return null;

                string uri = (string)key;
                byte[] imageData = Client.DownloadData(uri);
                using (MemoryStream stream = new MemoryStream(imageData))
                {
                    using (Image sourceImage = Image.FromStream(stream))
                    {
                        return ThumbnailExtractor.FromImage(sourceImage, size, useEmbeddedThumbnails, useExifOrientation, useWIC);
                    }
                }
            }
            /// <summary>
            /// Returns the path to the source image for use in drag operations.
            /// </summary>
            /// <param name="key">Item key.</param>
            public override string GetSourceImage(object key)
            {
                if (disposed)
                    return null;

                string uri = (string)key;
                string filename = Path.GetTempFileName();
                Client.DownloadFile(uri,filename);
                return filename;
            }
            /// <summary>
            /// Returns the details for the given item.
            /// </summary>
            /// <param name="key">Item key.</param>
            /// <param name="useWIC">true to use Windows Imaging Component; otherwise false.</param>
            /// <returns>An array of 2-tuples containing item details or null if an error occurs.</returns>
            public override Utility.TupleBase[] GetDetails(object key, bool useWIC)
            {
                if (disposed)
                    return null;

                string uri = (string)key;
                List<Utility.TupleBase> details = new List<Utility.TupleBase>();

                details.Add(Utility.Tuple.Create(ColumnType.Custom, "URL", uri));

                return details.ToArray();
            }
            /// <summary>
            /// Performs application-defined tasks associated with freeing,
            /// releasing, or resetting unmanaged resources.
            /// </summary>
            public override void Dispose()
            {
                disposed = true;
                if(client!=null)
                    client.Dispose();
                client = null;
            }
        }
        #endregion

    }
}
