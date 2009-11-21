using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the layout of the image list view drawing area.
    /// </summary>
    internal class ImageListViewLayoutManager
    {
        #region Member Variables
        private Rectangle mClientArea;
        private ImageListView mImageListView;
        private Rectangle mItemAreaBounds;
        private Rectangle mColumnHeaderBounds;
        private Size mItemSize;
        private Size mItemSizeWithMargin;
        private int mCols;
        private int mRows;
        private int mFirstPartiallyVisible;
        private int mLastPartiallyVisible;
        private int mFirstVisible;
        private int mLastVisible;

        private BorderStyle cachedBorderStyle;
        private View cachedView;
        private Point cachedViewOffset;
        private Size cachedSize;
        private int cachedItemCount;
        private Size cachedItemSize;
        private int cachedHeaderHeight;
        private Dictionary<Guid, bool> cachedVisibleItems;

        private bool vScrollVisible;
        private bool hScrollVisible;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the bounds of the entire client area.
        /// </summary>
        public Rectangle ClientArea { get { return mClientArea; } }
        /// <summary>
        /// Gets the owner image list view.
        /// </summary>
        public ImageListView ImageListView { get { return mImageListView; } }
        /// <summary>
        /// Gets the extends of the item area.
        /// </summary>
        public Rectangle ItemAreaBounds { get { return mItemAreaBounds; } }
        /// <summary>
        /// Gets the extents of the column header area.
        /// </summary>
        public Rectangle ColumnHeaderBounds { get { return mColumnHeaderBounds; } }
        /// <summary>
        /// Gets the items size.
        /// </summary>
        public Size ItemSize { get { return mItemSize; } }
        /// <summary>
        /// Gets the items size including the margin around the item.
        /// </summary>
        public Size ItemSizeWithMargin { get { return mItemSizeWithMargin; } }
        /// <summary>
        /// Gets the maximum number of columns that can be displayed.
        /// </summary>
        public int Cols { get { return mCols; } }
        /// <summary>
        /// Gets the maximum number of rows that can be displayed.
        /// </summary>
        public int Rows { get { return mRows; } }
        /// <summary>
        /// Gets the index of the first partially visible item.
        /// </summary>
        public int FirstPartiallyVisible { get { return mFirstPartiallyVisible; } }
        /// <summary>
        /// Gets the index of the last partially visible item.
        /// </summary>
        public int LastPartiallyVisible { get { return mLastPartiallyVisible; } }
        /// <summary>
        /// Gets the index of the first fully visible item.
        /// </summary>
        public int FirstVisible { get { return mFirstVisible; } }
        /// <summary>
        /// Gets the index of the last fully visible item.
        /// </summary>
        public int LastVisible { get { return mLastVisible; } }
        /// <summary>
        /// Determines whether an update is required.
        /// </summary>
        public bool UpdateRequired
        {
            get
            {
                if (mImageListView.BorderStyle != cachedBorderStyle)
                    return true;
                else if (mImageListView.View != cachedView)
                    return true;
                else if (mImageListView.ViewOffset != cachedViewOffset)
                    return true;
                else if (mImageListView.Size != cachedSize)
                    return true;
                else if (mImageListView.Items.Count != cachedItemCount)
                    return true;
                else if (mImageListView.mRenderer.MeasureItem(mImageListView.View) != cachedItemSize)
                    return true;
                else if (mImageListView.mRenderer.MeasureColumnHeaderHeight() != cachedHeaderHeight)
                    return true;
                else
                    return false;
            }
        }
        /// <summary>
        /// Returns the item margin adjusted to the current view mode.
        /// </summary>
        public Size AdjustedItemMargin
        {
            get
            {
                if (mImageListView.View == View.Details)
                    return new Size(2, 0);
                else
                    return mImageListView.ItemMargin;
            }
        }
        #endregion

        #region Constructor
        public ImageListViewLayoutManager(ImageListView owner)
        {
            mImageListView = owner;
            cachedVisibleItems = new Dictionary<Guid, bool>();

            vScrollVisible = false;
            hScrollVisible = false;

            Update();
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Returns the bounds of the item with the specified index.
        /// </summary>
        public Rectangle GetItemBounds(int itemIndex)
        {
            Point location = new Point();
            Size itemMargin = AdjustedItemMargin;
            if (mImageListView.BorderStyle != BorderStyle.None)
                location.Offset(1, 1);
            location.X += itemMargin.Width / 2 + (itemIndex % mCols) * (mItemSize.Width + itemMargin.Width) - mImageListView.ViewOffset.X;
            location.Y += itemMargin.Height / 2 + (itemIndex / mCols) * (mItemSize.Height + itemMargin.Height) - mImageListView.ViewOffset.Y;
            if (mImageListView.View == View.Details)
                location.Y += mImageListView.mRenderer.MeasureColumnHeaderHeight();
            return new Rectangle(location, mItemSize);
        }
        /// <summary>
        /// Returns the bounds of the item with the specified index, 
        /// including the margin around the item.
        /// </summary>
        public Rectangle GetItemBoundsWithMargin(int itemIndex)
        {
            Rectangle rec = GetItemBounds(itemIndex);
            if (mImageListView.View == View.Details)
                rec.Inflate(2, 0);
            else
                rec.Inflate(mImageListView.ItemMargin.Width / 2, mImageListView.ItemMargin.Height / 2);
            return rec;
        }
        /// <summary>
        /// Recalculates the control layout.
        /// </summary>
        public void Update()
        {
            Update(false);
        }
        /// <summary>
        /// Recalculates the control layout.
        /// </summary>
        public void Update(bool forceUpdate)
        {
            if (mImageListView.ClientRectangle.Width == 0 || mImageListView.ClientRectangle.Height == 0) return;
            if (!forceUpdate && !UpdateRequired) return;
            // Cache current properties to determine if we will need an update later
            cachedBorderStyle = mImageListView.BorderStyle;
            cachedView = mImageListView.View;
            cachedViewOffset = mImageListView.ViewOffset;
            cachedSize = mImageListView.Size;
            cachedItemCount = mImageListView.Items.Count;
            cachedItemSize = mImageListView.mRenderer.MeasureItem(mImageListView.View);
            cachedHeaderHeight = mImageListView.mRenderer.MeasureColumnHeaderHeight();
            cachedVisibleItems.Clear();

            // Calculate drawing area
            mClientArea = mImageListView.ClientRectangle;
            mItemAreaBounds = mImageListView.ClientRectangle;

            // Allocate space for border
            if (mImageListView.BorderStyle != BorderStyle.None)
            {
                mClientArea.Inflate(-1, -1);
                mItemAreaBounds.Inflate(-1, -1);
            }

            // Allocate space for scrollbars
            if (mImageListView.hScrollBar.Visible)
            {
                mClientArea.Height -= mImageListView.hScrollBar.Height;
                mItemAreaBounds.Height -= mImageListView.hScrollBar.Height;
            }
            if (mImageListView.vScrollBar.Visible)
            {
                mClientArea.Width -= mImageListView.vScrollBar.Width;
                mItemAreaBounds.Width -= mImageListView.vScrollBar.Width;
            }

            // Allocate space for column headers
            if (mImageListView.View == View.Details)
            {
                int headerHeight = cachedHeaderHeight;

                // Location of the column headers
                mColumnHeaderBounds.X = mClientArea.Left - mImageListView.ViewOffset.X;
                mColumnHeaderBounds.Y = mClientArea.Top;
                mColumnHeaderBounds.Height = headerHeight;
                mColumnHeaderBounds.Width = mClientArea.Width + mImageListView.ViewOffset.X;

                mItemAreaBounds.Y += headerHeight;
                mItemAreaBounds.Height -= headerHeight;
            }
            else
            {
                mColumnHeaderBounds = Rectangle.Empty;
            }
            if (mItemAreaBounds.Height < 1 || mItemAreaBounds.Height < 1) return;

            // Item size
            mItemSize = cachedItemSize;
            mItemSizeWithMargin = mItemSize + AdjustedItemMargin;

            // Maximum number of rows and columns that can be fully displayed
            mCols = (int)System.Math.Floor((float)mItemAreaBounds.Width / (float)mItemSizeWithMargin.Width);
            mRows = (int)System.Math.Floor((float)mItemAreaBounds.Height / (float)mItemSizeWithMargin.Height);
            if (mImageListView.View == View.Details) mCols = 1;
            if (mCols < 1) mCols = 1;
            if (mRows < 1) mRows = 1;

            // Check if we need the horizontal scroll bar
            bool hScrollRequired = (mImageListView.Items.Count > 0) && (mItemAreaBounds.Width < mItemSizeWithMargin.Width);
            if (hScrollRequired != hScrollVisible)
            {
                hScrollVisible = hScrollRequired;
                mImageListView.hScrollBar.Visible = hScrollRequired;
                Update(true);
                return;
            }

            // Check if we need the vertical scroll bar
            bool vScrollRequired = (mImageListView.Items.Count > 0) && (mCols * mRows < mImageListView.Items.Count);
            if (vScrollRequired != vScrollVisible)
            {
                vScrollVisible = vScrollRequired;
                mImageListView.vScrollBar.Visible = vScrollRequired;
                Update(true);
                return;
            }

            // Horizontal scroll range
            mImageListView.hScrollBar.SmallChange = 1;
            mImageListView.hScrollBar.LargeChange = mItemAreaBounds.Width;
            mImageListView.hScrollBar.Minimum = 0;
            mImageListView.hScrollBar.Maximum = mItemSizeWithMargin.Width;
            if (mImageListView.ViewOffset.X > mImageListView.hScrollBar.Maximum - mImageListView.hScrollBar.LargeChange + 1)
            {
                mImageListView.hScrollBar.Value = mImageListView.hScrollBar.Maximum - mImageListView.hScrollBar.LargeChange + 1;
                mImageListView.ViewOffset = new Point(mImageListView.hScrollBar.Value, mImageListView.ViewOffset.Y);
            }

            // Vertical scroll range
            mImageListView.vScrollBar.SmallChange = mItemSizeWithMargin.Height;
            mImageListView.vScrollBar.LargeChange = mItemAreaBounds.Height;
            mImageListView.vScrollBar.Minimum = 0;
            mImageListView.vScrollBar.Maximum = Math.Max(0, (int)System.Math.Ceiling((float)mImageListView.Items.Count / (float)mCols) * mItemSizeWithMargin.Height - 1);
            if (mImageListView.ViewOffset.Y > mImageListView.vScrollBar.Maximum - mImageListView.vScrollBar.LargeChange + 1)
            {
                mImageListView.vScrollBar.Value = mImageListView.vScrollBar.Maximum - mImageListView.vScrollBar.LargeChange + 1;
                mImageListView.ViewOffset = new Point(mImageListView.ViewOffset.X, mImageListView.vScrollBar.Value);
            }

            // Zero out the scrollbars if we don't have any items
            if (mImageListView.Items.Count == 0)
            {
                mImageListView.hScrollBar.Minimum = 0;
                mImageListView.hScrollBar.Maximum = 0;
                mImageListView.hScrollBar.Value = 0;
                mImageListView.vScrollBar.Minimum = 0;
                mImageListView.vScrollBar.Maximum = 0;
                mImageListView.vScrollBar.Value = 0;
                mImageListView.ViewOffset = new Point(0, 0);
            }

            // Horizontal scrollbar position
            mImageListView.hScrollBar.Left = (mImageListView.BorderStyle == BorderStyle.None ? 0 : 1);
            mImageListView.hScrollBar.Top = mImageListView.ClientRectangle.Bottom - (mImageListView.BorderStyle == BorderStyle.None ? 0 : 1) - mImageListView.hScrollBar.Height;
            mImageListView.hScrollBar.Width = mImageListView.ClientRectangle.Width - (mImageListView.BorderStyle == BorderStyle.None ? 0 : 2) - (mImageListView.vScrollBar.Visible ? mImageListView.vScrollBar.Width : 0);
            // Vertical scrollbar position
            mImageListView.vScrollBar.Left = mImageListView.ClientRectangle.Right - (mImageListView.BorderStyle == BorderStyle.None ? 0 : 1) - mImageListView.vScrollBar.Width;
            mImageListView.vScrollBar.Top = (mImageListView.BorderStyle == BorderStyle.None ? 0 : 1);
            mImageListView.vScrollBar.Height = mImageListView.ClientRectangle.Height - (mImageListView.BorderStyle == BorderStyle.None ? 0 : 2) - (mImageListView.hScrollBar.Visible ? mImageListView.hScrollBar.Height : 0);

            // Find the first and last partially visible items
            mFirstPartiallyVisible = (int)System.Math.Floor((float)mImageListView.ViewOffset.Y / (float)mItemSizeWithMargin.Height) * mCols;
            mLastPartiallyVisible = System.Math.Min((int)System.Math.Ceiling((float)(mImageListView.ViewOffset.Y + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * mCols - 1, mImageListView.Items.Count - 1);
            if (mFirstPartiallyVisible < 0) mFirstPartiallyVisible = 0;
            if (mFirstPartiallyVisible > mImageListView.Items.Count - 1) mFirstPartiallyVisible = mImageListView.Items.Count - 1;
            if (mLastPartiallyVisible < 0) mLastPartiallyVisible = 0;
            if (mLastPartiallyVisible > mImageListView.Items.Count - 1) mLastPartiallyVisible = mImageListView.Items.Count - 1;

            // Find the first and last visible items
            mFirstVisible = (int)System.Math.Ceiling((float)mImageListView.ViewOffset.Y / (float)mItemSizeWithMargin.Height) * mCols;
            mLastVisible = System.Math.Min((int)System.Math.Floor((float)(mImageListView.ViewOffset.Y + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * mCols - 1, mImageListView.Items.Count - 1);
            if (mFirstVisible < 0) mFirstVisible = 0;
            if (mFirstVisible > mImageListView.Items.Count - 1) mFirstVisible = mImageListView.Items.Count - 1;
            if (mLastVisible < 0) mLastVisible = 0;
            if (mLastVisible > mImageListView.Items.Count - 1) mLastVisible = mImageListView.Items.Count - 1;

            // Cache visible items
            if (mFirstPartiallyVisible >= 0 &&
                mLastPartiallyVisible >= 0 &&
                mFirstPartiallyVisible <= mImageListView.Items.Count - 1 &&
                mLastPartiallyVisible <= mImageListView.Items.Count - 1)
            {
                for (int i = mFirstPartiallyVisible; i <= mLastPartiallyVisible; i++)
                    cachedVisibleItems.Add(mImageListView.Items[i].Guid, false);
            }
        }
        /// <summary>
        /// Determines whether the item with the given guid is
        /// (partially) visible.
        /// </summary>
        /// <param name="guid">The guid of the item to check.</param>
        public bool IsItemVisible(Guid guid)
        {
            return cachedVisibleItems.ContainsKey(guid);
        }
        #endregion
    }
}
