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
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
#if USEWIC
using System.Windows.Media.Imaging;
#endif

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Read metadata.
    /// Only EXIF data when using .NET 2.0 methods.
    /// Prioritized EXIF/XMP/ICC/etc. data when using WIC/WPF methods.
    /// </summary>
    internal class MetadataExtractor
    {

        #region Exif Tag IDs
        const int TagImageDescription = 0x010E;
        const int TagEquipmentModel = 0x0110;
        const int TagDateTimeOriginal = 0x9003;
        const int TagArtist = 0x013B;
        const int TagCopyright = 0x8298;
        const int TagExposureTime = 0x829A;
        const int TagFNumber = 0x829D;
        const int TagISOSpeed = 0x8827;
        const int TagUserComment = 0x9286;
        const int TagRating = 0x4746;
        const int TagRatingPercent = 0x4749;
        const int TagEquipmentManufacturer = 0x010F;
        const int TagFocalLength = 0x920A;
        const int TagSoftware = 0x0131;
        #endregion

        #region Exif Format Conversion
        /// <summary>
        /// Converts the given Exif data to a byte.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static byte ExifByte(byte[] value)
        {
            return value[0];
        }
        /// <summary>
        /// Converts the given Exif data to an ASCII encoded string.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static string ExifAscii(byte[] value)
        {
            int len = Array.IndexOf(value, (byte)0);
            if (len == -1) len = value.Length;
            string str = Encoding.ASCII.GetString(value, 0, len);
            return str;
        }
        /// <summary>
        /// Converts the given Exif data to DateTime.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static DateTime ExifDateTime(byte[] value)
        {
            return DateTime.ParseExact(ExifAscii(value),
                "yyyy:MM:dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Converts the given Exif data to DateTime.
        /// </summary>
        /// <param name="value">Exif data as a string.</param>
        private static DateTime ExifDateTime(string value)
        {
            DateTime result = DateTime.MinValue;
            DateTime.TryParse(value, out result);
            return result;
        }
        /// <summary>
        /// Converts the given Exif data to an 16-bit unsigned integer.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static ushort ExifUShort(byte[] value)
        {
            return BitConverter.ToUInt16(value, 0);
        }
        /// <summary>
        /// Converts the given Exif data to an 32-bit unsigned integer.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static uint ExifUInt(byte[] value)
        {
            return BitConverter.ToUInt32(value, 0);
        }
        /// <summary>
        /// Converts the given Exif data to an 32-bit signed integer.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static int ExifInt(byte[] value)
        {
            return BitConverter.ToInt32(value, 0);
        }
        /// <summary>
        /// Converts the given Exif data to an unsigned rational value
        /// represented as a string.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static string ExifURational(byte[] value)
        {
            return BitConverter.ToUInt32(value, 0).ToString() + "/" +
                    BitConverter.ToUInt32(value, 4).ToString();
        }
        /// <summary>
        /// Converts the given Exif data to a signed rational value
        /// represented as a string.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static string ExifRational(byte[] value)
        {
            return BitConverter.ToInt32(value, 0).ToString() + "/" +
                    BitConverter.ToInt32(value, 4).ToString();
        }
        /// <summary>
        /// Converts the given Exif data to a double number.
        /// </summary>
        /// <param name="value">Exif data as a byte array.</param>
        private static double ExifDouble(byte[] value)
        {
            uint num = BitConverter.ToUInt32(value, 0);
            uint den = BitConverter.ToUInt32(value, 4);
            if (den == 0)
                return 0.0;
            else
                return num / (double)den;
        }
        #endregion

        #region Metadata properties
        /// <summary>
        /// Error.
        /// </summary>
        public Exception Error = null;
        /// <summary>
        /// Image width.
        /// </summary>
        public int Width = 0;
        /// <summary>
        /// Image height.
        /// </summary>
        public int Height = 0;
        /// <summary>
        /// Horizontal DPI.
        /// </summary>
        public double DPIX = 0.0;
        /// <summary>
        /// Vertical DPI.
        /// </summary>
        public double DPIY = 0.0;
        /// <summary>
        /// Date taken.
        /// </summary>
        public DateTime DateTaken = DateTime.MinValue;
        /// <summary>
        /// Image description (null = not available).
        /// </summary>
        public string ImageDescription = null;
        /// <summary>
        /// Camera manufacturer (null = not available).
        /// </summary>
        public string EquipmentManufacturer = null;
        /// <summary>
        /// Camera model (null = not available).
        /// </summary>
        public string EquipmentModel = null;
        /// <summary>
        /// Image creator (null = not available).
        /// </summary>
        public string Artist = null;
        /// <summary>
        /// Iso speed rating.
        /// </summary>
        public int ISOSpeed = 0;
        /// <summary>
        /// Exposure time.
        /// </summary>
        public double ExposureTime = 0.0;
        /// <summary>
        /// F number.
        /// </summary>
        public double FNumber = 0.0;
        /// <summary>
        /// Copyright information (null = not available).
        /// </summary>
        public string Copyright = null;
        /// <summary>
        /// Rating value between 0-99.
        /// </summary>
        public int Rating = 0;
        /// <summary>
        /// User comment (null = not available).
        /// </summary>
        public string Comment = null;
        /// <summary>
        /// Software used (null = not available).
        /// </summary>
        public string Software = null;
        /// <summary>
        /// Focal length.
        /// </summary>
        public double FocalLength = 0.0;
        #endregion

        #region Helper Methods
        /// <summary>
        /// Inits metadata via WIC/WPF (.NET 3.0).
        /// If WIC lacks a metadata reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="path">Filepath of image</param>
        private void InitViaWpf(string path)
        {
            bool wicError = false;
#if USEWIC
            try
            {
                using (FileStream streamWpf = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BitmapFrame frameWpf = BitmapFrame.Create
                            (streamWpf,
                             BitmapCreateOptions.IgnoreColorProfile,
                             BitmapCacheOption.None);
                    InitViaWpf(frameWpf);
                }
            }
            catch (Exception eWpf)
            {
                Error = eWpf;
                wicError = true;
            }
#else
            wicError = true;
#endif
            if (wicError)
            {
                try
                {
                    // Fall back to .NET 2.0 method.
                    InitViaBmp(path);
                }
                catch (Exception eBmp)
                {
                    Error = eBmp;
                }
            }
        }
#if USEWIC
        /// <summary>
        /// Inits metadata via WIC/WPF (.NET 3.0).
        /// </summary>
        /// <param name="frameWpf">Opened WPF image</param>
        private void InitViaWpf(BitmapFrame frameWpf)
        {
            Width = frameWpf.PixelWidth;
            Height = frameWpf.PixelHeight;
            DPIX = frameWpf.DpiX;
            DPIY = frameWpf.DpiY;

            BitmapMetadata data = frameWpf.Metadata as BitmapMetadata;
            if (data != null)
            {
                try
                {
                    InitViaWpf(data);
                }
                catch (Exception eWpf)
                {
                    Error = eWpf;
                }
            }
        }
#endif
        /// <summary>
        /// Open image and read metadata (.NET 2.0).
        /// </summary>
        /// <param name="path">Filepath of image</param>
        private void InitViaBmp(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (Utility.IsImage(stream))
                {
                    using (Image img = Image.FromStream(stream, false, false))
                    {
                        if (img != null)
                        {
                            InitViaBmp(img);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Read metadata using .NET 2.0 methods.
        /// </summary>
        /// <param name="img">Opened image</param>
        private void InitViaBmp(Image img)
        {
            Width = img.Width;
            Height = img.Height;
            DPIX = img.HorizontalResolution;
            DPIY = img.VerticalResolution;

            double dVal;
            int iVal;
            DateTime dateTime;
            string str;
            foreach (PropertyItem prop in img.PropertyItems)
            {
                switch (prop.Id)
                {
                    case TagImageDescription:
                        str = ExifAscii(prop.Value).Trim();
                        if (str != String.Empty)
                        {
                            ImageDescription = str;
                        }
                        break;
                    case TagArtist:
                        str = ExifAscii(prop.Value).Trim();
                        if (str != String.Empty)
                        {
                            Artist = str;
                        }
                        break;
                    case TagEquipmentManufacturer:
                        str = ExifAscii(prop.Value).Trim();
                        if (str != String.Empty)
                        {
                            EquipmentManufacturer = str;
                        }
                        break;
                    case TagEquipmentModel:
                        str = ExifAscii(prop.Value).Trim();
                        if (str != String.Empty)
                        {
                            EquipmentModel = str;
                        }
                        break;
                    case TagDateTimeOriginal:
                        dateTime = ExifDateTime(prop.Value);
                        if (dateTime != DateTime.MinValue)
                        {
                            DateTaken = dateTime;
                        }
                        break;
                    case TagExposureTime:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            ExposureTime = dVal;
                        }
                        break;
                    case TagFNumber:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            FNumber = dVal;
                        }
                        break;
                    case TagISOSpeed:
                        iVal = ExifUShort(prop.Value);
                        if (iVal != 0)
                        {
                            ISOSpeed = iVal;
                        }
                        break;
                    case TagCopyright:
                        str = ExifAscii(prop.Value);
                        if (str != String.Empty)
                        {
                            Copyright = str;
                        }
                        break;
                    case TagRating:
                        if (Rating == 0)
                        {
                            iVal = ExifUShort(prop.Value);
                            if (iVal == 1)
                                Rating = 1;
                            else if (iVal == 2)
                                Rating = 25;
                            else if (iVal == 3)
                                Rating = 50;
                            else if (iVal == 4)
                                Rating = 75;
                            else if (iVal == 5)
                                Rating = 99;
                        }
                        break;
                    case TagRatingPercent:
                        iVal = ExifUShort(prop.Value);
                        Rating = iVal;
                        break;
                    case TagUserComment:
                        str = ExifAscii(prop.Value);
                        if (str != String.Empty)
                        {
                            Comment = str;
                        }
                        break;
                    case TagSoftware:
                        str = ExifAscii(prop.Value).Trim();
                        if (str != String.Empty)
                        {
                            Software = str;
                        }
                        break;
                    case TagFocalLength:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            FocalLength = dVal;
                        }
                        break;
                }
            }
        }

#if USEWIC
        /// <summary>
        /// Read metadata via WIC/WPF.
        /// </summary>
        /// <param name="data">metadata</param>
        private void InitViaWpf(BitmapMetadata data)
        {
            ImageDescription = data.Subject;
            EquipmentManufacturer = data.CameraManufacturer;
            EquipmentModel = data.CameraModel;
            var authors = data.Author;
            if (authors != null && authors.Count > 0)
            {
                string[] authorsArray = new string[authors.Count];
                authors.CopyTo(authorsArray, 0);
                Artist = string.Join(";", authorsArray).Trim();
            }
            if (data.DateTaken != null)
            {
                DateTaken = DateTime.Parse(data.DateTaken);
            }
            Copyright = data.Copyright;
            Comment = data.Comment;
            Software = data.ApplicationName;
            int simpleRating = data.Rating;
            if (simpleRating == 1)
                Rating = 1;
            else if (simpleRating == 2)
                Rating = 25;
            else if (simpleRating == 3)
                Rating = 50;
            else if (simpleRating == 4)
                Rating = 75;
            else if (simpleRating == 5)
                Rating = 99;

            Object val;

            // Exposure time
            val = GetMetadataObject(data, "/app1/ifd/exif/{ushort=33434}", "/xmp/exif:ExposureTime");
            if (val != null)
                ExposureTime = ExifDouble(BitConverter.GetBytes((ulong)val));
            // FNumber
            val = GetMetadataObject(data, "/app1/ifd/exif/{ushort=33437}", "/xmp/exif:FNumber");
            if (val != null)
                FNumber = ExifDouble(BitConverter.GetBytes((ulong)val));
            // ISOSpeed
            val = GetMetadataObject(data, "/app1/ifd/exif/{ushort=34855}", "/xmp/<xmpseq>exif:ISOSpeedRatings", "/xmp/exif:ISOSpeed");
            if (val != null)
                ISOSpeed = (ushort)val;
            // FocalLength
            val = GetMetadataObject(data, "/app1/ifd/exif/{ushort=37386}", "/xmp/exif:FocalLength");
            if (val != null)
                FocalLength = ExifDouble(BitConverter.GetBytes((ulong)val));
        }
        /// <summary>
        /// Returns the metadata for the given query.
        /// </summary>
        /// <param name="metadata">The image metadata.</param>
        /// <param name="query">A list of query strings.</param>
        /// <returns>Metadata object or null if the metadata as not found.</returns>
        private object GetMetadataObject(BitmapMetadata metadata, params string[] query)
        {
            object val = null;
            foreach (string q in query)
            {
                val = metadata.GetQuery(q);
                if (val != null)
                    return val;
            }
            return val;
        }
#endif
        /// <summary>
        /// Convert FileTime to DateTime.
        /// </summary>
        /// <param name="ft">FileTime</param>
        /// <returns>DateTime</returns>
        private DateTime ConvertFileTime(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            long longTime = (((long)ft.dwHighDateTime) << 32) | ((uint)ft.dwLowDateTime);
            return DateTime.FromFileTimeUtc(longTime); // using UTC???
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the MetadataExtractor class.
        /// </summary>
        private MetadataExtractor()
        {
            ;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates an instance of the MetadataExtractor class.
        /// Reads metadata via WIC/WPF (.NET 3.0).
        /// If WIC lacks a metadata reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="path">Filepath of image</param>
        public static MetadataExtractor FromFile(string path)
        {
            return MetadataExtractor.FromFile(path, false);
        }
        /// <summary>
        /// Creates an instance of the MetadataExtractor class.
        /// Reads metadata via WIC/WPF (.NET 3.0).
        /// If WIC lacks a metadata reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="path">Filepath of image</param>
        /// <param name="useWic">true to use Windows Imaging Component; otherwise false.</param>
        public static MetadataExtractor FromFile(string path, bool useWic)
        {
            MetadataExtractor metadata = new MetadataExtractor();
            if (useWic)
                metadata.InitViaWpf(path);
            else
                metadata.InitViaBmp(path);
            return metadata;
        }
#if USEWIC
        /// <summary>
        /// Creates an instance of the MetadataExtractor class.
        /// Reads metadata via WIC/WPF (.NET 3.0).
        /// If WIC lacks a metadata reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="frameWpf">Opened WPF image</param>
        public static MetadataExtractor FromBitmap(BitmapFrame frameWpf)
        {
            MetadataExtractor metadata = new MetadataExtractor();
            metadata.InitViaWpf(frameWpf);
            return metadata;
        }
#endif
        #endregion
    }
}
