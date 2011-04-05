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

        private View cachedView;
        private Point cachedViewOffset;
        private Size cachedSize;
        private int cachedItemCount;
        private Size cachedItemSize;
        private int cachedGroupHeaderHeight;
        private int cachedColumnHeaderHeight;
        private bool cachedIntegralScroll;
        private Size cachedItemMargin;
        private int cachedPaneWidth;
        private bool cachedScrollBars;
        private Dictionary<Guid, bool> cachedVisibleItems;

        private bool vScrollVisible;
        private bool hScrollVisible;

        // Size required to display all items (i.e. scroll range)
        private int totalWidth;
        private int totalHeight;
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
                if (mImageListView.View != cachedView)
                    return true;
                else if (mImageListView.ViewOffset != cachedViewOffset)
                    return true;
                else if (mImageListView.ClientSize != cachedSize)
                    return true;
                else if (mImageListView.Items.Count != cachedItemCount)
                    return true;
                else if (mImageListView.mRenderer.MeasureItem(mImageListView.View) != cachedItemSize)
                    return true;
                else if (mImageListView.mRenderer.MeasureGroupHeaderHeight() != cachedGroupHeaderHeight)
                    return true;
                else if (mImageListView.mRenderer.MeasureColumnHeaderHeight() != cachedColumnHeaderHeight)
                    return true;
                else if (mImageListView.mRenderer.MeasureItemMargin(mImageListView.View) != cachedItemMargin)
                    return true;
                else if (mImageListView.PaneWidth != cachedPaneWidth)
                    return true;
                else if (mImageListView.ScrollBars != cachedScrollBars)
                    return true;
                else if (mImageListView.IntegralScroll != cachedIntegralScroll)
                    return true;
                else if (mImageListView.Items.collectionModified)
                    return true;
                else if (mImageListView.groups.collectionModified)
                    return true;
                else
                    return false;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ImageListViewLayoutManager class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
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
        /// Determines whether the item with the given guid is
        /// (partially) visible.
        /// </summary>
        /// <param name="guid">The guid of the item to check.</param>
        public bool IsItemVisible(Guid guid)
        {
            return cachedVisibleItems.ContainsKey(guid);
        }
        /// <summary>
        /// Returns the bounds of the item with the specified index.
        /// </summary>
        public Rectangle GetItemBounds(int itemIndex)
        {
            Point location = new Point();

            if (mImageListView.showGroups)
            {
                foreach (ImageListView.ImageListViewGroup group in mImageListView.groups)
                {
                    if (itemIndex >= group.FirstItemIndex && itemIndex <= group.LastItemIndex)
                    {
                        location = group.itemBounds.Location;
                        location.X += cachedItemMargin.Width / 2;
                        location.Y += cachedItemMargin.Height / 2;

                        if (mImageListView.View == View.Gallery)
                            location.X += (itemIndex - group.FirstItemIndex) * mItemSizeWithMargin.Width;
                        else
                        {
                            location.X += ((itemIndex - group.FirstItemIndex) % mCols) * mItemSizeWithMargin.Width;
                            location.Y += ((itemIndex - group.FirstItemIndex) / mCols) * mItemSizeWithMargin.Height;
                        }
                        break;
                    }
                }
            }
            else
            {
                location = mItemAreaBounds.Location;
                location.X += cachedItemMargin.Width / 2 - mImageListView.ViewOffset.X;
                location.Y += cachedItemMargin.Height / 2 - mImageListView.ViewOffset.Y;

                if (mImageListView.View == View.Gallery)
                    location.X += itemIndex * mItemSizeWithMargin.Width;
                else
                {
                    location.X += (itemIndex % mCols) * mItemSizeWithMargin.Width;
                    location.Y += (itemIndex / mCols) * mItemSizeWithMargin.Height;
                }
            }

            return new Rectangle(location, mItemSize);
        }
        /// <summary>
        /// Returns the bounds of the item with the specified index, 
        /// including the margin around the item.
        /// </summary>
        public Rectangle GetItemBoundsWithMargin(int itemIndex)
        {
            Rectangle rec = GetItemBounds(itemIndex);
            rec.Inflate(cachedItemMargin.Width / 2, cachedItemMargin.Height / 2);
            return rec;
        }
        /// <summary>
        /// Returns the item checkbox bounds.
        /// This method assumes a checkbox icon size of 16x16
        /// </summary>
        public Rectangle GetCheckBoxBounds(int itemIndex)
        {
            Rectangle bounds = GetWidgetBounds(GetItemBounds(itemIndex), new Size(16, 16),
                mImageListView.CheckBoxPadding, mImageListView.CheckBoxAlignment);

            // If the checkbox and the icon have the same alignment,
            // move the checkbox horizontally away from the icon
            if (mImageListView.View != View.Details && mImageListView.CheckBoxAlignment == mImageListView.IconAlignment &&
                mImageListView.ShowCheckBoxes && mImageListView.ShowFileIcons)
            {
                ContentAlignment alignment = mImageListView.CheckBoxAlignment;
                if (alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.TopCenter)
                    bounds.X -= 8 + mImageListView.IconPadding.Width / 2;
                else if (alignment == ContentAlignment.BottomRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.TopRight)
                    bounds.X -= 16 + mImageListView.IconPadding.Width;
            }

            return bounds;
        }
        /// <summary>
        /// Returns the item icon bounds.
        /// This method assumes an icon size of 16x16
        /// </summary>
        public Rectangle GetIconBounds(int itemIndex)
        {
            Rectangle bounds = GetWidgetBounds(GetItemBounds(itemIndex), new Size(16, 16),
                mImageListView.IconPadding, mImageListView.IconAlignment);

            // If the checkbox and the icon have the same alignment,
            // or in details view move the icon horizontally away from the checkbox
            if (mImageListView.View == View.Details && mImageListView.ShowCheckBoxes && mImageListView.ShowFileIcons)
                bounds.X += 16 + 2;
            else if (mImageListView.CheckBoxAlignment == mImageListView.IconAlignment &&
                mImageListView.ShowCheckBoxes && mImageListView.ShowFileIcons)
            {
                ContentAlignment alignment = mImageListView.CheckBoxAlignment;
                if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.TopLeft)
                    bounds.X += 16 + mImageListView.IconPadding.Width;
                else if (alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.TopCenter)
                    bounds.X += 8 + mImageListView.IconPadding.Width / 2;
            }

            return bounds;
        }
        /// <summary>
        /// Returns the bounds of a widget.
        /// Used to calculate the bounds of checkboxes and icons.
        /// </summary>
        private Rectangle GetWidgetBounds(Rectangle bounds, Size size, Size padding, ContentAlignment alignment)
        {
            // Apply padding
            if (mImageListView.View == View.Details)
                bounds.Inflate(-2, -2);
            else
                bounds.Inflate(-padding.Width, -padding.Height);

            int x = 0;
            if (mImageListView.View == View.Details)
                x = bounds.Left;
            else if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.TopLeft)
                x = bounds.Left;
            else if (alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.TopCenter)
                x = bounds.Left + bounds.Width / 2 - size.Width / 2;
            else // if (alignment == ContentAlignment.BottomRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.TopRight)
                x = bounds.Right - size.Width;

            int y = 0;
            if (mImageListView.View == View.Details)
                y = bounds.Top + bounds.Height / 2 - size.Height / 2;
            else if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.BottomRight)
                y = bounds.Bottom - size.Height;
            else if (alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.MiddleRight)
                y = bounds.Top + bounds.Height / 2 - size.Height / 2;
            else // if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight)
                y = bounds.Top;

            return new Rectangle(x, y, size.Width, size.Height);
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
        /// <param name="forceUpdate">true to force an update; otherwise false.</param>
        /// </summary>
        public void Update(bool forceUpdate)
        {
            if (mImageListView.ClientRectangle.Width == 0 || mImageListView.ClientRectangle.Height == 0)
                return;

            // If only item order is changed, just update visible items.
            if (!forceUpdate && !UpdateRequired && mImageListView.Items.collectionModified)
            {
                UpdateGroups();
                UpdateVisibleItems();
                return;
            }

            if (!forceUpdate && !UpdateRequired)
                return;

            // Get the item size from the renderer
            mItemSize = mImageListView.mRenderer.MeasureItem(mImageListView.View);
            cachedItemMargin = mImageListView.mRenderer.MeasureItemMargin(mImageListView.View);
            mItemSizeWithMargin = mItemSize + cachedItemMargin;

            // Cache current properties to determine if we will need an update later
            cachedView = mImageListView.View;
            cachedViewOffset = mImageListView.ViewOffset;
            cachedSize = mImageListView.ClientSize;
            cachedItemCount = mImageListView.Items.Count;
            cachedIntegralScroll = mImageListView.IntegralScroll;
            cachedItemSize = mItemSize;
            cachedGroupHeaderHeight = mImageListView.mRenderer.MeasureGroupHeaderHeight();
            cachedColumnHeaderHeight = mImageListView.mRenderer.MeasureColumnHeaderHeight();
            cachedPaneWidth = mImageListView.PaneWidth;
            cachedScrollBars = mImageListView.ScrollBars;
            mImageListView.Items.collectionModified = false;
            mImageListView.groups.collectionModified = false;

            // Calculate item area bounds
            if (!UpdateItemArea())
                return;

            // Let the calculated bounds modified by the renderer
            LayoutEventArgs eLayout = new LayoutEventArgs(mItemAreaBounds);
            mImageListView.mRenderer.OnLayout(eLayout);
            mItemAreaBounds = eLayout.ItemAreaBounds;
            if (mItemAreaBounds.Width <= 0 || mItemAreaBounds.Height <= 0)
                return;

            // Calculate the number of rows and columns
            CalculateGrid();

            // Update groups
            UpdateGroups();

            // Check if we need the scroll bars.
            // Recalculate the layout if scroll bar visibility changes.
            if (CheckScrollBars())
            {
                Update(true);
                return;
            }

            // Update scroll range
            UpdateScrollBars();

            // Cache visible items
            UpdateVisibleItems();
        }
        /// <summary>
        /// Calculates the maximum number of rows and columns 
        /// that can be fully displayed.
        /// </summary>
        private void CalculateGrid()
        {
            mRows = (int)System.Math.Floor((float)mItemAreaBounds.Height / (float)mItemSizeWithMargin.Height);
            mCols = (int)System.Math.Floor((float)mItemAreaBounds.Width / (float)mItemSizeWithMargin.Width);
            if (mImageListView.View == View.Details) mCols = 1;
            if (mImageListView.View == View.Gallery) mRows = 1;
            if (mCols < 1) mCols = 1;
            if (mRows < 1) mRows = 1;

            totalWidth = mRows * mItemSizeWithMargin.Width;
            totalHeight = mCols * mItemSizeWithMargin.Height;
        }
        /// <summary>
        /// Calculates the item area.
        /// </summary>
        /// <returns>true if the item area is not empty (both width and height
        /// greater than zero); otherwise false.</returns>
        private bool UpdateItemArea()
        {
            // Calculate drawing area
            mClientArea = mImageListView.ClientRectangle;
            mItemAreaBounds = mImageListView.ClientRectangle;

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
                int headerHeight = cachedColumnHeaderHeight;

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
            // Modify item area for the gallery view mode
            if (mImageListView.View == View.Gallery)
            {
                mItemAreaBounds.Height = mItemSizeWithMargin.Height;
                mItemAreaBounds.Y = mClientArea.Bottom - mItemSizeWithMargin.Height;
            }
            // Modify item area for the pane view mode
            if (mImageListView.View == View.Pane)
            {
                mItemAreaBounds.Width -= cachedPaneWidth;
                mItemAreaBounds.X += cachedPaneWidth;
            }

            return (mItemAreaBounds.Width > 0 && mItemAreaBounds.Height > 0);
        }
        /// <summary>
        /// Shows or hides the scroll bars.
        /// Returns true if the layout needs to be recalculated; otherwise false.
        /// </summary>
        /// <returns></returns>
        private bool CheckScrollBars()
        {
            // Horizontal scroll bar
            bool hScrollRequired = false;
            bool hScrollChanged = false;
            if (mImageListView.ScrollBars)
                hScrollRequired = (mImageListView.Items.Count > 0) && (mItemAreaBounds.Width < totalWidth);

            if (hScrollRequired != hScrollVisible)
            {
                hScrollVisible = hScrollRequired;
                mImageListView.hScrollBar.Visible = hScrollRequired;
                hScrollChanged = true;
            }

            // Vertical scroll bar
            bool vScrollRequired = false;
            bool vScrollChanged = false;
            if (mImageListView.ScrollBars)
                vScrollRequired = (mImageListView.Items.Count > 0) && (mItemAreaBounds.Height < totalHeight);

            if (vScrollRequired != vScrollVisible)
            {
                vScrollVisible = vScrollRequired;
                mImageListView.vScrollBar.Visible = vScrollRequired;
                vScrollChanged = true;
            }

            // Determine if the layout needs to be recalculated
            return (hScrollChanged || vScrollChanged);
        }
        /// <summary>
        /// Updates scroll bar parameters.
        /// </summary>
        private void UpdateScrollBars()
        {
            // Set scroll range
            if (mImageListView.Items.Count != 0)
            {
                // Horizontal scroll range
                if (mImageListView.View == View.Gallery)
                {
                    mImageListView.hScrollBar.Minimum = 0;
                    mImageListView.hScrollBar.Maximum = Math.Max(0, totalWidth - 1);
                    if (!mImageListView.IntegralScroll)
                        mImageListView.hScrollBar.LargeChange = mItemAreaBounds.Width;
                    else
                        mImageListView.hScrollBar.LargeChange = mItemSizeWithMargin.Width * mCols;
                    mImageListView.hScrollBar.SmallChange = mItemSizeWithMargin.Width;
                }
                else
                {
                    mImageListView.hScrollBar.Minimum = 0;
                    mImageListView.hScrollBar.Maximum = mCols * mItemSizeWithMargin.Width;
                    mImageListView.hScrollBar.LargeChange = mItemAreaBounds.Width;
                    mImageListView.hScrollBar.SmallChange = 1;
                }
                if (mImageListView.ViewOffset.X > mImageListView.hScrollBar.Maximum - mImageListView.hScrollBar.LargeChange + 1)
                {
                    mImageListView.hScrollBar.Value = mImageListView.hScrollBar.Maximum - mImageListView.hScrollBar.LargeChange + 1;
                    mImageListView.ViewOffset = new Point(mImageListView.hScrollBar.Value, mImageListView.ViewOffset.Y);
                }

                // Vertical scroll range
                if (mImageListView.View == View.Gallery)
                {
                    mImageListView.vScrollBar.Minimum = 0;
                    mImageListView.vScrollBar.Maximum = mRows * mItemSizeWithMargin.Height;
                    mImageListView.vScrollBar.LargeChange = mItemAreaBounds.Height;
                    mImageListView.vScrollBar.SmallChange = 1;
                }
                else
                {
                    mImageListView.vScrollBar.Minimum = 0;
                    mImageListView.vScrollBar.Maximum = Math.Max(0, totalHeight - 1);
                    if (!mImageListView.IntegralScroll)
                        mImageListView.vScrollBar.LargeChange = mItemAreaBounds.Height;
                    else
                        mImageListView.vScrollBar.LargeChange = mItemSizeWithMargin.Height * mRows;
                    mImageListView.vScrollBar.SmallChange = mItemSizeWithMargin.Height;
                }
                if (mImageListView.ViewOffset.Y > mImageListView.vScrollBar.Maximum - mImageListView.vScrollBar.LargeChange + 1)
                {
                    mImageListView.vScrollBar.Value = mImageListView.vScrollBar.Maximum - mImageListView.vScrollBar.LargeChange + 1;
                    mImageListView.ViewOffset = new Point(mImageListView.ViewOffset.X, mImageListView.vScrollBar.Value);
                }
            }
            else // if (mImageListView.Items.Count == 0)
            {
                // Zero out the scrollbars if we don't have any items
                mImageListView.hScrollBar.Minimum = 0;
                mImageListView.hScrollBar.Maximum = 0;
                mImageListView.hScrollBar.Value = 0;
                mImageListView.vScrollBar.Minimum = 0;
                mImageListView.vScrollBar.Maximum = 0;
                mImageListView.vScrollBar.Value = 0;
                mImageListView.ViewOffset = new Point(0, 0);
            }

            // Horizontal scrollbar position
            mImageListView.hScrollBar.Left = 0;
            mImageListView.hScrollBar.Top = mImageListView.ClientRectangle.Bottom - mImageListView.hScrollBar.Height;
            mImageListView.hScrollBar.Width = mImageListView.ClientRectangle.Width - (mImageListView.vScrollBar.Visible ? mImageListView.vScrollBar.Width : 0);
            // Vertical scrollbar position
            mImageListView.vScrollBar.Left = mImageListView.ClientRectangle.Right - mImageListView.vScrollBar.Width;
            mImageListView.vScrollBar.Top = 0;
            mImageListView.vScrollBar.Height = mImageListView.ClientRectangle.Height - (mImageListView.hScrollBar.Visible ? mImageListView.hScrollBar.Height : 0);
        }
        /// <summary>
        /// Updates the dictionary of visible items.
        /// </summary>
        private void UpdateVisibleItems()
        {
            if (mImageListView.showGroups)
            {
                mFirstPartiallyVisible = -1;
                mLastPartiallyVisible = -1;
                mFirstVisible = -1;
                mLastVisible = -1;

                ImageListView.ImageListViewGroup firstVisibleGroup = null;
                ImageListView.ImageListViewGroup lastVisibleGroup = null;
                foreach (ImageListView.ImageListViewGroup group in mImageListView.groups)
                {
                    if (ItemAreaBounds.IntersectsWith(group.itemBounds))
                    {
                        if (firstVisibleGroup == null)
                            firstVisibleGroup = group;
                        lastVisibleGroup = group;
                    }
                    else if (lastVisibleGroup != null)
                        break;
                }

                // Find the first and last visible items
                if (mImageListView.View == View.Gallery)
                {
                    mFirstPartiallyVisible = firstVisibleGroup.FirstItemIndex + (int)System.Math.Floor((float)(ItemAreaBounds.Left - firstVisibleGroup.itemBounds.Left) / (float)mItemSizeWithMargin.Width) * firstVisibleGroup.itemRows;
                    mLastPartiallyVisible = lastVisibleGroup.FirstItemIndex + (int)System.Math.Ceiling((float)((ItemAreaBounds.Left - lastVisibleGroup.itemBounds.Left) + mItemAreaBounds.Width) / (float)mItemSizeWithMargin.Width) * lastVisibleGroup.itemRows - 1;
                    mFirstVisible = firstVisibleGroup.FirstItemIndex + (int)System.Math.Ceiling((float)(ItemAreaBounds.Left - firstVisibleGroup.itemBounds.Left) / (float)mItemSizeWithMargin.Width) * firstVisibleGroup.itemRows;
                    mLastVisible = lastVisibleGroup.FirstItemIndex + (int)System.Math.Floor((float)((ItemAreaBounds.Left - lastVisibleGroup.itemBounds.Left) + mItemAreaBounds.Width) / (float)mItemSizeWithMargin.Width) * lastVisibleGroup.itemRows - 1;
                }
                else
                {
                    mFirstPartiallyVisible = firstVisibleGroup.FirstItemIndex + (int)System.Math.Floor((float)(ItemAreaBounds.Top - firstVisibleGroup.itemBounds.Top) / (float)mItemSizeWithMargin.Height) * firstVisibleGroup.itemCols;
                    mLastPartiallyVisible = lastVisibleGroup.FirstItemIndex + (int)System.Math.Ceiling((float)((ItemAreaBounds.Top - lastVisibleGroup.itemBounds.Top) + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * lastVisibleGroup.itemCols - 1;
                    mFirstVisible = firstVisibleGroup.FirstItemIndex + (int)System.Math.Ceiling((float)(ItemAreaBounds.Top - firstVisibleGroup.itemBounds.Top) / (float)mItemSizeWithMargin.Height) * firstVisibleGroup.itemCols;
                    mLastVisible = lastVisibleGroup.FirstItemIndex + (int)System.Math.Floor((float)((ItemAreaBounds.Top - lastVisibleGroup.itemBounds.Top) + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * lastVisibleGroup.itemCols - 1;
                }
            }
            else
            {
                // Find the first and last visible items
                if (mImageListView.View == View.Gallery)
                {
                    mFirstPartiallyVisible = (int)System.Math.Floor((float)mImageListView.ViewOffset.X / (float)mItemSizeWithMargin.Width) * mRows;
                    mLastPartiallyVisible = (int)System.Math.Ceiling((float)(mImageListView.ViewOffset.X + mItemAreaBounds.Width) / (float)mItemSizeWithMargin.Width) * mRows - 1;
                    mFirstVisible = (int)System.Math.Ceiling((float)mImageListView.ViewOffset.X / (float)mItemSizeWithMargin.Width) * mRows;
                    mLastVisible = (int)System.Math.Floor((float)(mImageListView.ViewOffset.X + mItemAreaBounds.Width) / (float)mItemSizeWithMargin.Width) * mRows - 1;
                }
                else
                {
                    mFirstPartiallyVisible = (int)System.Math.Floor((float)mImageListView.ViewOffset.Y / (float)mItemSizeWithMargin.Height) * mCols;
                    mLastPartiallyVisible = (int)System.Math.Ceiling((float)(mImageListView.ViewOffset.Y + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * mCols - 1;
                    mFirstVisible = (int)System.Math.Ceiling((float)mImageListView.ViewOffset.Y / (float)mItemSizeWithMargin.Height) * mCols;
                    mLastVisible = (int)System.Math.Floor((float)(mImageListView.ViewOffset.Y + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * mCols - 1;
                }
            }

            // Bounds check
            if (mFirstPartiallyVisible < 0) mFirstPartiallyVisible = 0;
            if (mFirstPartiallyVisible > mImageListView.Items.Count - 1) mFirstPartiallyVisible = mImageListView.Items.Count - 1;
            if (mLastPartiallyVisible < 0) mLastPartiallyVisible = 0;
            if (mLastPartiallyVisible > mImageListView.Items.Count - 1) mLastPartiallyVisible = mImageListView.Items.Count - 1;
            if (mFirstVisible < 0) mFirstVisible = 0;
            if (mFirstVisible > mImageListView.Items.Count - 1) mFirstVisible = mImageListView.Items.Count - 1;
            if (mLastVisible < 0) mLastVisible = 0;
            if (mLastVisible > mImageListView.Items.Count - 1) mLastVisible = mImageListView.Items.Count - 1;

            // Cache visible items
            cachedVisibleItems.Clear();

            if (mFirstPartiallyVisible >= 0 &&
                mLastPartiallyVisible >= 0 &&
                mFirstPartiallyVisible <= mImageListView.Items.Count - 1 &&
                mLastPartiallyVisible <= mImageListView.Items.Count - 1)
            {
                for (int i = mFirstPartiallyVisible; i <= mLastPartiallyVisible; i++)
                    cachedVisibleItems.Add(mImageListView.Items[i].Guid, false);
            }

            // Current item state processed
            mImageListView.Items.collectionModified = false;
        }
        /// <summary>
        /// Updates the group display properties.
        /// </summary>
        private void UpdateGroups()
        {
            int x = mItemAreaBounds.Left - mImageListView.ViewOffset.X;
            int y = mItemAreaBounds.Top - mImageListView.ViewOffset.Y;
            if (mImageListView.View == View.Gallery)
                totalWidth = 0;
            else
                totalHeight = 0;

            foreach (ImageListView.ImageListViewGroup group in mImageListView.groups)
            {
                if (mImageListView.View == View.Gallery)
                {
                    // Number of rows and columns to enclose all items
                    group.itemRows = mRows;
                    group.itemCols = (int)System.Math.Ceiling((float)group.ItemCount / (float)mRows);

                    // Header area
                    group.headerBounds = new Rectangle(x, y, cachedGroupHeaderHeight, mItemSize.Height * mRows);
                    x += cachedGroupHeaderHeight;

                    // Item area
                    group.itemBounds = new Rectangle(x, y, group.itemCols * mItemSizeWithMargin.Width, group.itemRows * mItemSizeWithMargin.Height);
                    if (group.Collapsed)
                        group.itemBounds.Width = 0;

                    // Update total size
                    totalWidth += group.headerBounds.Width + group.itemBounds.Width;

                    // Offset to next group
                    x += group.itemBounds.Width;
                }
                else
                {
                    // Number of rows and columns to enclose all items
                    group.itemCols = mCols;
                    group.itemRows = (int)System.Math.Ceiling((float)group.ItemCount / (float)mCols);

                    // Header area
                    group.headerBounds = new Rectangle(x, y, mClientArea.Width + mImageListView.ViewOffset.X, cachedGroupHeaderHeight);
                    y += cachedGroupHeaderHeight;

                    // Item area
                    group.itemBounds = new Rectangle(x, y, group.itemCols * mItemSizeWithMargin.Width, group.itemRows * mItemSizeWithMargin.Height);
                    if (group.Collapsed)
                        group.itemBounds.Height = 0;

                    // Update total size
                    totalHeight += group.headerBounds.Height + group.itemBounds.Height;

                    // Offset to next group
                    y += group.itemBounds.Height;
                }

                group.isVisible = ItemAreaBounds.IntersectsWith(group.headerBounds) || ItemAreaBounds.IntersectsWith(group.itemBounds);
            }

            // Groups processed
            mImageListView.groups.collectionModified = false;
        }
        #endregion
    }
}
