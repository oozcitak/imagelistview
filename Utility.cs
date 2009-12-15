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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing.Imaging;
using System.Text;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Contains utility functions.
    /// </summary>
    public static class Utility
    {
        #region Constants
        private const int PropertyTagThumbnailData = 0x501B;
        private const int PropertyTagThumbnailImageWidth = 0x5020;
        private const int PropertyTagThumbnailImageHeight = 0x5021;
        #endregion

        #region Platform Invoke
        // GetFileAttributesEx
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileAttributesEx(string lpFileName,
            GET_FILEEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FILE_ATTRIBUTE_DATA fileData);

        private enum GET_FILEEX_INFO_LEVELS
        {
            GetFileExInfoStandard,
            GetFileExMaxInfoLevel
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;

            public DateTime Value
            {
                get
                {
                    long longTime = (((long)dwHighDateTime) << 32) | ((uint)dwLowDateTime);
                    return DateTime.FromFileTimeUtc(longTime);
                }
            }
        }
        // SHGetFileInfo
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, FileAttributes dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, SHGFI uFlags);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_TYPE)]
            public string szTypeName;
        };
        private const int MAX_PATH = 260;
        private const int MAX_TYPE = 80;
        [Flags]
        private enum SHGFI : uint
        {
            Icon = 0x000000100,
            DisplayName = 0x000000200,
            TypeName = 0x000000400,
            Attributes = 0x000000800,
            IconLocation = 0x000001000,
            ExeType = 0x000002000,
            SysIconIndex = 0x000004000,
            LinkOverlay = 0x000008000,
            Selected = 0x000010000,
            Attr_Specified = 0x000020000,
            LargeIcon = 0x000000000,
            SmallIcon = 0x000000001,
            OpenIcon = 0x000000002,
            ShellIconSize = 0x000000004,
            PIDL = 0x000000008,
            UseFileAttributes = 0x000000010,
            AddOverlays = 0x000000020,
            OverlayIndex = 0x000000040,
        }
        /// <summary>
        /// Creates a value for use as an lParam parameter in a message.
        /// </summary>
        /// <param name="low">the low-order word of the new value.</param>
        /// <param name="high">the high-order word of the new value.</param>
        /// <returns>Concatenation of low and high as an IntPtr.</returns>
        public static IntPtr MakeLParam(short low, short high)
        {
            return (IntPtr)((((int)low) & 0xffff) | ((((int)high) & 0xffff) << 16));
        }
        /// <summary>
        /// Creates a quadword value from the given low and high-order double words.
        /// </summary>
        /// <param name="low">the low-order dword of the new value.</param>
        /// <param name="high">the high-order dword of the new value.</param>
        /// <returns></returns>
        public static long MakeQWord(int lowPart, int highPart)
        {
            return (long)(((long)lowPart) | (long)(highPart << 32));
        }
        /// <summary>
        /// Creates a quadword value from the given low and high-order double words.
        /// </summary>
        /// <param name="low">the low-order dword of the new value.</param>
        /// <param name="high">the high-order dword of the new value.</param>
        /// <returns></returns>
        public static ulong MakeQWord(uint lowPart, uint highPart)
        {
            return (ulong)(((ulong)lowPart) | (ulong)(highPart << 32));
        }
        #endregion

        #region Text Utilities
        /// <summary>
        /// Formats the given file size in bytes as a human readable string.
        /// </summary>
        public static string FormatSize(long size)
        {
            double mod = 1024;
            double sized = size;

            // string[] units = new string[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB" };
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
            int i;
            for (i = 0; sized > mod; i++)
            {
                sized /= mod;
            }

            return string.Format("{0} {1}", System.Math.Round(sized, 2), units[i]);
        }
        #endregion

        #region Shell Utilities
        //Exif tags
        private const int PropertyTagImageDescription = 0x010E;
        private const int PropertyTagEquipmentModel = 0x0110;
        private const int PropertyTagDateTime = 0x0132;
        private const int PropertyTagArtist = 0x013B;
        private const int PropertyTagCopyright = 0x8298;
        private const int PropertyTagExposureTime = 0x829A;
        private const int PropertyTagFNumber = 0x829D;
        private const int PropertyTagISOSpeed = 0x8827;
        private const int PropertyTagShutterSpeed = 0x9201;
        private const int PropertyTagAperture = 0x9202;
        private const int PropertyTagUserComment = 0x9286;

        /// <summary>
        /// A utility class combining FileInfo with SHGetFileInfo for image files.
        /// </summary>
        public class ShellImageFileInfo
        {
            private static Dictionary<string, string> cachedFileTypes;
            private uint structSize = 0;

            public bool Error { get; private set; }
            public FileAttributes FileAttributes { get; private set; }
            public DateTime CreationTime { get; private set; }
            public DateTime LastAccessTime { get; private set; }
            public DateTime LastWriteTime { get; private set; }
            public string Extension { get; private set; }
            public string DirectoryName { get; private set; }
            public string DisplayName { get; private set; }
            public long Size { get; private set; }
            public string TypeName { get; private set; }
            public Size Dimensions { get; private set; }
            public SizeF Resolution { get; private set; }
            // Exif tags
            public string ImageDescription { get; private set; }
            public string EquipmentModel { get; private set; }
            public DateTime DateTaken { get; private set; }
            public string Artist { get; private set; }
            public string Copyright { get; private set; }
            public string ExposureTime { get; private set; }
            public float FNumber { get; private set; }
            public ushort ISOSpeed { get; private set; }
            public string ShutterSpeed { get; private set; }
            public string ApertureValue { get; private set; }
            public string UserComment { get; private set; }

            public ShellImageFileInfo(string path)
            {
                if (cachedFileTypes == null)
                    cachedFileTypes = new Dictionary<string, string>();

                try
                {
                    FileInfo info = new FileInfo(path);
                    FileAttributes = info.Attributes;
                    CreationTime = info.CreationTime;
                    LastAccessTime = info.LastAccessTime;
                    LastWriteTime = info.LastWriteTime;
                    Size = info.Length;
                    DirectoryName = info.DirectoryName;
                    DisplayName = info.Name;
                    Extension = info.Extension;

                    string typeName;
                    if (!cachedFileTypes.TryGetValue(Extension, out typeName))
                    {
                        SHFILEINFO shinfo = new SHFILEINFO();
                        if (structSize == 0) structSize = (uint)Marshal.SizeOf(shinfo);
                        SHGetFileInfo(path, (FileAttributes)0, out shinfo, structSize, SHGFI.TypeName);
                        typeName = shinfo.szTypeName;
                        cachedFileTypes.Add(Extension, typeName);
                    }
                    TypeName = typeName;
                    using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        using (Image img = Image.FromStream(stream, false, false))
                        {
                            Dimensions = img.Size;
                            Resolution = new SizeF(img.HorizontalResolution, img.VerticalResolution);
                            // Read exif properties
                            foreach (PropertyItem prop in img.PropertyItems)
                            {
                                switch (prop.Id)
                                {
                                    case PropertyTagImageDescription:
                                        ImageDescription = ReadExifAscii(prop.Value);
                                        break;
                                    case PropertyTagEquipmentModel:
                                        EquipmentModel = ReadExifAscii(prop.Value);
                                        break;
                                    case PropertyTagDateTime:
                                        DateTaken = ReadExifDateTime(prop.Value);
                                        break;
                                    case PropertyTagArtist:
                                        Artist = ReadExifAscii(prop.Value);
                                        break;
                                    case PropertyTagCopyright:
                                        Copyright = ReadExifAscii(prop.Value);
                                        break;
                                    case PropertyTagExposureTime:
                                        ExposureTime = ReadExifURational(prop.Value);
                                        break;
                                    case PropertyTagFNumber:
                                        FNumber = ReadExifFloat(prop.Value);
                                        break;
                                    case PropertyTagISOSpeed:
                                        ISOSpeed = ReadExifUShort(prop.Value);
                                        break;
                                    case PropertyTagShutterSpeed:
                                        ShutterSpeed = ReadExifRational(prop.Value);
                                        break;
                                    case PropertyTagAperture:
                                        ApertureValue = ReadExifURational(prop.Value);
                                        break;
                                    case PropertyTagUserComment:
                                        UserComment = ReadExifAscii(prop.Value);
                                        break;
                                }
                            }
                        }
                    }
                    Error = false;
                }
                catch
                {
                    Error = true;
                }
            }
        }
        // Convert Exif types
        private static byte ReadExifByte(byte[] value)
        {
            return value[0];
        }
        private static string ReadExifAscii(byte[] value)
        {
            int len = Array.IndexOf(value, (byte)0);
            if (len == -1) len = value.Length;
            return Encoding.ASCII.GetString(value, 0, len);
        }
        private static DateTime ReadExifDateTime(byte[] value)
        {
            return DateTime.ParseExact(ReadExifAscii(value),
                "yyyy:MM:dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
        }
        private static ushort ReadExifUShort(byte[] value)
        {
            return BitConverter.ToUInt16(value, 0);
        }
        private static uint ReadExifUInt(byte[] value)
        {
            return BitConverter.ToUInt32(value, 0);
        }
        private static int ReadExifInt(byte[] value)
        {
            return BitConverter.ToInt32(value, 0);
        }
        private static string ReadExifURational(byte[] value)
        {
            return BitConverter.ToUInt32(value, 0).ToString() + "/" +
                    BitConverter.ToUInt32(value, 4).ToString();
        }
        private static string ReadExifRational(byte[] value)
        {
            return BitConverter.ToInt32(value, 0).ToString() + "/" +
                    BitConverter.ToInt32(value, 4).ToString();
        }
        private static float ReadExifFloat(byte[] value)
        {
            uint num = BitConverter.ToUInt32(value, 0);
            uint den = BitConverter.ToUInt32(value, 4);
            if (den == 0)
                return 0.0f;
            else
                return (float)num / (float)den;
        }
        #endregion

        #region Graphics Utilities
        /// <summary>
        /// Creates a thumbnail from the given image.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="size">Requested image size.</param>
        /// <param name="backColor">Background color of returned thumbnail.</param>
        /// <returns>The image from the given file or null if an error occurs.</returns>
        public static Image ThumbnailFromImage(Image image, Size size, Color backColor)
        {
            if (size.Width <= 0 || size.Height <= 0)
                throw new ArgumentException();

            Image thumb = null;
            try
            {
                float f = System.Math.Max((float)image.Width / (float)size.Width, (float)image.Height / (float)size.Height);
                if (f < 1.0f) f = 1.0f; // Do not upsize small images
                int width = (int)System.Math.Round((float)image.Width / f);
                int height = (int)System.Math.Round((float)image.Height / f);
                thumb = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(thumb))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.High;

                    using (Brush brush = new SolidBrush(backColor))
                    {
                        g.FillRectangle(Brushes.White, 0, 0, width, height);
                    }

                    g.DrawImage(image, 0, 0, width, height);
                }
            }
            catch
            {
                if (thumb != null)
                    thumb.Dispose();
                thumb = null;
            }

            return thumb;
        }
        /// <summary>
        /// Creates a thumbnail from the given image file.
        /// </summary>
        /// <param name="filename">The filename pointing to an image.</param>
        /// <param name="size">Requested image size.</param>
        /// <param name="useEmbeddedThumbnails">Embedded thumbnail usage.</param>
        /// <param name="backColor">Background color of returned thumbnail.</param>
        /// <param name="useGDI">If true uses GDI instead of GDI+.</param>
        /// <returns>The image from the given file or null if an error occurs.</returns>
        public static Image ThumbnailFromFile(string filename, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, Color backColor)
        {
            if (size.Width <= 0 || size.Height <= 0)
                throw new ArgumentException();

            Image source = null;
            Image thumb = null;
            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    using (Image img = Image.FromStream(stream, false, false))
                    {
                        if (useEmbeddedThumbnails != UseEmbeddedThumbnails.Never)
                        {
                            foreach (int index in img.PropertyIdList)
                            {
                                if (index == PropertyTagThumbnailData)
                                {
                                    // Fetch the embedded thumbnail
                                    byte[] rawImage = img.GetPropertyItem(PropertyTagThumbnailData).Value;
                                    using (MemoryStream memStream = new MemoryStream(rawImage))
                                    {
                                        source = Image.FromStream(memStream);
                                    }
                                    if (useEmbeddedThumbnails == UseEmbeddedThumbnails.Auto)
                                    {
                                        // Check that the embedded thumbnail is large enough.
                                        if (Math.Max((float)source.Width / (float)size.Width,
                                            (float)source.Height / (float)size.Height) < 1.0f)
                                        {
                                            source.Dispose();
                                            source = null;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                // Revert to source image if an embedded thumbnail of required size
                // was not found.
                if (source == null)
                    source = Image.FromFile(filename);

                float f = System.Math.Max((float)source.Width / (float)size.Width, (float)source.Height / (float)size.Height);
                if (f < 1.0f) f = 1.0f; // Do not upsize small images
                int width = (int)System.Math.Round((float)source.Width / f);
                int height = (int)System.Math.Round((float)source.Height / f);
                thumb = new Bitmap(source, width, height);
                using (Graphics g = Graphics.FromImage(thumb))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.Clear(backColor);
                    g.DrawImage(source, 0, 0, width, height);
                }
                source.Dispose();
            }
            catch
            {
                if (thumb != null)
                    thumb.Dispose();
                thumb = null;
            }
            finally
            {
                if (source != null)
                    source.Dispose();
                source = null;
            }

            return thumb;
        }
        /// <summary>
        /// Creates a new image from the given base 64 string.
        /// </summary>
        public static Image ImageFromBase64String(string base64String)
        {
            byte[] imageData = Convert.FromBase64String(base64String);
            MemoryStream memory = new MemoryStream(imageData);
            Image image = Image.FromStream(memory);
            memory.Close();
            return image;
        }
        /// <summary>
        /// Returns the base 64 string representation of the given image.
        /// </summary>
        public static string ImageToBase64String(Image image)
        {
            MemoryStream memory = new MemoryStream();
            image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            string base64String = Convert.ToBase64String(memory.ToArray());
            memory.Close();
            return base64String;
        }
        /// <summary>
        /// Gets a path representing a rounded rectangle.
        /// </summary>
        private static GraphicsPath GetRoundedRectanglePath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddLine(x + radius, y, x + width - radius, y);
            if (radius > 0)
                path.AddArc(x + width - 2 * radius, y, 2 * radius, 2 * radius, 270.0f, 90.0f);
            path.AddLine(x + width, y + radius, x + width, y + height - radius);
            if (radius > 0)
                path.AddArc(x + width - 2 * radius, y + height - 2 * radius, 2 * radius, 2 * radius, 0.0f, 90.0f);
            path.AddLine(x + width - radius, y + height, x + radius, y + height);
            if (radius > 0)
                path.AddArc(x, y + height - 2 * radius, 2 * radius, 2 * radius, 90.0f, 90.0f);
            path.AddLine(x, y + height - radius, x, y + radius);
            if (radius > 0)
                path.AddArc(x, y, 2 * radius, 2 * radius, 180.0f, 90.0f);
            return path;
        }
        /// <summary>
        /// Fills the interior of a rounded rectangle.
        /// </summary>
        public static void FillRoundedRectangle(System.Drawing.Graphics graphics, Brush brush, int x, int y, int width, int height, int radius)
        {
            using (GraphicsPath path = GetRoundedRectanglePath(x, y, width, height, radius))
            {
                graphics.FillPath(brush, path);
            }
        }

        /// <summary>
        /// Fills the interior of a rounded rectangle.
        /// </summary>
        public static void FillRoundedRectangle(System.Drawing.Graphics graphics, Brush brush, float x, float y, float width, float height, float radius)
        {
            FillRoundedRectangle(graphics, brush, (int)x, (int)y, (int)width, (int)height, (int)radius);
        }

        /// <summary>
        /// Fills the interior of a rounded rectangle.
        /// </summary>
        public static void FillRoundedRectangle(System.Drawing.Graphics graphics, Brush brush, Rectangle rect, int radius)
        {
            FillRoundedRectangle(graphics, brush, rect.Left, rect.Top, rect.Width, rect.Height, radius);
        }

        /// <summary>
        /// Fills the interior of a rounded rectangle.
        /// </summary>
        public static void FillRoundedRectangle(System.Drawing.Graphics graphics, Brush brush, RectangleF rect, float radius)
        {
            FillRoundedRectangle(graphics, brush, (int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height, (int)radius);
        }

        /// <summary>
        /// Draws the outline of a rounded rectangle.
        /// </summary>
        public static void DrawRoundedRectangle(System.Drawing.Graphics graphics, Pen pen, int x, int y, int width, int height, int radius)
        {
            using (GraphicsPath path = GetRoundedRectanglePath(x, y, width, height, radius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// Draws the outline of a rounded rectangle.
        /// </summary>
        public static void DrawRoundedRectangle(System.Drawing.Graphics graphics, Pen pen, float x, float y, float width, float height, float radius)
        {
            DrawRoundedRectangle(graphics, pen, (int)x, (int)y, (int)width, (int)height, (int)radius);
        }

        /// <summary>
        /// Draws the outline of a rounded rectangle.
        /// </summary>
        public static void DrawRoundedRectangle(System.Drawing.Graphics graphics, Pen pen, Rectangle rect, int radius)
        {
            DrawRoundedRectangle(graphics, pen, rect.Left, rect.Top, rect.Width, rect.Height, radius);
        }

        /// <summary>
        /// Draws the outline of a rounded rectangle.
        /// </summary>
        public static void DrawRoundedRectangle(System.Drawing.Graphics graphics, Pen pen, RectangleF rect, float radius)
        {
            DrawRoundedRectangle(graphics, pen, (int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height, (int)radius);
        }
        #endregion
    }
}
