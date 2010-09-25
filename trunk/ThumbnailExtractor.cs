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
//
// WIC support coded by Jens

using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Media;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Extracts thumbnails from images.
    /// </summary>
    internal static class ThumbnailExtractor
    {
        #region Public Methods
        /// <summary>
        /// Creates a thumbnail from the given image.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="size">Requested image size.</param>
        /// <param name="useExifOrientation">true to automatically rotate images based on Exif orientation; otherwise false.</param>
        /// <returns>The thumbnail image from the given image or null if an error occurs.</returns>
        public static Image FromImage(Image image, Size size, bool useExifOrientation)
        {
            if (size.Width <= 0 || size.Height <= 0)
                throw new ArgumentException();

            MemoryStream stream = null;
            BitmapFrame frameWpf = null;
            try
            {
                stream = new MemoryStream();

                image.Save(stream, ImageFormat.MemoryBmp);
                // Performance vs image quality settings.
                // Selecting BitmapCacheOption.None speeds up thumbnail generation of large images tremendously
                // if the file contains no embedded thumbnail. The image quality is only slightly worse.
                frameWpf = BitmapFrame.Create(stream,
                    BitmapCreateOptions.IgnoreColorProfile,
                    BitmapCacheOption.None);
            }
            catch
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                frameWpf = null;
            }

            if (stream == null || frameWpf == null)
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                return null;
            }

            int rotate = 0;
            if (useExifOrientation)
            {
                MetadataExtractor metaInfo = MetadataExtractor.FromBitmap(frameWpf);
                rotate = metaInfo.RotationAngle;
            }

            Image thumb = GetThumbnail(frameWpf, size, UseEmbeddedThumbnails.Auto, rotate);
            stream.Dispose();
            return thumb;
        }
        /// <summary>
        /// Creates a thumbnail from the given image file.
        /// </summary>
        /// <comment>
        /// This much faster .NET 3.0 method replaces the original .NET 2.0 method.
        /// The image quality is slightly reduced (low filtering mode).
        /// </comment>
        /// <param name="filename">The filename pointing to an image.</param>
        /// <param name="size">Requested image size.</param>
        /// <param name="useEmbeddedThumbnails">Embedded thumbnail usage.</param>
        /// <param name="useExifOrientation">true to automatically rotate images based on Exif orientation; otherwise false.</param>
        /// <returns>The thumbnail image from the given file or null if an error occurs.</returns>
        public static Image FromFile(string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool useExifOrientation)
        {
            if (size.Width <= 0 || size.Height <= 0)
                throw new ArgumentException();

            // File can be read and an image is recognized.
            FileStream stream = null;
            BitmapFrame frameWpf = null;
            try
            {
                stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream != null)
                {
                    // Performance vs image quality settings.
                    // Selecting BitmapCacheOption.None speeds up thumbnail generation of large images tremendously
                    // if the file contains no embedded thumbnail. The image quality is only slightly worse.
                    frameWpf = BitmapFrame.Create(stream,
                        BitmapCreateOptions.IgnoreColorProfile,
                        BitmapCacheOption.None);
                }
            }
            catch
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                frameWpf = null;
            }

            if (stream == null || frameWpf == null)
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                return null;
            }


            int rotate = 0;
            if (useExifOrientation)
            {
                MetadataExtractor metaInfo = MetadataExtractor.FromBitmap(frameWpf);
                rotate = metaInfo.RotationAngle;
            }

            Image thumb = GetThumbnail(frameWpf, size, UseEmbeddedThumbnails.Auto, rotate);
            stream.Dispose();
            return thumb;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Creates a  thumbnail from the given bitmap.
        /// </summary>
        /// <param name="bmp">Source bitmap.</param>
        /// <param name="size">Requested image size.</param>
        /// <param name="useEmbeddedThumbnails">Embedded thumbnail usage.</param>
        /// <param name="rotate">Rotation angle in degrees.</param>
        private static Image GetThumbnail(BitmapFrame bmp, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, int rotate)
        {
            Image thumb = null;
            // Try to read the thumbnail.
            if (bmp.Thumbnail != null)
            {
                try
                {
                    BitmapSource sourceWpf = bmp.Thumbnail;
                    double scale = Math.Min(size.Width / (double)sourceWpf.PixelWidth,
                                            size.Height / (double)sourceWpf.PixelHeight);
                    if (bmp.Decoder == null ||
                        (bmp.Decoder.Preview == null && bmp.Decoder.Frames == null) ||
                        useEmbeddedThumbnails == UseEmbeddedThumbnails.Always)
                    {
                        // Take the thumbnail if nothing else is available or if ALWAYS
                        thumb = ConvertToBitmap(ScaleDownRotateBitmap(sourceWpf, scale, rotate));
                    }
                    else if (useEmbeddedThumbnails == UseEmbeddedThumbnails.Auto)
                    {
                        // Check that the embedded thumbnail is large enough.
                        if ((float)scale <= 1.0f)
                        {
                            thumb = ConvertToBitmap(ScaleDownRotateBitmap(sourceWpf, scale, rotate));
                        }
                    }
                }
                catch
                {
                    if (thumb != null)
                    {
                        thumb.Dispose();
                        thumb = null;
                    }
                }
            }

            // Try to read the preview.
            if (bmp.Decoder != null &&
                bmp.Decoder.Preview != null &&
                thumb == null)
            {
                try
                {
                    BitmapSource sourceWpf = bmp.Decoder.Preview;
                    double scale = Math.Min(size.Width / (double)sourceWpf.PixelWidth,
                                            size.Height / (double)sourceWpf.PixelHeight);
                    if (bmp.Decoder.Frames == null ||
                        useEmbeddedThumbnails == UseEmbeddedThumbnails.Always)
                    {
                        // Take the thumbnail if nothing else is available or if ALWAYS
                        thumb = ConvertToBitmap(ScaleDownRotateBitmap(sourceWpf, scale, rotate));
                    }
                    else if (useEmbeddedThumbnails == UseEmbeddedThumbnails.Auto)
                    {
                        // Check that the embedded thumbnail is large enough.
                        if ((float)scale <= 1.0f)
                        {
                            thumb = ConvertToBitmap(ScaleDownRotateBitmap(sourceWpf, scale, rotate));
                        }
                    }
                }
                catch
                {
                    if (thumb != null)
                    {
                        thumb.Dispose();
                        thumb = null;
                    }
                }
            }

            // Use source image if nothings else fits.
            if (bmp.Decoder != null &&
                bmp.Decoder.Frames != null &&
                thumb == null)
            {
                try
                {
                    BitmapSource sourceWpf = bmp.Decoder.Frames[0];
                    double scale = Math.Min(size.Width / (double)sourceWpf.PixelWidth,
                                            size.Height / (double)sourceWpf.PixelHeight);
                    thumb = ConvertToBitmap(ScaleDownRotateBitmap(sourceWpf, scale, rotate));
                }
                catch
                {
                    if (thumb != null)
                    {
                        thumb.Dispose();
                        thumb = null;
                    }
                }
            }

            return thumb;
        }

        /// <summary>
        /// Scales down and rotates a Wpf bitmap.
        /// </summary>
        /// <param name="sourceWpf">Original Wpf bitmap</param>
        /// <param name="scale">Uniform scaling factor</param>
        /// <param name="angle">Rotation angle</param>
        /// <returns>Scaled and rotated Wpf bitmap</returns>
        private static BitmapSource ScaleDownRotateBitmap(BitmapSource sourceWpf, double scale, int angle)
        {
            if (angle % 90 != 0)
            {
                throw new ArgumentException("Rotation angle should be a multiple of 90 degrees.", "angle");
            }

            // Do not upscale and no rotation.
            if ((float)scale >= 1.0f && angle == 0)
            {
                return sourceWpf;
            }

            BitmapSource scaledWpf;
            if ((float)scale < 1.0f) // Only downscale
            {
                double xScale = Math.Max(1.0 / (double)sourceWpf.PixelWidth, scale);
                double yScale = Math.Max(1.0 / (double)sourceWpf.PixelHeight, scale);
                ScaleTransform scaleTransform = new ScaleTransform(xScale, yScale);
                scaledWpf = new TransformedBitmap(sourceWpf, scaleTransform);
            }
            else
            {
                scaledWpf = sourceWpf;
            }
            if (angle != 0)
            {
                // RotateTransform
                RotateTransform rotateTransform = new RotateTransform(angle);
                TransformedBitmap rotatedWpf = new TransformedBitmap(scaledWpf, rotateTransform);

                return BitmapFrame.Create(rotatedWpf);
            }
            else
            {
                return BitmapFrame.Create(scaledWpf);
            }
        }

        /// <summary>
        /// Converts BitmapSource to Bitmap.
        /// </summary>
        /// <param name="sourceWpf">BitmapSource</param>
        /// <returns>Bitmap</returns>
        private static Bitmap ConvertToBitmap(BitmapSource sourceWpf)
        {
            BitmapSource bmpWpf = sourceWpf;

            // PixelFormat settings/conversion
            System.Drawing.Imaging.PixelFormat formatBmp = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            if (sourceWpf.Format == PixelFormats.Bgr24)
            {
                formatBmp = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            }
            else if (sourceWpf.Format == System.Windows.Media.PixelFormats.Pbgra32)
            {
                formatBmp = System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
            }
            else if (sourceWpf.Format != System.Windows.Media.PixelFormats.Bgra32 &&
                     sourceWpf.Format != System.Windows.Media.PixelFormats.Bgr32)
            {
                // Convert BitmapSource
                FormatConvertedBitmap convertWpf = new FormatConvertedBitmap();
                convertWpf.BeginInit();
                convertWpf.Source = sourceWpf;
                convertWpf.DestinationFormat = PixelFormats.Bgra32;
                convertWpf.EndInit();
                bmpWpf = convertWpf;
            }

            // Copy/Convert to Bitmap
            Bitmap bmp = new Bitmap(bmpWpf.PixelWidth, bmpWpf.PixelHeight, formatBmp);
            Rectangle rect = new Rectangle(Point.Empty, bmp.Size);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, formatBmp);
            bmpWpf.CopyPixels(System.Windows.Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }
        #endregion
    }
}
