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
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.Generic;


namespace Manina.Windows.Forms
{
    #region Event Delegates
    /// <summary>
    /// Represents the method that will handle the CacheError event.
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A CacheErrorEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void CacheErrorEventHandler(object sender, CacheErrorEventArgs e);
    /// <summary>
    /// Represents the method that will handle the DropFiles event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A DropFileEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void DropFilesEventHandler(object sender, DropFileEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ColumnClick event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ColumnClickEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ColumnHover event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ColumnHoverEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ColumnHoverEventHandler(object sender, ColumnHoverEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ColumnWidthChanged event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ColumnEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ColumnWidthChangedEventHandler(object sender, ColumnEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ItemClick event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ItemClickEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ItemClickEventHandler(object sender, ItemClickEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ItemCheckBoxClick event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ItemEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ItemCheckBoxClickEventHandler(object sender, ItemEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ItemHover event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ItemHoverEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ItemHoverEventHandler(object sender, ItemHoverEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ItemDoubleClick event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ItemClickEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ItemDoubleClickEventHandler(object sender, ItemClickEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ThumbnailCaching event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ItemEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ThumbnailCachingEventHandler(object sender, ThumbnailCachingEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ThumbnailCached event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ThumbnailCachedEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ThumbnailCachedEventHandler(object sender, ThumbnailCachedEventArgs e);
    /// <summary>
    /// Represents the method that will handle the RetrieveVirtualItemThumbnail event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A VirtualItemThumbnailEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void RetrieveVirtualItemThumbnailEventHandler(object sender, VirtualItemThumbnailEventArgs e);
    /// <summary>
    /// Represents the method that will handle the RetrieveVirtualItemImage event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A VirtualItemImageEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void RetrieveVirtualItemImageEventHandler(object sender, VirtualItemImageEventArgs e);
    /// <summary>
    /// Represents the method that will handle the RetrieveVirtualItemDetails event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A VirtualItemDetailsEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void RetrieveVirtualItemDetailsEventHandler(object sender, VirtualItemDetailsEventArgs e);
    /// <summary>
    /// Refreshes the owner control.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate void RefreshDelegateInternal();
    #endregion

    #region Event Arguments
    /// <summary>
    /// Represents the event arguments for errors during cache operations.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class CacheErrorEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewItem that is associated with this error.
        /// This parameter can be null.
        /// </summary>
        public ImageListViewItem Item { get; private set; }
        /// <summary>
        /// Gets a value indicating which error occurred during an asynchronous operation.
        /// </summary>
        public Exception Error { get; private set; }
        /// <summary>
        /// Gets the thread raising the error.
        /// </summary>
        public CacheThread CacheThread { get; private set; }

        /// <summary>
        /// Initializes a new instance of the CacheErrorEventArgs class.
        /// </summary>
        /// <param name="item">The ImageListViewItem that is associated with this error.</param>
        /// <param name="error">The error that occurred during an asynchronous operation.</param>
        /// <param name="cacheThread">The thread raising the error.</param>
        public CacheErrorEventArgs(ImageListViewItem item, Exception error, CacheThread cacheThread)
        {
            Item = item;
            Error = error;
            CacheThread = cacheThread;
        }
    }
    /// <summary>
    /// Represents the event arguments for column related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class DropFileEventArgs
    {
        /// <summary>
        /// Gets or sets whether default event code will be processed.
        /// When set to true, the control will automatically insert the new items.
        /// Otherwise, the control will not process the dropped files.
        /// </summary>
        public bool Cancel { get; set; }
        /// <summary>
        /// Gets the position of the insertion caret.
        /// This determines where the new items will be inserted.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Gets the array of filenames droppped on the control.
        /// </summary>
        public string[] FileNames { get; private set; }

        /// <summary>
        /// Initializes a new instance of the DropFileEventArgs class.
        /// </summary>
        /// <param name="index">The position of the insertion caret.</param>
        /// <param name="fileNames">The array of filenames droppped on the control.</param>
        public DropFileEventArgs(int index, string[] fileNames)
        {
            Cancel = false;
            Index = index;
            FileNames = fileNames;
        }
    }
    /// <summary>
    /// Represents the event arguments for column related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ColumnEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewColumnHeader that is the target of the event.
        /// </summary>
        public ImageListView.ImageListViewColumnHeader Column { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ColumnEventArgs class.
        /// </summary>
        /// <param name="column">The column that is the target of this event.</param>
        public ColumnEventArgs(ImageListView.ImageListViewColumnHeader column)
        {
            Column = column;
        }
    }
    /// <summary>
    /// Represents the event arguments for column click related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ColumnClickEventArgs
    {

        /// <summary>
        /// Gets the ImageListViewColumnHeader that is the target of the event.
        /// </summary>
        public ImageListView.ImageListViewColumnHeader Column { get; private set; }
        /// <summary>
        /// Gets the coordinates of the cursor.
        /// </summary>
        public Point Location { get; private set; }
        /// <summary>
        /// Gets the x-coordinates of the cursor.
        /// </summary>
        public int X { get { return Location.X; } }
        /// <summary>
        /// Gets the y-coordinates of the cursor.
        /// </summary>
        public int Y { get { return Location.Y; } }
        /// <summary>
        /// Gets the state of the mouse buttons.
        /// </summary>
        public MouseButtons Buttons { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ColumnClickEventArgs class.
        /// </summary>
        /// <param name="column">The column that is the target of this event.</param>
        /// <param name="location">The location of the mouse.</param>
        /// <param name="buttons">One of the System.Windows.Forms.MouseButtons values 
        /// indicating which mouse button was pressed.</param>
        public ColumnClickEventArgs(ImageListView.ImageListViewColumnHeader column, Point location, MouseButtons buttons)
        {
            Column = column;
            Location = location;
            Buttons = buttons;
        }
    }
    /// <summary>
    /// Represents the event arguments for column hover related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ColumnHoverEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewColumnHeader that was previously hovered.
        /// Returns null if there was no previously hovered column.
        /// </summary>
        public ImageListView.ImageListViewColumnHeader PreviousColumn { get; private set; }
        /// <summary>
        /// Gets the currently hovered ImageListViewColumnHeader.
        /// Returns null if there is no hovered column.
        /// </summary>
        public ImageListView.ImageListViewColumnHeader Column { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ColumnHoverEventArgs class.
        /// </summary>
        /// <param name="column">The currently hovered column.</param>
        /// <param name="previousColumn">The previously hovered column.</param>
        public ColumnHoverEventArgs(ImageListView.ImageListViewColumnHeader column, ImageListView.ImageListViewColumnHeader previousColumn)
        {
            Column = column;
            PreviousColumn = previousColumn;
        }
    }
    /// <summary>
    /// Represents the event arguments for item related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ItemEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewItem that is the target of the event.
        /// </summary>
        public ImageListViewItem Item { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ItemEventArgs class.
        /// </summary>
        /// <param name="item">The item that is the target of this event.</param>
        public ItemEventArgs(ImageListViewItem item)
        {
            Item = item;
        }
    }
    /// <summary>
    /// Represents the event arguments for item click related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ItemClickEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewItem that is the target of the event.
        /// </summary>
        public ImageListViewItem Item { get; private set; }
        /// <summary>
        /// Gets the index of the sub item under the hit point.
        /// The index returned is the 0-based index of the column
        /// as displayed on the screen, considering column visibility
        /// and display indices.
        /// Returns -1 if the hit point is not over a sub item.
        /// </summary>
        public int SubItemIndex { get; private set; }
        /// <summary>
        /// Gets the coordinates of the cursor.
        /// </summary>
        public Point Location { get; private set; }
        /// <summary>
        /// Gets the x-coordinates of the cursor.
        /// </summary>
        public int X { get { return Location.X; } }
        /// <summary>
        /// Gets the y-coordinates of the cursor.
        /// </summary>
        public int Y { get { return Location.Y; } }
        /// <summary>
        /// Gets the state of the mouse buttons.
        /// </summary>
        public MouseButtons Buttons { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ItemClickEventArgs class.
        /// </summary>
        /// <param name="item">The item that is the target of this event.</param>
        /// <param name="subItemIndex">Gets the index of the sub item under the hit point.</param>
        /// <param name="location">The location of the mouse.</param>
        /// <param name="buttons">One of the System.Windows.Forms.MouseButtons values 
        /// indicating which mouse button was pressed.</param>
        public ItemClickEventArgs(ImageListViewItem item, int subItemIndex, Point location, MouseButtons buttons)
        {
            Item = item;
            SubItemIndex = subItemIndex;
            Location = location;
            Buttons = buttons;
        }
    }
    /// <summary>
    /// Represents the event arguments for item hover related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ItemHoverEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewItem that was previously hovered.
        /// Returns null if there was no previously hovered item.
        /// </summary>
        public ImageListViewItem PreviousItem { get; private set; }
        /// <summary>
        /// Gets the currently hovered ImageListViewItem.
        /// Returns null if there is no hovered item.
        /// </summary>
        public ImageListViewItem Item { get; private set; }
        /// <summary>
        /// Gets the index of the sub item that was previously hovered.
        /// The index returned is the 0-based index of the column
        /// as displayed on the screen, considering column visibility
        /// and display indices.
        /// Returns -1 if the hit point is not over a sub item.
        /// </summary>
        public int PreviousSubItemIndex { get; private set; }
        /// <summary>
        /// Gets the index of the hovered sub item.
        /// The index returned is the 0-based index of the column
        /// as displayed on the screen, considering column visibility
        /// and display indices.
        /// Returns -1 if the hit point is not over a sub item.
        /// </summary>
        public int SubItemIndex { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ItemEventArgs class.
        /// </summary>
        /// <param name="item">The currently hovered item.</param>
        /// <param name="subItemIndex">The index of the hovered sub item.</param>
        /// <param name="previousItem">The previously hovered item.</param>
        /// <param name="previousSubItemIndex">The index of the sub item that was previously hovered.</param>
        public ItemHoverEventArgs(ImageListViewItem item, int subItemIndex, ImageListViewItem previousItem, int previousSubItemIndex)
        {
            Item = item;
            SubItemIndex = subItemIndex;

            PreviousItem = previousItem;
            PreviousSubItemIndex = previousSubItemIndex;
        }
    }
    /// <summary>
    /// Represents the event arguments related to control layout.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class LayoutEventArgs
    {
        /// <summary>
        /// Gets or sets the rectangle bounding the item area.
        /// </summary>
        public Rectangle ItemAreaBounds { get; set; }

        /// <summary>
        /// Initializes a new instance of the LayoutEventArgs class.
        /// </summary>
        /// <param name="itemAreaBounds">The rectangle bounding the item area.</param>
        public LayoutEventArgs(Rectangle itemAreaBounds)
        {
            ItemAreaBounds = itemAreaBounds;
        }
    }
    /// <summary>
    /// Represents the event arguments for the thumbnail caching event.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ThumbnailCachingEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewItem that is the target of the event.
        /// </summary>
        public ImageListViewItem Item { get; private set; }
        /// <summary>
        /// Gets the size of the thumbnail request.
        /// </summary>
        public Size Size { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ItemEventArgs class.
        /// </summary>
        /// <param name="item">The item that is the target of this event.</param>
        /// <param name="size">The size of the thumbnail request.</param>
        public ThumbnailCachingEventArgs(ImageListViewItem item, Size size)
        {
            Item = item;
            Size = size;
        }
    }
    /// <summary>
    /// Represents the event arguments for the thumbnail cached event.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ThumbnailCachedEventArgs
    {
        /// <summary>
        /// Gets the ImageListViewItem that is the target of the event.
        /// </summary>
        public ImageListViewItem Item { get; private set; }
        /// <summary>
        /// Gets the size of the thumbnail request.
        /// </summary>
        public Size Size { get; private set; }
        /// <summary>
        /// Gets the cached thumbnail image.
        /// </summary>
        public Image Thumbnail { get; private set; }
        /// <summary>
        /// Gets whether the cached image is a thumbnail image or
        /// a large image for gallery or pane views.
        /// </summary>
        public bool IsThumbnail { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ItemEventArgs class.
        /// </summary>
        /// <param name="item">The item that is the target of this event.</param>
        /// <param name="thumbnail">The cached thumbnail image.</param>
        /// <param name="size">The size of the thumbnail request.</param>
        /// <param name="thumbnailImage">true if the cached image is a thumbnail image; otherwise false
        /// if the image is a large image for gallery or pane views.</param>
        public ThumbnailCachedEventArgs(ImageListViewItem item, Image thumbnail, Size size, bool thumbnailImage)
        {
            Item = item;
            Thumbnail = thumbnail;
            Size = size;
            IsThumbnail = thumbnailImage;
        }
    }
    /// <summary>
    /// Represents the event arguments related to virtual item 
    /// thumbnail requests.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class VirtualItemThumbnailEventArgs
    {
        /// <summary>
        /// Gets the key of the virtual item.
        /// </summary>
        public object Key { get; private set; }
        /// <summary>
        /// Gets the size of the thumbnail image for the virtual item
        /// represented by Key.
        /// </summary>
        public Size ThumbnailDimensions { get; private set; }
        /// <summary>
        /// Gets or sets the thumbnail image for the virtual item
        /// represented by Key.
        /// </summary>
        public Image ThumbnailImage { get; set; }

        /// <summary>
        /// Initializes a new instance of the LayoutEventArgs class.
        /// </summary>
        /// <param name="key">The key of the virtual item.</param>
        /// <param name="thumbnailDimensions">Requested thumbnail pixel dimensions.</param>
        public VirtualItemThumbnailEventArgs(object key, Size thumbnailDimensions)
        {
            Key = key;
            ThumbnailDimensions = thumbnailDimensions;
        }
    }
    /// <summary>
    /// Represents the event arguments related to virtual item images.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class VirtualItemImageEventArgs
    {
        /// <summary>
        /// Gets the key of the virtual item.
        /// </summary>
        public object Key { get; private set; }
        /// <summary>
        /// Gets or sets the full path to the source image for the virtual item
        /// represented by Key.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Initializes a new instance of the LayoutEventArgs class.
        /// </summary>
        /// <param name="key">The key of the virtual item.</param>
        public VirtualItemImageEventArgs(object key)
        {
            Key = key;
        }
    }
    /// <summary>
    /// Represents the event arguments related to virtual item details.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class VirtualItemDetailsEventArgs
    {
        /// <summary>
        /// Gets the key of the virtual item.
        /// </summary>
        public object Key { get; private set; }
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
        /// Gets or sets rating between 0-100.
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

        /// <summary>
        /// Initializes a new instance of the LayoutEventArgs class.
        /// </summary>
        /// <param name="key">The key of the virtual item.</param>
        public VirtualItemDetailsEventArgs(object key)
        {
            Key = key;
        }
    }
    #endregion
}
