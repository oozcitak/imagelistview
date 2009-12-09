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
    /// Represents the method that will handle the ColumnClick event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ColumnClickEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);
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
    public delegate void ThumbnailCachingEventHandler(object sender, ItemEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ThumbnailCached event. 
    /// </summary>
    /// <param name="sender">The ImageListView object that is the source of the event.</param>
    /// <param name="e">A ItemEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void ThumbnailCachedEventHandler(object sender, ItemEventArgs e);
    /// <summary>
    /// Represents the method that will handle the ThumbnailCached event. 
    /// </summary>
    /// <param name="guid">The guid of the item whose thumbnail is cached.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate void ThumbnailCachedEventHandlerInternal(Guid guid);
    #endregion

    #region Internal Delegates
    /// <summary>
    /// Updates item details.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate void UpdateItemDetailsDelegateInternal(ImageListViewItem item,Utility.ShellImageFileInfo info);
    /// <summary>
    /// Determines if the given item is visible.
    /// </summary>
    /// <param name="guid">The guid of the item to check visibility.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate bool CheckItemVisibleDelegateInternal(Guid guid);
    /// <summary>
    /// Gets the guids of visible items.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate Dictionary<Guid, bool> GetVisibleItemsDelegateInternal();
    /// <summary>
    /// Refreshes the owner control.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate void RefreshDelegateInternal();
    #endregion

    #region Event Arguments
    /// <summary>
    /// Represents the event arguments for column related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ColumnEventArgs
    {
        private ImageListView.ImageListViewColumnHeader mColumn;

        /// <summary>
        /// Gets the ImageListViewColumnHeader that is the target of the event.
        /// </summary>
        public ImageListView.ImageListViewColumnHeader Column { get { return mColumn; } }

        public ColumnEventArgs(ImageListView.ImageListViewColumnHeader column)
        {
            mColumn = column;
        }
    }
    /// <summary>
    /// Represents the event arguments for column related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ColumnClickEventArgs
    {
        private ImageListView.ImageListViewColumnHeader mColumn;
        private Point mLocation;
        private MouseButtons mButtons;

        /// <summary>
        /// Gets the ImageListViewColumnHeader that is the target of the event.
        /// </summary>
        public ImageListView.ImageListViewColumnHeader Column { get { return mColumn; } }
        /// <summary>
        /// Gets the coordinates of the cursor.
        /// </summary>
        public Point Location { get { return mLocation; } }
        /// <summary>
        /// Gets the x-coordinates of the cursor.
        /// </summary>
        public int X { get { return mLocation.X; } }
        /// <summary>
        /// Gets the y-coordinates of the cursor.
        /// </summary>
        public int Y { get { return mLocation.Y; } }
        /// <summary>
        /// Gets the state of the mouse buttons.
        /// </summary>
        public MouseButtons Buttons { get { return mButtons; } }

        public ColumnClickEventArgs(ImageListView.ImageListViewColumnHeader column, Point location, MouseButtons buttons)
        {
            mColumn = column;
            mLocation = location;
            mButtons = buttons;
        }
    }
    /// <summary>
    /// Represents the event arguments for item related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ItemEventArgs
    {
        private ImageListViewItem mItem;

        /// <summary>
        /// Gets the ImageListViewItem that is the target of the event.
        /// </summary>
        public ImageListViewItem Item { get { return mItem; } }

        public ItemEventArgs(ImageListViewItem item)
        {
            mItem = item;
        }
    }
    /// <summary>
    /// Represents the event arguments for item related events.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class ItemClickEventArgs
    {
        private ImageListViewItem mItem;
        private Point mLocation;
        private MouseButtons mButtons;

        /// <summary>
        /// Gets the ImageListViewItem that is the target of the event.
        /// </summary>
        public ImageListViewItem Item { get { return mItem; } }
        /// <summary>
        /// Gets the coordinates of the cursor.
        /// </summary>
        public Point Location { get { return mLocation; } }
        /// <summary>
        /// Gets the x-coordinates of the cursor.
        /// </summary>
        public int X { get { return mLocation.X; } }
        /// <summary>
        /// Gets the y-coordinates of the cursor.
        /// </summary>
        public int Y { get { return mLocation.Y; } }
        /// <summary>
        /// Gets the state of the mouse buttons.
        /// </summary>
        public MouseButtons Buttons { get { return mButtons; } }

        public ItemClickEventArgs(ImageListViewItem item, Point location, MouseButtons buttons)
        {
            mItem = item;
            mLocation = location;
            mButtons = buttons;
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

        public LayoutEventArgs(Rectangle itemAreaBounds)
        {
            ItemAreaBounds = itemAreaBounds;
        }
    }
    #endregion
}
