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
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.ComponentModel;

namespace Manina.Windows.Forms
{
    public partial class ImageListView
    {
        /// <summary>
        /// Represents an overridable class for image list view renderers.
        /// </summary>
        public class ImageListViewRenderer : IDisposable
        {
            #region Member Variables
            private bool mClip;
            internal ImageListView mImageListView;
            private BufferedGraphicsContext bufferContext;
            private BufferedGraphics bufferGraphics;
            private bool disposed;
            private bool suspended;
            private int suspendCount;
            private bool needsPaint;
            private ItemDrawOrder mItemDrawOrder;
            #endregion

            #region Properties
            /// <summary>
            /// Gets the ImageListView owning this item.
            /// </summary>
            public ImageListView ImageListView { get { return mImageListView; } }
            /// <summary>
            /// Gets or sets whether the graphics is clipped to the bounds of 
            /// drawing elements.
            /// </summary>
            public bool Clip { get { return mClip; } set { mClip = value; } }
            /// <summary>
            /// Gets or sets the order by which items are drawn.
            /// </summary>
            public ItemDrawOrder ItemDrawOrder { get { return mItemDrawOrder; } set { mItemDrawOrder = value; } }
            /// <summary>
            /// Gets the rectangle bounding the item display area.
            /// </summary>
            public Rectangle ItemAreaBounds { get { return mImageListView.layoutManager.ItemAreaBounds; } }
            /// <summary>
            /// Gets the rectangle bounding the column headers.
            /// </summary>
            public Rectangle ColumnHeaderBounds { get { return mImageListView.layoutManager.ColumnHeaderBounds; } }
            #endregion

            #region Constructors
            public ImageListViewRenderer()
            {
                disposed = false;
                suspended = false;
                suspendCount = 0;
                needsPaint = true;
                mClip = true;
                mItemDrawOrder = ItemDrawOrder.ItemIndex;
            }
            #endregion

            #region DrawItemParams
            /// <summary>
            /// Represents the paramaters required to draw an item.
            /// </summary>
            private struct DrawItemParams
            {
                public ImageListViewItem Item;
                public ItemState State;
                public Rectangle Bounds;

                public DrawItemParams(ImageListViewItem item, ItemState state, Rectangle bounds)
                {
                    Item = item;
                    State = state;
                    Bounds = bounds;
                }
            }
            #endregion

            #region ItemDrawOrderComparer
            /// <summary>
            /// Compares items by the draw order.
            /// </summary>
            private class ItemDrawOrderComparer : IComparer<DrawItemParams>
            {
                private ItemDrawOrder mDrawOrder;

                public ItemDrawOrderComparer(ItemDrawOrder drawOrder)
                {
                    mDrawOrder = drawOrder;
                }

                /// <summary>
                /// Compares items by the draw order.
                /// </summary>
                /// <param name="param1">First item to compare.</param>
                /// <param name="param2">Second item to compare.</param>
                /// <returns>1 if the first item should be drawn first, 
                /// -1 if the second item should be drawn first,
                /// 0 if the two items can be drawn in any order.</returns>
                public int Compare(DrawItemParams param1, DrawItemParams param2)
                {
                    if (ReferenceEquals(param1, param2))
                        return 0;
                    if (ReferenceEquals(param1.Item, param2.Item))
                        return 0;

                    int comparison = 0;

                    if (mDrawOrder == ItemDrawOrder.ItemIndex)
                    {
                        return CompareByIndex(param1, param2);
                    }
                    else if (mDrawOrder == ItemDrawOrder.ZOrder)
                    {
                        return CompareByZOrder(param1, param2);
                    }
                    else if (mDrawOrder == ItemDrawOrder.NormalSelectedHovered)
                    {
                        comparison = -CompareByHovered(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareBySelected(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByNormal(param1, param2);
                        if (comparison != 0) return comparison;
                    }
                    else if (mDrawOrder == ItemDrawOrder.NormalHoveredSelected)
                    {
                        comparison = -CompareBySelected(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByHovered(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByNormal(param1, param2);
                        if (comparison != 0) return comparison;
                    }
                    else if (mDrawOrder == ItemDrawOrder.SelectedNormalHovered)
                    {
                        comparison = -CompareByHovered(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByNormal(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareBySelected(param1, param2);
                        if (comparison != 0) return comparison;
                    }
                    else if (mDrawOrder == ItemDrawOrder.SelectedHoveredNormal)
                    {
                        comparison = -CompareByNormal(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByHovered(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareBySelected(param1, param2);
                        if (comparison != 0) return comparison;
                    }
                    else if (mDrawOrder == ItemDrawOrder.HoveredNormalSelected)
                    {
                        comparison = -CompareBySelected(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByNormal(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByHovered(param1, param2);
                        if (comparison != 0) return comparison;
                    }
                    else if (mDrawOrder == ItemDrawOrder.HoveredSelectedNormal)
                    {
                        comparison = -CompareByNormal(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareBySelected(param1, param2);
                        if (comparison != 0) return comparison;
                        comparison = -CompareByHovered(param1, param2);
                        if (comparison != 0) return comparison;
                    }

                    // Compare by zorder
                    comparison = CompareByZOrder(param1, param2);
                    if (comparison != 0) return comparison;

                    // Finally compare by index
                    comparison = CompareByIndex(param1, param2);
                    return comparison;
                }

                /// <summary>
                /// Compares items by their index property.
                /// </summary>
                private int CompareByIndex(DrawItemParams param1, DrawItemParams param2)
                {
                    if (param1.Item.Index < param2.Item.Index)
                        return -1;
                    else if (param1.Item.Index > param2.Item.Index)
                        return 1;
                    else
                        return 0;
                }
                /// <summary>
                /// Compares items by their zorder property.
                /// </summary>
                private int CompareByZOrder(DrawItemParams param1, DrawItemParams param2)
                {
                    if (param1.Item.ZOrder < param2.Item.ZOrder)
                        return -1;
                    else if (param1.Item.ZOrder > param2.Item.ZOrder)
                        return 1;
                    else
                        return 0;
                }
                /// <summary>
                /// Compares items by their neutral state.
                /// </summary>
                private int CompareByNormal(DrawItemParams param1, DrawItemParams param2)
                {
                    if (param1.State == ItemState.None && param2.State != ItemState.None)
                        return -1;
                    else if (param1.State != ItemState.None && param2.State == ItemState.None)
                        return 1;
                    else
                        return 0;
                }
                /// <summary>
                /// Compares items by their selected state.
                /// </summary>
                private int CompareBySelected(DrawItemParams param1, DrawItemParams param2)
                {
                    if ((param1.State & ItemState.Selected) == ItemState.Selected &&
                        (param2.State & ItemState.Selected) != ItemState.Selected)
                        return -1;
                    else if ((param1.State & ItemState.Selected) != ItemState.Selected &&
                        (param2.State & ItemState.Selected) == ItemState.Selected)
                        return 1;
                    else
                        return 0;
                }
                /// <summary>
                /// Compares items by their hovered state.
                /// </summary>
                private int CompareByHovered(DrawItemParams param1, DrawItemParams param2)
                {
                    if ((param1.State & ItemState.Hovered) == ItemState.Hovered)
                        return -1;
                    else if ((param2.State & ItemState.Hovered) == ItemState.Hovered)
                        return 1;
                    else
                        return 0;
                }
                /// <summary>
                /// Compares items by their focused state.
                /// </summary>
                private int CompareByFocused(DrawItemParams param1, DrawItemParams param2)
                {
                    if ((param1.State & ItemState.Focused) == ItemState.Focused)
                        return -1;
                    else if ((param2.State & ItemState.Focused) == ItemState.Focused)
                        return 1;
                    else
                        return 0;
                }
            }
            #endregion

            #region Instance Methods
            /// <summary>
            /// Loads and returns the image for the given item.
            /// </summary>
            public Image GetImageAsync(ImageListViewItem item, Size size)
            {
                Image img = mImageListView.cacheManager.GetRendererImage(item.Guid, size, mImageListView.UseEmbeddedThumbnails);
                if (img != null)
                    return img;

                mImageListView.cacheManager.AddToRendererCache(item.Guid, item.FileName, size, mImageListView.UseEmbeddedThumbnails);
                return null;
            }
            #endregion

            #region Internal Methods
            /// <summary>
            /// Redraws the owner control.
            /// </summary>
            /// <param name="forceUpdate">If true, forces an immediate update, even if
            /// the renderer is suspended by a SuspendPaint call.</param>
            internal void Refresh(Graphics graphics, bool forceUpdate)
            {
                if (forceUpdate || CanPaint())
                    Render(graphics);
                else
                    needsPaint = true;
            }
            /// <summary>
            /// Redraws the owner control.
            /// </summary>
            internal void Refresh(Graphics graphics)
            {
                Refresh(graphics, false);
            }
            /// <summary>
            /// Suspends painting until a matching ResumePaint call is made.
            /// Used by the parent control as part of SuspendLayout.
            /// </summary>
            internal void SuspendPaint(bool ispublic)
            {
                if (ispublic)
                    suspended = true;
                else
                    SuspendPaint();
            }
            /// <summary>
            /// Resumes painting. This call must be matched by a prior SuspendPaint call.
            /// Used by the parent control as part of ResumeLayout.
            /// </summary>
            internal void ResumePaint(bool ispublic)
            {
                if (ispublic)
                {
                    suspended = false;
                    if (needsPaint) mImageListView.Refresh();
                }
                else
                    ResumePaint();
            }
            /// <summary>
            /// Suspends painting until a matching ResumePaint call is made.
            /// </summary>
            internal void SuspendPaint()
            {
                if (suspendCount == 0) needsPaint = false;
                suspendCount++;
            }
            /// <summary>
            /// Resumes painting. This call must be matched by a prior SuspendPaint call.
            /// </summary>
            internal void ResumePaint()
            {
                System.Diagnostics.Debug.Assert(
                    suspendCount > 0,
                    "Suspend count does not match resume count.",
                    "ResumePaint() must be matched by a prior SuspendPaint() call."
                    );

                suspendCount--;
                if (needsPaint)
                    mImageListView.Refresh();
            }
            /// <summary>
            /// Determines if the control can be painted.
            /// </summary>
            internal bool CanPaint()
            {
                return (suspended == false && suspendCount == 0);
            }
            /// <summary>
            /// Renders the control.
            /// </summary>
            private void Render(Graphics graphics)
            {
                if (disposed)
                    return;

                if (bufferGraphics == null)
                    RecreateBuffer();

                // Update the layout
                mImageListView.layoutManager.Update();

                // Set drawing area
                Graphics g = bufferGraphics.Graphics;
                g.ResetClip();

                // Draw background
                g.SetClip(mImageListView.layoutManager.ClientArea);
                DrawBackground(g, mImageListView.layoutManager.ClientArea);

                // Draw column headers
                if (mImageListView.View == View.Details)
                {
                    int x = mImageListView.layoutManager.ColumnHeaderBounds.Left;
                    int y = mImageListView.layoutManager.ColumnHeaderBounds.Top;
                    int h = MeasureColumnHeaderHeight();
                    int lastX = 0;
                    foreach (ImageListViewColumnHeader column in mImageListView.Columns.GetUIColumns())
                    {
                        ColumnState state = ColumnState.None;
                        if (column.Hovered)
                            state |= ColumnState.Hovered;
                        if (mImageListView.nav.HoveredSeparator == column.Type)
                            state |= ColumnState.SeparatorHovered;
                        if (mImageListView.nav.SelSeperator == column.Type)
                            state |= ColumnState.SeparatorSelected;

                        Rectangle bounds = new Rectangle(x, y, column.Width, h);
                        if (mClip)
                        {
                            Rectangle clip = Rectangle.Intersect(bounds, mImageListView.layoutManager.ClientArea);
                            g.SetClip(clip);
                        }
                        else
                            g.SetClip(mImageListView.layoutManager.ClientArea);
                        DrawColumnHeader(g, column, state, bounds);
                        x += column.Width;
                        lastX = bounds.Right;
                    }

                    // Extender column
                    if (mImageListView.Columns.Count != 0)
                    {
                        if (lastX < mImageListView.layoutManager.ClientArea.Right)
                        {
                            Rectangle extender = new Rectangle(
                                lastX,
                                mImageListView.layoutManager.ColumnHeaderBounds.Top,
                                mImageListView.layoutManager.ClientArea.Right - lastX,
                                mImageListView.layoutManager.ColumnHeaderBounds.Height);
                            if (mClip)
                                g.SetClip(extender);
                            else
                                g.SetClip(mImageListView.layoutManager.ClientArea);
                            DrawColumnExtender(g, extender);
                        }
                    }
                    else
                    {
                        Rectangle extender = mImageListView.layoutManager.ColumnHeaderBounds;
                        if (mClip)
                            g.SetClip(extender);
                        else
                            g.SetClip(mImageListView.layoutManager.ClientArea);
                        DrawColumnExtender(g, extender);
                    }
                }

                // Draw items
                if (mImageListView.Items.Count > 0 &&
                    (mImageListView.View == View.Thumbnails || mImageListView.View == View.Gallery ||
                    (mImageListView.View == View.Details && mImageListView.Columns.GetUIColumns().Count != 0)) &&
                    mImageListView.layoutManager.FirstPartiallyVisible != -1 &&
                    mImageListView.layoutManager.LastPartiallyVisible != -1)
                {
                    List<DrawItemParams> drawItemParams = new List<DrawItemParams>();
                    for (int i = mImageListView.layoutManager.FirstPartiallyVisible; i <= mImageListView.layoutManager.LastPartiallyVisible; i++)
                    {
                        ImageListViewItem item = mImageListView.Items[i];

                        // Determine item state
                        ItemState state = ItemState.None;
                        bool isSelected;
                        if (mImageListView.nav.Highlight.TryGetValue(item, out isSelected))
                        {
                            if (isSelected)
                                state |= ItemState.Selected;
                        }
                        else if (item.Selected)
                            state |= ItemState.Selected;

                        if (item.Hovered && mImageListView.nav.Dragging == false)
                            state |= ItemState.Hovered;

                        if (item.Focused)
                            state |= ItemState.Focused;

                        // Get item bounds
                        Rectangle bounds = mImageListView.layoutManager.GetItemBounds(i);

                        // Add to params to be sorted and drawn
                        drawItemParams.Add(new DrawItemParams(item, state, bounds));
                    }

                    // Sort items by draw order
                    drawItemParams.Sort(new ItemDrawOrderComparer(mItemDrawOrder));

                    // Draw items
                    foreach (DrawItemParams param in drawItemParams)
                    {
                        if (mClip)
                        {
                            Rectangle clip = Rectangle.Intersect(param.Bounds, mImageListView.layoutManager.ItemAreaBounds);
                            g.SetClip(clip);
                        }
                        else
                            g.SetClip(mImageListView.layoutManager.ClientArea);
                        DrawItem(g, param.Item, param.State, param.Bounds);
                    }
                }

                // Draw the large preview image in Gallery mode
                if (mImageListView.View == View.Gallery && mImageListView.Items.Count != 0)
                {
                    ImageListViewItem item = null;
                    if (mImageListView.Items.FocusedItem != null)
                        item = mImageListView.Items.FocusedItem;
                    else if (mImageListView.SelectedItems.Count != 0)
                        item = mImageListView.SelectedItems[0];
                    else
                        item = mImageListView.Items[0];

                    Rectangle bounds = mImageListView.layoutManager.ClientArea;
                    bounds.Height -= mImageListView.layoutManager.ItemAreaBounds.Height;

                    Image image = GetImageAsync(item, bounds.Size);
                    if (image == null) image = item.ThumbnailImage;

                    if (mClip)
                        g.SetClip(bounds);
                    else
                        g.SetClip(mImageListView.layoutManager.ClientArea);
                    DrawGalleryImage(g, item, image, bounds);
                }

                // Scrollbar filler
                if (mImageListView.hScrollBar.Visible && mImageListView.vScrollBar.Visible)
                {
                    Rectangle bounds = mImageListView.layoutManager.ClientArea;
                    Rectangle filler = new Rectangle(bounds.Right, bounds.Bottom, mImageListView.vScrollBar.Width, mImageListView.hScrollBar.Height);
                    g.SetClip(filler);
                    DrawScrollBarFiller(g, filler);
                }

                // Draw the selection rectangle
                if (mImageListView.nav.Dragging)
                {
                    Rectangle sel = new Rectangle(
                        System.Math.Min(mImageListView.nav.SelStart.X, mImageListView.nav.SelEnd.X),
                        System.Math.Min(mImageListView.nav.SelStart.Y, mImageListView.nav.SelEnd.Y),
                        System.Math.Abs(mImageListView.nav.SelStart.X - mImageListView.nav.SelEnd.X),
                        System.Math.Abs(mImageListView.nav.SelStart.Y - mImageListView.nav.SelEnd.Y));
                    if (sel.Height > 0 && sel.Width > 0)
                    {
                        if (mClip)
                        {
                            Rectangle selclip = new Rectangle(sel.Left, sel.Top, sel.Width + 1, sel.Height + 1);
                            g.SetClip(selclip);
                        }
                        else
                            g.SetClip(mImageListView.layoutManager.ClientArea);
                        g.ExcludeClip(mImageListView.layoutManager.ColumnHeaderBounds);
                        DrawSelectionRectangle(g, sel);
                    }
                }

                // Draw the insertion caret
                if (mImageListView.nav.DragIndex != -1)
                {
                    int i = mImageListView.nav.DragIndex;
                    Rectangle bounds = bounds = mImageListView.layoutManager.GetItemBounds(i);
                    if (mImageListView.nav.DragCaretOnRight)
                        bounds.Offset(mImageListView.layoutManager.ItemSizeWithMargin.Width, 0);
                    bounds.Offset(-mImageListView.ItemMargin.Width, 0);
                    bounds.Width = mImageListView.ItemMargin.Width;
                    if (mClip)
                        g.SetClip(bounds);
                    else
                        g.SetClip(mImageListView.layoutManager.ClientArea);
                    DrawInsertionCaret(g, bounds);
                }

                // Draw the overlay image
                g.SetClip(mImageListView.layoutManager.ClientArea);
                DrawOverlay(g, mImageListView.layoutManager.ClientArea);

                // Draw on to the control
                bufferGraphics.Render(graphics);
            }
            /// <summary>
            /// Destroys the current buffer and creates a new buffered graphics 
            /// sized to the client area of the owner control.
            /// </summary>
            internal void RecreateBuffer()
            {
                bufferContext = BufferedGraphicsManager.Current;

                if (disposed)
                    throw (new ObjectDisposedException("bufferContext"));

                int width = System.Math.Max(mImageListView.Width, 1);
                int height = System.Math.Max(mImageListView.Height, 1);

                bufferContext.MaximumBuffer = new Size(width, height);

                if (bufferGraphics != null) bufferGraphics.Dispose();
                bufferGraphics = bufferContext.Allocate(mImageListView.CreateGraphics(), new Rectangle(0, 0, width, height));

                InitializeGraphics(bufferGraphics.Graphics);
            }
            /// <summary>
            /// Releases buffered graphics objects.
            /// </summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;

                if (bufferGraphics != null)
                    bufferGraphics.Dispose();

                OnDispose();
            }
            #endregion

            #region Virtual Methods
            /// <summary>
            /// Initializes the System.Drawing.Graphics used to draw
            /// control elements.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            public virtual void InitializeGraphics(Graphics g)
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.High;
            }
            /// <summary>
            /// Returns the height of column headers.
            /// </summary>
            public virtual int MeasureColumnHeaderHeight()
            {
                if (mImageListView.HeaderFont == null)
                    return 24;
                else
                    return System.Math.Max(mImageListView.HeaderFont.Height + 4, 24);
            }
            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the item measurement should be made.</param>
            public virtual Size MeasureItem(View view)
            {
                Size itemSize = new Size();

                // Reference text height
                int textHeight = mImageListView.Font.Height;

                if (view == View.Thumbnails || view == View.Gallery)
                {
                    // Calculate item size
                    Size itemPadding = new Size(4, 4);
                    itemSize = mImageListView.ThumbnailSize + itemPadding + itemPadding;
                    itemSize.Height += textHeight + System.Math.Max(4, textHeight / 3); // textHeight / 3 = vertical space between thumbnail and text
                }
                else if (view == View.Details)
                {
                    // Calculate total column width
                    int colWidth = 0;
                    foreach (ImageListViewColumnHeader column in mImageListView.Columns)
                        if (column.Visible) colWidth += column.Width;

                    // Calculate item size
                    itemSize = new Size(colWidth, textHeight + 2 * textHeight / 6); // textHeight / 6 = vertical space between item border and text
                }

                return itemSize;
            }
            /// <summary>
            /// Draws the background of the control.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The client coordinates of the item area.</param>
            public virtual void DrawBackground(Graphics g, Rectangle bounds)
            {
                // Clear the background
                g.Clear(mImageListView.BackColor);

                // Draw the background image
                if (ImageListView.BackgroundImage != null)
                {
                    Image img = ImageListView.BackgroundImage;

                    if (ImageListView.BackgroundImageLayout == ImageLayout.None)
                    {
                        g.DrawImageUnscaled(img, ImageListView.layoutManager.ItemAreaBounds.Location);
                    }
                    else if (ImageListView.BackgroundImageLayout == ImageLayout.Center)
                    {
                        int x = bounds.Left + (bounds.Width - img.Width) / 2;
                        int y = bounds.Top + (bounds.Height - img.Height) / 2;
                        g.DrawImageUnscaled(img, x, y);
                    }
                    else if (ImageListView.BackgroundImageLayout == ImageLayout.Stretch)
                    {
                        g.DrawImage(img, bounds);
                    }
                    else if (ImageListView.BackgroundImageLayout == ImageLayout.Tile)
                    {
                        using (Brush imgBrush = new TextureBrush(img, WrapMode.Tile))
                        {
                            g.FillRectangle(imgBrush, bounds);
                        }
                    }
                    else if (ImageListView.BackgroundImageLayout == ImageLayout.Zoom)
                    {
                        float xscale = (float)bounds.Width / (float)img.Width;
                        float yscale = (float)bounds.Height / (float)img.Height;
                        float scale = Math.Min(xscale, yscale);
                        int width = (int)(((float)img.Width) * scale);
                        int height = (int)(((float)img.Height) * scale);
                        int x = bounds.Left + (bounds.Width - width) / 2;
                        int y = bounds.Top + (bounds.Height - height) / 2;
                        g.DrawImage(img, x, y, width, height);
                    }
                }
            }
            /// <summary>
            /// Draws the selection rectangle.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="selection">The client coordinates of the selection rectangle.</param>
            public virtual void DrawSelectionRectangle(Graphics g, Rectangle selection)
            {
                using (Brush bSelection = new SolidBrush(Color.FromArgb(128, SystemColors.Highlight)))
                {
                    g.FillRectangle(bSelection, selection);
                    g.DrawRectangle(SystemPens.Highlight, selection);
                }
            }
            /// <summary>
            /// Draws the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="state">The current view state of item.</param>
            /// <param name="bounds">The bounding rectangle of item in client coordinates.</param>
            public virtual void DrawItem(Graphics g, ImageListViewItem item, ItemState state, Rectangle bounds)
            {
                Size itemPadding = new Size(4, 4);

                // Paint background
                using (Brush bItemBack = new SolidBrush(item.BackColor))
                {
                    g.FillRectangle(bItemBack, bounds);
                }
                if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                {
                    using (Brush bSelected = new LinearGradientBrush(bounds, Color.FromArgb(16, SystemColors.Highlight), Color.FromArgb(64, SystemColors.Highlight), LinearGradientMode.Vertical))
                    {
                        Utility.FillRoundedRectangle(g, bSelected, bounds, (mImageListView.View == View.Details ? 2 : 4));
                    }
                }
                else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                {
                    using (Brush bGray64 = new LinearGradientBrush(bounds, Color.FromArgb(16, SystemColors.GrayText), Color.FromArgb(64, SystemColors.GrayText), LinearGradientMode.Vertical))
                    {
                        Utility.FillRoundedRectangle(g, bGray64, bounds, (mImageListView.View == View.Details ? 2 : 4));
                    }
                }
                if (((state & ItemState.Hovered) != ItemState.None))
                {
                    using (Brush bHovered = new LinearGradientBrush(bounds, Color.FromArgb(8, SystemColors.Highlight), Color.FromArgb(32, SystemColors.Highlight), LinearGradientMode.Vertical))
                    {
                        Utility.FillRoundedRectangle(g, bHovered, bounds, (mImageListView.View == View.Details ? 2 : 4));
                    }
                }

                if (mImageListView.View == View.Thumbnails || mImageListView.View == View.Gallery)
                {
                    // Draw the image
                    Image img = item.ThumbnailImage;
                    if (img != null)
                    {
                        int x = bounds.Left + itemPadding.Width + (mImageListView.ThumbnailSize.Width - img.Width) / 2;
                        int y = bounds.Top + itemPadding.Height + (mImageListView.ThumbnailSize.Height - img.Height) / 2;
                        g.DrawImageUnscaled(img, x, y);
                        // Draw image border
                        if (img.Width > 32)
                        {
                            using (Pen pGray128 = new Pen(Color.FromArgb(128, Color.Gray)))
                            {
                                g.DrawRectangle(pGray128, x, y, img.Width, img.Height);
                            }
                            if (System.Math.Min(mImageListView.ThumbnailSize.Width, mImageListView.ThumbnailSize.Height) > 32)
                            {
                                using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                                {
                                    g.DrawRectangle(pWhite128, x + 1, y + 1, img.Width - 2, img.Height - 2);
                                }
                            }
                        }
                    }

                    // Draw item text
                    SizeF szt = TextRenderer.MeasureText(item.Text, mImageListView.Font);
                    RectangleF rt;
                    using (StringFormat sf = new StringFormat())
                    {
                        rt = new RectangleF(bounds.Left + itemPadding.Width, bounds.Top + 2 * itemPadding.Height + mImageListView.ThumbnailSize.Height, mImageListView.ThumbnailSize.Width, szt.Height);
                        sf.Alignment = StringAlignment.Center;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        using (Brush bItemFore = new SolidBrush(item.ForeColor))
                        {
                            g.DrawString(item.Text, mImageListView.Font, bItemFore, rt, sf);
                        }
                    }
                }
                else if (mImageListView.View == View.Details)
                {
                    // Separators 
                    int x = mImageListView.layoutManager.ColumnHeaderBounds.Left;
                    List<ImageListViewColumnHeader> uicolumns = mImageListView.Columns.GetUIColumns();
                    foreach (ImageListViewColumnHeader column in uicolumns)
                    {
                        x += column.Width;
                        if (!ReferenceEquals(column, uicolumns[uicolumns.Count - 1]))
                        {
                            using (Pen pGray32 = new Pen(Color.FromArgb(32, SystemColors.GrayText)))
                            {
                                g.DrawLine(pGray32, x, bounds.Top, x, bounds.Bottom);
                            }
                        }
                    }
                    Size offset = new Size(2, (bounds.Height - mImageListView.Font.Height) / 2);
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Alignment = StringAlignment.Near;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        // Sub text
                        RectangleF rt = new RectangleF(bounds.Left + offset.Width, bounds.Top + offset.Height, uicolumns[0].Width - 2 * offset.Width, bounds.Height - 2 * offset.Height);
                        foreach (ImageListViewColumnHeader column in uicolumns)
                        {
                            rt.Width = column.Width - 2 * offset.Width;
                            using (Brush bItemFore = new SolidBrush(item.ForeColor))
                            {
                                g.DrawString(item.GetSubItemText(column.Type), mImageListView.Font, bItemFore, rt, sf);
                            }
                            rt.X += column.Width;
                        }
                    }
                }

                // Item border
                using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                {
                    Utility.DrawRoundedRectangle(g, pWhite128, bounds.Left + 1, bounds.Top + 1, bounds.Width - 3, bounds.Height - 3, (mImageListView.View == View.Details ? 2 : 4));
                }
                if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                {
                    using (Pen pHighlight128 = new Pen(Color.FromArgb(128, SystemColors.Highlight)))
                    {
                        Utility.DrawRoundedRectangle(g, pHighlight128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, (mImageListView.View == View.Details ? 2 : 4));
                    }
                }
                else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                {
                    using (Pen pGray128 = new Pen(Color.FromArgb(128, SystemColors.GrayText)))
                    {
                        Utility.DrawRoundedRectangle(g, pGray128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, (mImageListView.View == View.Details ? 2 : 4));
                    }
                }
                else if (mImageListView.View == View.Thumbnails && (state & ItemState.Selected) == ItemState.None)
                {
                    using (Pen pGray64 = new Pen(Color.FromArgb(64, SystemColors.GrayText)))
                    {
                        Utility.DrawRoundedRectangle(g, pGray64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, (mImageListView.View == View.Details ? 2 : 4));
                    }
                }

                if (mImageListView.Focused && ((state & ItemState.Hovered) != ItemState.None))
                {
                    using (Pen pHighlight64 = new Pen(Color.FromArgb(64, SystemColors.Highlight)))
                    {
                        Utility.DrawRoundedRectangle(g, pHighlight64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, (mImageListView.View == View.Details ? 2 : 4));
                    }
                }

                // Focus rectangle
                if (mImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
                {
                    ControlPaint.DrawFocusRectangle(g, bounds);
                }
            }
            /// <summary>
            /// Draws the column headers.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="column">The ImageListViewColumnHeader to draw.</param>
            /// <param name="state">The current view state of column.</param>
            /// <param name="bounds">The bounding rectangle of column in client coordinates.</param>
            public virtual void DrawColumnHeader(Graphics g, ImageListViewColumnHeader column, ColumnState state, Rectangle bounds)
            {
                // Paint background
                if (mImageListView.Focused && ((state & ColumnState.Hovered) == ColumnState.Hovered))
                {
                    using (Brush bHovered = new LinearGradientBrush(bounds, Color.FromArgb(16, SystemColors.Highlight), Color.FromArgb(64, SystemColors.Highlight), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bHovered, bounds);
                    }
                }
                else
                {
                    using (Brush bNormal = new LinearGradientBrush(bounds, Color.FromArgb(32, SystemColors.Control), Color.FromArgb(196, SystemColors.Control), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bNormal, bounds);
                    }
                }
                using (Brush bBorder = new LinearGradientBrush(bounds, SystemColors.ControlLightLight, SystemColors.ControlDark, LinearGradientMode.Vertical))
                using (Pen pBorder = new Pen(bBorder))
                {
                    g.DrawLine(pBorder, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                    g.DrawLine(pBorder, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                }
                g.DrawLine(SystemPens.ControlLightLight, bounds.Left + 1, bounds.Top + 1, bounds.Left + 1, bounds.Bottom - 2);
                g.DrawLine(SystemPens.ControlLightLight, bounds.Right - 1, bounds.Top + 1, bounds.Right - 1, bounds.Bottom - 2);

                // Sort image
                int textOffset = 4;
                if (column.Type == mImageListView.SortColumn && mImageListView.SortOrder != SortOrder.None)
                {
                    Image img = GetSortArrowImage(mImageListView.SortOrder);
                    if (img != null)
                    {
                        g.DrawImageUnscaled(img, bounds.X + 4, bounds.Top + (bounds.Height - img.Height) / 2);
                        textOffset += img.Width;
                    }
                }

                // Text
                bounds.X += textOffset;
                bounds.Width -= textOffset;
                if (bounds.Width > 4)
                {
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Alignment = StringAlignment.Near;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        g.DrawString(column.Text, (mImageListView.HeaderFont == null ? mImageListView.Font : mImageListView.HeaderFont), SystemBrushes.WindowText, bounds, sf);
                    }
                }
            }
            /// <summary>
            /// Draws the large preview image of the focused item in Gallery mode.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="image">The image to draw.</param>
            /// <param name="bounds">The bounding rectangle of the preview area.</param>
            public virtual void DrawGalleryImage(Graphics g, ImageListViewItem item, Image image, Rectangle bounds)
            {
                // Calculate image bounds
                float xscale = (float)(bounds.Width - 2 * mImageListView.ItemMargin.Width) / (float)image.Width;
                float yscale = (float)(bounds.Height - 2 * mImageListView.ItemMargin.Height) / (float)image.Height;
                float scale = Math.Min(xscale, yscale);
                if (scale > 1.0f) scale = 1.0f;
                int imageWidth = (int)((float)image.Width * scale);
                int imageHeight = (int)((float)image.Height * scale);
                int imageX = bounds.Left + (bounds.Width - imageWidth) / 2;
                int imageY = bounds.Top + (bounds.Height - imageHeight) / 2;
                // Draw image
                g.DrawImage(image, imageX, imageY, imageWidth, imageHeight);
                // Draw image border
                if (Math.Min(imageWidth, imageHeight) > 32)
                {
                    using (Pen pGray128 = new Pen(Color.FromArgb(128, Color.Gray)))
                    {
                        g.DrawRectangle(pGray128, imageX, imageY, imageWidth, imageHeight);
                    }
                    using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                    {
                        g.DrawRectangle(pWhite128, imageX + 1, imageY + 1, imageWidth - 2, imageHeight - 2);
                    }
                }
            }
            /// <summary>
            /// Draws the extender after the last column.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of extender column in client coordinates.</param>
            public virtual void DrawColumnExtender(Graphics g, Rectangle bounds)
            {
                // Paint background
                using (Brush bBack = new LinearGradientBrush(bounds, Color.FromArgb(32, SystemColors.Control), Color.FromArgb(196, SystemColors.Control), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bBack, bounds);
                }
                using (Brush bBorder = new LinearGradientBrush(bounds, SystemColors.ControlLightLight, SystemColors.ControlDark, LinearGradientMode.Vertical))
                using (Pen pBorder = new Pen(bBorder))
                {
                    g.DrawLine(pBorder, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                    g.DrawLine(pBorder, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                }
                g.DrawLine(SystemPens.ControlLightLight, bounds.Left + 1, bounds.Top + 1, bounds.Left + 1, bounds.Bottom - 2);
                g.DrawLine(SystemPens.ControlLightLight, bounds.Right - 1, bounds.Top + 1, bounds.Right - 1, bounds.Bottom - 2);
            }
            /// <summary>
            /// Draws the area between the vertical and horizontal scrollbars.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of the filler in client coordinates.</param>
            public virtual void DrawScrollBarFiller(Graphics g, Rectangle bounds)
            {
                g.FillRectangle(SystemBrushes.Control, bounds);
            }
            /// <summary>
            /// Gets the image representing the sort arrow on column headers.
            /// </summary>
            /// <param name="sortOrder">The SortOrder for which the sort arrow image should be returned.</param>
            /// <returns>The sort arrow image representing sortOrder.</returns>
            public virtual Image GetSortArrowImage(SortOrder sortOrder)
            {
                if (mImageListView.SortOrder == SortOrder.Ascending)
                {
                    return Utility.ImageFromBase64String(@"iVBORw0KGgoAAAANSUhEUgAAAAoAAAAGCAYAAAD68A/GAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAgY0hSTQAAeiYAAICEAAD6AAAAgOgAAHUwAADqYAAAOpgAABdwnLpRPAAAAHNJREFUGFdjYCAXFB9LP1lwKPUIXv1FR9Mudpyq+996ovp/xo7Y8xiKS45nsBUdSbvdfqrm//Jr8/4vvTLnf+2B4v9xa0JvRi4LYINrKDycujtvf9L35mOV/xdfmfV/4eUZ/9sO1/6PWOL/PXie1w6SvAEA+BE3G3fNEd8AAAAASUVORK5CYII=");
                }
                else if (mImageListView.SortOrder == SortOrder.Descending)
                    return Utility.ImageFromBase64String(@"iVBORw0KGgoAAAANSUhEUgAAAAoAAAAGCAYAAAD68A/GAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAgY0hSTQAAeiYAAICEAAD6AAAAgOgAAHUwAADqYAAAOpgAABdwnLpRPAAAAHNJREFUGFdjYCAFeE1y2uHaY/s9Z23q/6ZDtf8bDlb9D5jk/V8nV/27RobKbrhZzl02bHbNFjfDZwb+rztQ+b9mf9l/7163/+ppyreVkxTYMCw1LtU979vv8d+rx/W/WqrSRbyu0sxUPaKaoniSFKejqAUAXY8qTCsVRMkAAAAASUVORK5CYII=");
                return
                    null;
            }
            /// <summary>
            /// Draws the insertion caret for drag & drop operations.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of the insertion caret.</param>
            public virtual void DrawInsertionCaret(Graphics g, Rectangle bounds)
            {
                using (Brush b = new SolidBrush(SystemColors.Highlight))
                {
                    bounds.X = bounds.X + bounds.Width / 2 - 1;
                    bounds.Width = 2;
                    g.FillRectangle(b, bounds);
                }
            }
            /// <summary>
            /// Draws an overlay image over the client area.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of the client area.</param>
            public virtual void DrawOverlay(Graphics g, Rectangle bounds)
            {
                ;
            }
            /// <summary>
            /// Releases managed resources.
            /// </summary>
            public virtual void OnDispose()
            {
                ;
            }
            /// <summary>
            /// Sets the layout of the control.
            /// </summary>
            /// <param name="e">A LayoutEventArgs that contains event data.</param>
            public virtual void OnLayout(LayoutEventArgs e)
            {
                ;
            }
            #endregion
        }
    }
}
