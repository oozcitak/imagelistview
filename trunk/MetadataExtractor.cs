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
using System.Windows.Media.Imaging;

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
        const int TagThumbnailData = 0x501B;
        const int TagThumbnailImageWidth = 0x5020;
        const int TagThumbnailImageHeight = 0x5021;
        const int TagImageDescription = 0x010E;
        const int TagEquipmentModel = 0x0110;
        const int TagDateTimeOriginal = 0x9003;
        const int TagArtist = 0x013B;
        const int TagCopyright = 0x8298;
        const int TagExposureTime = 0x829A;
        const int TagFNumber = 0x829D;
        const int TagISOSpeed = 0x8827;
        const int TagShutterSpeed = 0x9201;
        const int TagAperture = 0x9202;
        const int TagUserComment = 0x9286;
        const int TagRating = 0x4746;
        const int TagRatingPercent = 0x4749;
        const int TagEquipmentManufacturer = 0x010F;
        const int TagFocalLength = 0x920A;
        const int TagSoftware = 0x0131;
        const int TagOrientation = 0x0112;
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
        /// Image dimension (width x height).
        /// </summary>
        public int Dimension = 0;
        /// <summary>
        /// Orientation flag.
        /// </summary>
        public int Orientation = 0;
        /// <summary>
        /// Rotation angle (derived from orientation flag).
        /// </summary>
        public int RotationAngle = 0;
        /// <summary>
        /// Date taken.
        /// </summary>
        public DateTime DateTaken = DateTime.MinValue;
        /// <summary>
        /// Date taken as formatted string (null = not available).
        /// </summary>
        public string DateTakenString = null;
        /// <summary>
        /// Image description (null = not available).
        /// </summary>
        public string ImageDescription = null;
        /// <summary>
        /// Software used (null = not available).
        /// </summary>
        public string Software = null;
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
        /// Iso speed rating as string (null = not available).
        /// </summary>
        public string ISOSpeedString = null;
        /// <summary>
        /// Exposure time.
        /// </summary>
        public double ExposureTime = 0.0;
        /// <summary>
        /// Exposure time as rounded and formatted string (null = not available).
        /// </summary>
        public string ExposureTimeString = null;
        /// <summary>
        /// Shutter speed.
        /// </summary>
        public double ShutterSpeed = 0.0;
        /// <summary>
        /// Sutter speed rounded and formatted string (null = not available).
        /// </summary>
        public string ShutterSpeedString = null;
        /// <summary>
        /// F number.
        /// </summary>
        public double FNumber = 0.0;
        /// <summary>
        /// F number as rounded string (null = not available).
        /// </summary>
        public string FNumberString = null;
        /// <summary>
        /// Focal length.
        /// </summary>
        public double FocalLength = 0.0;
        /// <summary>
        /// Focal length as rounded string (null = not available).
        /// </summary>
        public string FocalLengthString = null;
        /// <summary>
        /// Copyright information (null = not available).
        /// </summary>
        public string Copyright = null;
        /// <summary>
        /// Rating value between 0-99.
        /// </summary>
        public int Rating = 0;
        /// <summary>
        /// Aperture value.
        /// </summary>
        public double ApertureValue = 0.0;
        /// <summary>
        /// Sutter speed rounded and formatted string (null = not available).
        /// </summary>
        public string ApertureValueString = null;
        /// <summary>
        /// User comment (null = not available).
        /// </summary>
        public string Comment = null;
        #endregion

        #region Helper Methods
        /// <summary>
        /// Inits metadata via WIC/WPF (.NET 3.0).
        /// If WIC lacks a Metadata-Reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="path">Filepath of image</param>
        private void InitViaWpf(string path)
        {
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
        /// <summary>
        /// Inits metadata via WIC/WPF (.NET 3.0).
        /// If WIC lacks a Metadata-Reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="frameWpf">Opened WPF image</param>
        private void InitViaWpf(BitmapFrame frameWpf)
        {
            Width = frameWpf.PixelWidth;
            Height = frameWpf.PixelHeight;
            DPIX = frameWpf.DpiX;
            DPIY = frameWpf.DpiY;
            Dimension = Width * Height;

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
                    try
                    {
                        Image img = null;
                        using (MemoryStream stream = new MemoryStream())
                        {
                            BmpBitmapEncoder e = new BmpBitmapEncoder();
                            e.Frames.Add(BitmapFrame.Create(frameWpf));
                            e.Save(stream);
                            img = Image.FromStream(stream);
                        }
                        // WIC lacks a metadata reader.
                        // Fall back to .NET 2.0 method.
                        if (img != null)
                            InitViaBmp(img);
                    }
                    catch (Exception eBmp)
                    {
                        Error = eBmp;
                    }
                }
            }
        }
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
            Dimension = Width * Height;

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
                    case TagSoftware:
                        str = ExifAscii(prop.Value).Trim();
                        if (str != String.Empty)
                        {
                            Software = str;
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
                            DateTakenString = dateTime.ToString("g");
                        }
                        break;
                    case TagExposureTime:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            ExposureTime = dVal;
                            if ((float)dVal >= 1.0f)
                                ExposureTimeString = Math.Round(dVal, 1).ToString();
                            else
                                ExposureTimeString = "1/" + Math.Round(1.0 / dVal);
                        }
                        break;
                    case TagFNumber:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            FNumber = dVal;
                            FNumberString = Math.Round(dVal, 1).ToString();
                        }
                        break;
                    case TagFocalLength:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            FocalLength = dVal;
                            FocalLengthString = (Math.Round(dVal, 1)).ToString();
                        }
                        break;
                    case TagISOSpeed:
                        iVal = ExifUShort(prop.Value);
                        if (iVal != 0)
                        {
                            ISOSpeed = iVal;
                            ISOSpeedString = iVal.ToString();
                        }
                        break;
                    case TagOrientation:
                        iVal = ExifUShort(prop.Value);
                        if (iVal != 0)
                        {
                            Orientation = iVal;
                            RotationAngle = GetRotationAngle(iVal);
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
                    case TagShutterSpeed:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            ShutterSpeed = dVal;
                            if ((float)dVal >= 1.0f)
                                ShutterSpeedString = Math.Round(dVal, 1).ToString();
                            else
                                ShutterSpeedString = "1/" + Math.Round(1.0 / dVal);
                        }
                        break;
                    case TagAperture:
                        dVal = ExifDouble(prop.Value);
                        if (dVal != 0.0)
                        {
                            ApertureValue = dVal;
                            if ((float)dVal >= 1.0f)
                                ApertureValueString = Math.Round(dVal, 1).ToString();
                            else
                                ApertureValueString = "1/" + Math.Round(1.0 / dVal);
                        }
                        break;
                    case TagUserComment:
                        str = ExifAscii(prop.Value);
                        if (str != String.Empty)
                        {
                            Comment = str;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Read metadata via WIC/WPF.
        /// </summary>
        /// <param name="data">metadata</param>
        private void InitViaWpf(BitmapMetadata data)
        {
            double dVal;
            int iVal;
            DateTime dateTime;
            string str;
            Object val;

            val = GetMetadataObject(data, "System.Subject");
            if (val != null)
            {
                str = ((string)val).Trim();
                if (str != String.Empty)
                {
                    ImageDescription = str;
                }
            }
            val = GetMetadataObject(data, "System.ApplicationName");
            if (val != null)
            {
                str = ((string)val).Trim();
                if (str != String.Empty)
                {
                    Software = str;
                }
            }
            val = GetMetadataObject(data, "System.Photo.CameraManufacturer");
            if (val != null)
            {
                str = ((string)val).Trim();
                if (str != String.Empty)
                {
                    EquipmentManufacturer = str;
                }
            }
            val = GetMetadataObject(data, "System.Photo.CameraModel");
            if (val != null)
            {
                str = ((string)val).Trim();
                if (str != String.Empty)
                {
                    EquipmentModel = str;
                }
            }
            val = GetMetadataObject(data, "System.Author");
            if (val != null)
            {
                str = String.Concat((string[])val).Trim();
                if (str != String.Empty)
                {
                    Artist = str;
                }
            }
            val = GetMetadataObject(data, "System.Photo.DateTaken");
            if (val != null)
            {
                dateTime = ConvertFileTime((System.Runtime.InteropServices.ComTypes.FILETIME)val);
                if (dateTime != DateTime.MinValue)
                {
                    DateTaken = dateTime;
                    DateTakenString = dateTime.ToString("g");
                }
            }
            val = GetMetadataObject(data, "System.Photo.ExposureTime");
            if (val != null)
            {
                dVal = (double)val;
                if (dVal != 0.0)
                {
                    ExposureTime = dVal;
                    if ((float)dVal >= 1.0f)
                        ExposureTimeString = Math.Round(dVal, 1).ToString();
                    else
                        ExposureTimeString = "1/" + Math.Round(1.0 / dVal);
                }
            }
            val = GetMetadataObject(data, "System.Photo.FNumber");
            if (val != null)
            {
                dVal = (double)val;
                if (dVal != 0.0)
                {
                    FNumber = dVal;
                    FNumberString = Math.Round(dVal, 1).ToString();
                }
            }
            val = GetMetadataObject(data, "System.Photo.FocalLength");
            if (val != null)
            {
                dVal = (double)val;
                if (dVal != 0.0)
                {
                    FocalLength = dVal;
                    FocalLengthString = (Math.Round(dVal, 1)).ToString();
                }
            }
            val = GetMetadataObject(data, "System.Photo.ISOSpeed");
            if (val != null)
            {
                iVal = (ushort)val;
                if (iVal != 0)
                {
                    ISOSpeed = iVal;
                    ISOSpeedString = iVal.ToString();
                }
            }
            val = GetMetadataObject(data, "System.Photo.Orientation");
            if (val != null)
            {
                iVal = (ushort)val;
                if (iVal != 0)
                {
                    Orientation = iVal;
                    RotationAngle = GetRotationAngle(iVal);
                }
            }
            val = GetMetadataObject(data, "System.Copyright");
            if (val != null)
            {
                str = ((string)val).Trim();
                if (str != String.Empty)
                {
                    Copyright = str;
                }
            }
            val = GetMetadataObject(data, "System.Photo.ShutterSpeed");
            if (val != null)
            {
                dVal = (double)val;
                if (dVal != 0.0)
                {
                    ShutterSpeed = dVal;
                    if ((float)dVal >= 1.0f)
                        ShutterSpeedString = Math.Round(dVal, 1).ToString();
                    else
                        ShutterSpeedString = "1/" + Math.Round(1.0 / dVal);
                }
            }
            val = GetMetadataObject(data, "System.Photo.Aperture");
            if (val != null)
            {
                dVal = (double)val;
                if (dVal != 0.0)
                {
                    ApertureValue = dVal;
                    if ((float)dVal >= 1.0f)
                        ApertureValueString = Math.Round(dVal, 1).ToString();
                    else
                        ApertureValueString = "1/" + Math.Round(1.0 / dVal);
                }
            }
            val = GetMetadataObject(data, "System.Comment");
            if (val != null)
            {
                str = ((string)val).Trim();
                if (str != String.Empty)
                {
                    Comment = str;
                }
            }
            val = GetMetadataObject(data, "System.Rating");
            if (val != null)
            {
                iVal = (ushort)val;
                Rating = iVal;
            }
            if (Rating == 0)
            {
                val = GetMetadataObject(data, "System.SimpleRating");
                if (val != null)
                {
                    iVal = (ushort)val;
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
            }
        }
        /// <summary>
        /// Returns the metadata for the given query.
        /// </summary>
        /// <param name="metadata">The image metadata.</param>
        /// <param name="query">Query string.</param>
        /// <returns>Metadata object or null if the metadata as not found.</returns>
        private object GetMetadataObject(BitmapMetadata metadata, string query)
        {
            object val = null;
            try
            {
                val = metadata.GetQuery(query);
            }
            catch (NotSupportedException)
            {
                val = null;
            }
            return val;
        }
        /// <summary>
        /// Get rotation angle from orientation flag.
        /// </summary>
        /// <param name="orientationFlag">Orientation flag</param>
        /// <returns>Rotation angle</returns>
        private int GetRotationAngle(int orientationFlag)
        {
            if (orientationFlag == 6)
                return 90;
            else if (orientationFlag == 3)
                return 180;
            else if (orientationFlag == 8)
                return 270;
            else
                return 0;
        }

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
        /// If WIC lacks a Metadata-Reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="path">Filepath of image</param>
        public static MetadataExtractor FromFile(string path)
        {
            MetadataExtractor metadata = new MetadataExtractor();
            try
            {
                metadata.InitViaWpf(path);
            }
            catch (Exception e)
            {
                metadata.Error = e;
            }

            return metadata;
        }

        /// <summary>
        /// Creates an instance of the MetadataExtractor class.
        /// Reads metadata via WIC/WPF (.NET 3.0).
        /// If WIC lacks a Metadata-Reader for this image type then fall back to .NET 2.0 method. 
        /// </summary>
        /// <param name="path">Filepath of image</param>
        /// <param name="frameWpf">Opened WPF image</param>
        public static MetadataExtractor FromBitmap(BitmapFrame frameWpf)
        {
            MetadataExtractor metadata = new MetadataExtractor();
            try
            {
                metadata.InitViaWpf(frameWpf);
            }
            catch (Exception e)
            {
                metadata.Error = e;
            }
            return metadata;
        }
        #endregion
    }
}
