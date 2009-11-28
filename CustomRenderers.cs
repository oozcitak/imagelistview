using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading;

namespace Manina.Windows.Forms
{
    public static class ImageListViewRenderers
    {
        #region DefaultRenderer
        /// <summary>
        /// The default renderer.
        /// </summary>
        public class DefaultRenderer : ImageListView.ImageListViewRenderer
        {
            public DefaultRenderer()
            {
                ;
            }
        }
        #endregion

        #region TilesRenderer
        /// <summary>
        /// Displays items with large tiles.
        /// </summary>
        public class TilesRenderer : ImageListView.ImageListViewRenderer
        {
            private Font mCaptionFont;
            private int mTileWidth;
            private int mTextHeight;

            private Font CaptionFont
            {
                get
                {
                    if (mCaptionFont == null)
                        mCaptionFont = new Font(mImageListView.Font, FontStyle.Bold);
                    return mCaptionFont;
                }
            }

            public TilesRenderer()
                : this(180)
            {
                ;
            }

            public TilesRenderer(int tileWidth)
                : base()
            {
                mTileWidth = tileWidth;
            }

            public override void OnDispose()
            {
                if (mCaptionFont != null)
                    mCaptionFont.Dispose();
            }

            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the item measurement should be made.</param>
            public override Size MeasureItem(View view)
            {
                if (view == View.Thumbnails)
                {
                    Size itemSize = new Size();
                    mTextHeight = (int)(5.8f * (float)CaptionFont.Height);

                    // Calculate item size
                    Size itemPadding = new Size(4, 4);
                    itemSize.Width = mImageListView.ThumbnailSize.Width + 4 * itemPadding.Width + mTileWidth;
                    itemSize.Height = Math.Max(mTextHeight, mImageListView.ThumbnailSize.Height) + itemPadding.Height;
                    return itemSize;
                }
                else
                    return base.MeasureItem(view);
            }
            /// <summary>
            /// Draws the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="state">The current view state of item.</param>
            /// <param name="bounds">The bounding rectangle of item in client coordinates.</param>
            public override void DrawItem(Graphics g, ImageListViewItem item, ItemState state, Rectangle bounds)
            {
                if (ImageListView.View == View.Thumbnails)
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
                            Utility.FillRoundedRectangle(g, bSelected, bounds, 4);
                        }
                    }
                    else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Brush bGray64 = new LinearGradientBrush(bounds, Color.FromArgb(16, SystemColors.GrayText), Color.FromArgb(64, SystemColors.GrayText), LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bGray64, bounds, 4);
                        }
                    }
                    if (((state & ItemState.Hovered) != ItemState.None))
                    {
                        using (Brush bHovered = new LinearGradientBrush(bounds, Color.FromArgb(8, SystemColors.Highlight), Color.FromArgb(32, SystemColors.Highlight), LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bHovered, bounds, 4);
                        }
                    }

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

                        // Draw item text
                        int lineHeight = CaptionFont.Height;
                        RectangleF rt;
                        StringFormat sf = new StringFormat();
                        rt = new RectangleF(bounds.Left + 2 * itemPadding.Width + mImageListView.ThumbnailSize.Width,
                            bounds.Top + itemPadding.Height + (Math.Max(mImageListView.ThumbnailSize.Height, mTextHeight) - mTextHeight) / 2,
                            mTileWidth, lineHeight);
                        sf.Alignment = StringAlignment.Near;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        using (Brush bItemFore = new SolidBrush(item.ForeColor))
                        {
                            g.DrawString(item.Text, CaptionFont, bItemFore, rt, sf);
                        }
                        using (Brush bItemDetails = new SolidBrush(Color.Gray))
                        {
                            rt.Offset(0, 1.5f * lineHeight);
                            g.DrawString(item.GetSubItemText(ColumnType.FileType),
                                mImageListView.Font, bItemDetails, rt, sf);
                            rt.Offset(0, 1.1f * lineHeight);
                            g.DrawString(string.Format("{0} pixels, {1} dpi", item.GetSubItemText(ColumnType.Dimensions), item.Resolution.Width),
                                mImageListView.Font, bItemDetails, rt, sf);
                            rt.Offset(0, 1.1f * lineHeight);
                            g.DrawString(item.GetSubItemText(ColumnType.FileSize),
                                mImageListView.Font, bItemDetails, rt, sf);
                            rt.Offset(0, 1.1f * lineHeight);
                            g.DrawString(item.GetSubItemText(ColumnType.DateModified),
                                mImageListView.Font, bItemDetails, rt, sf);
                        }
                    }

                    // Item border
                    using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                    {
                        Utility.DrawRoundedRectangle(g, pWhite128, bounds.Left + 1, bounds.Top + 1, bounds.Width - 3, bounds.Height - 3, 4);
                    }
                    if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pHighlight128 = new Pen(Color.FromArgb(128, SystemColors.Highlight)))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pGray128 = new Pen(Color.FromArgb(128, SystemColors.GrayText)))
                        {
                            Utility.DrawRoundedRectangle(g, pGray128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if ((state & ItemState.Selected) == ItemState.None)
                    {
                        using (Pen pGray64 = new Pen(Color.FromArgb(64, SystemColors.GrayText)))
                        {
                            Utility.DrawRoundedRectangle(g, pGray64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }

                    if (mImageListView.Focused && ((state & ItemState.Hovered) != ItemState.None))
                    {
                        using (Pen pHighlight64 = new Pen(Color.FromArgb(64, SystemColors.Highlight)))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }

                    // Focus rectangle
                    if (mImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
                    {
                        ControlPaint.DrawFocusRectangle(g, bounds);
                    }
                }
                else
                    base.DrawItem(g, item, state, bounds);
            }
        }
        #endregion

        #region XPRenderer
        /// <summary>
        /// Mimics Windows XP appearance.
        /// </summary>
        public class XPRenderer : ImageListView.ImageListViewRenderer
        {
            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the item measurement should be made.</param>
            public override Size MeasureItem(View view)
            {
                Size itemSize = new Size();

                // Reference text height
                int textHeight = mImageListView.Font.Height;

                if (view == View.Thumbnails)
                {
                    // Calculate item size
                    Size itemPadding = new Size(4, 4);
                    itemSize = mImageListView.ThumbnailSize + itemPadding + itemPadding;
                    itemSize.Height += textHeight + System.Math.Max(4, textHeight / 3) + itemPadding.Height; // textHeight / 3 = vertical space between thumbnail and text
                    return itemSize;
                }
                else
                    return base.MeasureItem(view);
            }
            /// <summary>
            /// Draws the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="state">The current view state of item.</param>
            /// <param name="bounds">The bounding rectangle of item in client coordinates.</param>
            public override void DrawItem(System.Drawing.Graphics g, ImageListViewItem item, ItemState state, System.Drawing.Rectangle bounds)
            {
                // Paint background
                using (Brush bItemBack = new SolidBrush(item.BackColor))
                {
                    g.FillRectangle(bItemBack, bounds);
                }

                if (mImageListView.View == View.Thumbnails)
                {
                    Size itemPadding = new Size(4, 4);

                    // Draw the image
                    Image img = item.ThumbnailImage;
                    if (img != null)
                    {
                        int x = bounds.Left + itemPadding.Width + (mImageListView.ThumbnailSize.Width - img.Width) / 2;
                        int y = bounds.Top + itemPadding.Height + (mImageListView.ThumbnailSize.Height - img.Height) / 2;
                        Rectangle imageBounds = new Rectangle(bounds.Location + itemPadding, mImageListView.ThumbnailSize);
                        g.DrawImageUnscaled(img, x, y);
                        // Draw image border
                        if (img.Width > 32)
                        {
                            if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                            {
                                using (Pen pen = new Pen(SystemColors.Highlight, 3))
                                {
                                    g.DrawRectangle(pen, imageBounds);
                                }
                            }
                            else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                            {
                                using (Pen pen = new Pen(SystemColors.GrayText, 3))
                                {
                                    pen.Alignment = PenAlignment.Center;
                                    g.DrawRectangle(pen, imageBounds);
                                }
                            }
                            else
                            {
                                using (Pen pGray128 = new Pen(Color.FromArgb(128, SystemColors.GrayText)))
                                {
                                    g.DrawRectangle(pGray128, imageBounds);
                                }
                            }
                        }
                    }

                    // Draw item text
                    SizeF szt = TextRenderer.MeasureText(item.Text, mImageListView.Font);
                    RectangleF rt;
                    StringFormat sf = new StringFormat();
                    rt = new RectangleF(bounds.Left + itemPadding.Width, bounds.Top + 3 * itemPadding.Height + mImageListView.ThumbnailSize.Height, mImageListView.ThumbnailSize.Width, szt.Height);
                    sf.Alignment = StringAlignment.Center;
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    rt.Width += 1;
                    rt.Inflate(1, 2);
                    if (mImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
                        rt.Inflate(-1, -1);
                    if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        g.FillRectangle(SystemBrushes.Highlight, rt);
                    }
                    else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        g.FillRectangle(SystemBrushes.GrayText, rt);
                    }
                    if (((state & ItemState.Selected) != ItemState.None))
                    {
                        g.DrawString(item.Text, mImageListView.Font, SystemBrushes.HighlightText, rt, sf);
                    }
                    else
                    {
                        using (Brush bItemFore = new SolidBrush(item.ForeColor))
                        {
                            g.DrawString(item.Text, mImageListView.Font, bItemFore, rt, sf);
                        }
                    }

                    if (mImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
                    {
                        Rectangle fRect = Rectangle.Round(rt);
                        fRect.Inflate(1, 1);
                        ControlPaint.DrawFocusRectangle(g, fRect);
                    }
                }
                else
                {
                    if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        g.FillRectangle(SystemBrushes.Highlight, bounds);
                    }
                    else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        g.FillRectangle(SystemBrushes.GrayText, bounds);
                    }

                    Size offset = new Size(2, (bounds.Height - mImageListView.Font.Height) / 2);
                    StringFormat sf = new StringFormat();
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    sf.Alignment = StringAlignment.Near;
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    // Sub text
                    List<Manina.Windows.Forms.ImageListView.ImageListViewColumnHeader> uicolumns = mImageListView.Columns.GetUIColumns();
                    RectangleF rt = new RectangleF(bounds.Left + offset.Width, bounds.Top + offset.Height, uicolumns[0].Width - 2 * offset.Width, bounds.Height - 2 * offset.Height);
                    foreach (Manina.Windows.Forms.ImageListView.ImageListViewColumnHeader column in uicolumns)
                    {
                        rt.Width = column.Width - 2 * offset.Width;
                        using (Brush bItemFore = new SolidBrush(item.ForeColor))
                        {
                            if ((state & ItemState.Selected) == ItemState.None)
                                g.DrawString(item.GetSubItemText(column.Type), mImageListView.Font, bItemFore, rt, sf);
                            else
                                g.DrawString(item.GetSubItemText(column.Type), mImageListView.Font, SystemBrushes.HighlightText, rt, sf);
                        }
                        rt.X += column.Width;
                    }

                    if (mImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
                        ControlPaint.DrawFocusRectangle(g, bounds);
                }
            }
        }
        #endregion

        #region ZoomingRenderer
        /// <summary>
        /// Zooms items on mouse over.
        /// </summary>
        public class ZoomingRenderer : ImageListView.ImageListViewRenderer
        {
            private string requestedFileName = null;
            private string cachedFileName = null;
            private Image cachedImage = null;
            private object lockObject = new object();
            private Thread cacheThread;
            private float mZoomRatio;

            public ZoomingRenderer()
                : this(0.5f)
            {
                ;
            }

            public ZoomingRenderer(float zoomRatio)
                : base()
            {
                if (zoomRatio < 0.0f) zoomRatio = 0.0f;
                if (zoomRatio > 1.0f) zoomRatio = 1.0f;
                mZoomRatio = zoomRatio;
                cacheThread = new Thread(new ParameterizedThreadStart(DoWork));
                cacheThread.IsBackground = true;
                cacheThread.Start(this);
                while (!cacheThread.IsAlive) ;
            }

            private static void DoWork(object sender)
            {
                ZoomingRenderer renderer = (ZoomingRenderer)sender;
                while (true)
                {
                    lock (renderer.lockObject)
                    {
                        Monitor.Wait(renderer.lockObject);
                        if (renderer.requestedFileName != renderer.cachedFileName)
                        {
                            renderer.cachedImage = Image.FromFile(renderer.requestedFileName);
                            renderer.cachedFileName = renderer.requestedFileName;
                        }
                    }

                    renderer.mImageListView.BeginInvoke(new RefreshEventHandlerInternal(renderer.mImageListView.OnRefreshInternal));
                }
            }

            public override void OnDispose()
            {
                base.OnDispose();

                if (cacheThread.IsAlive)
                {
                    cacheThread.Abort();
                    cacheThread.Join();
                }
            }

            /// <summary>
            /// Initializes the System.Drawing.Graphics used to draw
            /// control elements.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            public override void InitializeGraphics(System.Drawing.Graphics g)
            {
                base.InitializeGraphics(g);

                Clip = false;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                ItemDrawOrder = ItemDrawOrder.NormalSelectedHovered;
            }
            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the item measurement should be made.</param>
            /// <returns></returns>
            public override Size MeasureItem(View view)
            {
                if (view == View.Thumbnails)
                    return mImageListView.ThumbnailSize + new Size(8, 8);
                else
                    return base.MeasureItem(view);
            }
            /// <summary>
            /// Draws the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="state">The current view state of item.</param>
            /// <param name="bounds">The bounding rectangle of item in client coordinates.</param>
            public override void DrawItem(Graphics g, ImageListViewItem item, ItemState state, Rectangle bounds)
            {
                if (ImageListView.View == View.Thumbnails)
                {
                    // Zoom on mouse over
                    if ((state & ItemState.Hovered) != ItemState.None)
                    {
                        bounds.Inflate((int)(bounds.Width * mZoomRatio), (int)(bounds.Height * mZoomRatio));
                        if (bounds.Bottom > ItemAreaBounds.Bottom)
                            bounds.Y = ItemAreaBounds.Bottom - bounds.Height;
                        if (bounds.Top < ItemAreaBounds.Top)
                            bounds.Y = ItemAreaBounds.Top;
                        if (bounds.Right > ItemAreaBounds.Right)
                            bounds.X = ItemAreaBounds.Right - bounds.Width;
                        if (bounds.Left < ItemAreaBounds.Left)
                            bounds.X = ItemAreaBounds.Left;
                    }

                    // Get item image
                    Image img = null;
                    img = item.ThumbnailImage;
                    if ((state & ItemState.Hovered) == ItemState.Hovered)
                    {
                        lock (lockObject)
                        {
                            if (cachedFileName == item.FileName)
                                img = cachedImage;
                            else
                            {
                                requestedFileName = item.FileName;
                                Monitor.Pulse(lockObject);
                            }
                        }
                    }

                    // Calculate image bounds
                    float xscale = ((float)bounds.Width - 8.0f) / (float)img.Width;
                    float yscale = ((float)bounds.Height - 8.0f) / (float)img.Height;
                    float scale = Math.Min(xscale, yscale);
                    if (scale > 1.0f) scale = 1.0f;
                    int imageWidth = (int)((float)img.Width * scale);
                    int imageHeight = (int)((float)img.Height * scale);
                    int imageX = bounds.Left + (bounds.Width - imageWidth) / 2;
                    int imageY = bounds.Top + (bounds.Height - imageHeight) / 2;

                    // Allocate space for item text
                    if ((state & ItemState.Hovered) != ItemState.None &&
                        (bounds.Height - imageHeight) / 2 < mImageListView.Font.Height + 8)
                    {
                        int delta = (mImageListView.Font.Height + 8) - (bounds.Height - imageHeight) / 2;
                        bounds.Height += 2 * delta;
                        imageY += delta;
                    }

                    // Paint background
                    using (Brush bBack = new SolidBrush(mImageListView.BackColor))
                    {
                        Utility.FillRoundedRectangle(g, bBack, bounds, 5);
                    }
                    using (Brush bItemBack = new SolidBrush(item.BackColor))
                    {
                        Utility.FillRoundedRectangle(g, bItemBack, bounds, 5);
                    }
                    if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Brush bSelected = new LinearGradientBrush(bounds, Color.FromArgb(16, SystemColors.Highlight), Color.FromArgb(64, SystemColors.Highlight), LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bSelected, bounds, 5);
                        }
                    }
                    else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Brush bGray64 = new LinearGradientBrush(bounds, Color.FromArgb(16, SystemColors.GrayText), Color.FromArgb(64, SystemColors.GrayText), LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bGray64, bounds, 5);
                        }
                    }
                    if (((state & ItemState.Hovered) != ItemState.None))
                    {
                        using (Brush bHovered = new LinearGradientBrush(bounds, Color.FromArgb(8, SystemColors.Highlight), Color.FromArgb(32, SystemColors.Highlight), LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bHovered, bounds, 5);
                        }
                    }

                    // Draw the image
                    g.DrawImage(img, imageX, imageY, imageWidth, imageHeight);
                    // Draw image border
                    if (imageWidth > 32)
                    {
                        using (Pen pGray128 = new Pen(Color.FromArgb(128, Color.Gray)))
                        {
                            g.DrawRectangle(pGray128, imageX, imageY, imageWidth, imageHeight);
                        }
                        if (System.Math.Min(imageWidth, imageHeight) > 32)
                        {
                            using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                            {
                                g.DrawRectangle(pWhite128, imageX + 1, imageY + 1, imageWidth - 2, imageHeight - 2);
                            }
                        }
                    }

                    // Draw item text
                    if ((state & ItemState.Hovered) != ItemState.None)
                    {
                        RectangleF rt;
                        StringFormat sf = new StringFormat();
                        rt = new RectangleF(bounds.Left + 4, bounds.Top + 4, bounds.Width - 8, (bounds.Height - imageHeight) / 2 - 8);
                        sf.Alignment = StringAlignment.Center;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        using (Brush bItemFore = new SolidBrush(item.ForeColor))
                        {
                            g.DrawString(item.Text, mImageListView.Font, bItemFore, rt, sf);
                        }
                        rt.Y = bounds.Bottom - (bounds.Height - imageHeight) / 2 + 4;
                        using (Brush bGrayText = new SolidBrush(Color.Gray))
                        {
                            g.DrawString(string.Format("{0} pixels, {1}", item.GetSubItemText(ColumnType.Dimensions), item.GetSubItemText(ColumnType.FileSize)),
                                mImageListView.Font, bGrayText, rt, sf);
                        }
                    }

                    // Item border
                    using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                    {
                        Utility.DrawRoundedRectangle(g, pWhite128, bounds.Left + 1, bounds.Top + 1, bounds.Width - 3, bounds.Height - 3, 4);
                    }
                    if (mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pHighlight128 = new Pen(Color.FromArgb(128, SystemColors.Highlight)))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if (!mImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pGray128 = new Pen(Color.FromArgb(128, SystemColors.GrayText)))
                        {
                            Utility.DrawRoundedRectangle(g, pGray128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if ((state & ItemState.Selected) == ItemState.None)
                    {
                        using (Pen pGray64 = new Pen(Color.FromArgb(64, SystemColors.GrayText)))
                        {
                            Utility.DrawRoundedRectangle(g, pGray64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }

                    if (mImageListView.Focused && ((state & ItemState.Hovered) != ItemState.None))
                    {
                        using (Pen pHighlight64 = new Pen(Color.FromArgb(64, SystemColors.Highlight)))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                }
                else
                    base.DrawItem(g, item, state, bounds);
            }
        }
        #endregion

        #region PanelRenderer
        /// <summary>
        /// Shows detailed image information on a fixed panel.
        /// </summary>
        public class PanelRenderer : ImageListView.ImageListViewRenderer
        {
            private int mPanelWidth;
            private bool mPanelVisible;
            private string mDefaultText;
            private Font mCaptionFont;

            private Font CaptionFont
            {
                get
                {
                    if (mCaptionFont == null)
                        mCaptionFont = new Font(mImageListView.Font, FontStyle.Bold);
                    return mCaptionFont;
                }
            }

            public PanelRenderer()
                : this(180)
            {
                ;
            }

            public PanelRenderer(int panelWidth)
                : this(panelWidth, "Select an image to display its properties here.")
            {
                ;
            }

            public PanelRenderer(int panelWidth, string defaultText)
            {
                mPanelWidth = panelWidth;
                mDefaultText = defaultText;
            }

            /// <summary>
            /// Sets the layout of the control.
            /// </summary>
            /// <param name="e">A LayoutEventArgs that contains event data.</param>
            public override void OnLayout(LayoutEventArgs e)
            {
                mPanelVisible = false;
                // Allocate space for the panel
                int iwidth = this.MeasureItem(View.Thumbnails).Width + mImageListView.ItemMargin.Width;
                if (mImageListView.View == View.Thumbnails && e.ItemAreaBounds.Width > mPanelWidth + iwidth)
                {
                    Rectangle r = e.ItemAreaBounds;
                    r.X += mPanelWidth;
                    r.Width -= mPanelWidth;
                    e.ItemAreaBounds = r;
                    mPanelVisible = true;
                }
            }
            /// <summary>
            /// Initializes the System.Drawing.Graphics used to draw
            /// control elements.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            public override void InitializeGraphics(Graphics g)
            {
                base.InitializeGraphics(g);

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            }
            /// <summary>
            /// Releases managed resources.
            /// </summary>
            public override void OnDispose()
            {
                if (mCaptionFont != null)
                    mCaptionFont.Dispose();
            }
            /// <summary>
            /// Draws the background of the control.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The client coordinates of the item area.</param>
            public override void DrawBackground(Graphics g, Rectangle bounds)
            {
                base.DrawBackground(g, bounds);
                if (!mPanelVisible) return;

                // Draw the panel
                Rectangle rect = bounds;
                rect.Width = mPanelWidth - 6;
                using (Brush bBack = new LinearGradientBrush(rect, Color.White, Color.FromArgb(220, 220, 220), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bBack, rect);
                }
                using (Brush bBorder = new LinearGradientBrush(rect, Color.FromArgb(220, 220, 220), Color.White, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bBorder, rect.Right, rect.Top, 2, rect.Height);
                }

                rect.Inflate(-4, -4);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                if (mImageListView.Items.FocusedItem == null)
                {
                    // Default text
                    g.DrawString(mDefaultText, mImageListView.Font, Brushes.Black, rect, sf);
                }
                else
                {
                    ImageListViewItem item = mImageListView.Items.FocusedItem;
                    rect.Inflate(-4, -4);
                    // Draw selected image
                    using (Image img = item.GetImage())
                    {
                        // Calculate image bounds
                        float xscale = (float)rect.Width / (float)img.Width;
                        float yscale = (float)rect.Height / (float)img.Height;
                        float scale = Math.Min(xscale, yscale);
                        if (scale > 1.0f) scale = 1.0f;
                        int imageWidth = (int)((float)img.Width * scale);
                        int imageHeight = (int)((float)img.Height * scale);
                        int imageX = rect.Left + (rect.Width - imageWidth) / 2;
                        int imageY = rect.Top;
                        // Draw image
                        g.DrawImage(img, imageX, imageY, imageWidth, imageHeight);
                        // Draw image border
                        if (img.Width > 32)
                        {
                            using (Pen pGray128 = new Pen(Color.FromArgb(128, Color.Gray)))
                            {
                                g.DrawRectangle(pGray128, imageX, imageY, imageWidth, imageHeight);
                            }
                            if (System.Math.Min(mImageListView.ThumbnailSize.Width, mImageListView.ThumbnailSize.Height) > 32)
                            {
                                using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                                {
                                    g.DrawRectangle(pWhite128, imageX + 1, imageY + 1, imageWidth - 2, imageHeight - 2);
                                }
                            }
                        }
                        rect.Y = imageHeight + 16;
                        rect.Height -= imageHeight + 16;
                    }
                    // Image information
                    sf.Alignment = StringAlignment.Near;
                    sf.LineAlignment = StringAlignment.Near;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    g.DrawString(item.Text, CaptionFont, Brushes.Black, rect, sf);

                    int textHeight = (int)CaptionFont.GetHeight() * 2;
                    rect.Y += textHeight;
                    rect.Height -= textHeight;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(item.FileType);

                    sb.Append("Dimensions: ");
                    sb.Append(item.GetSubItemText(ColumnType.Dimensions));
                    sb.AppendLine();

                    sb.Append("Resolution: ");
                    sb.Append(item.Resolution.Width);
                    sb.AppendLine(" dpi");

                    sb.Append("Size: ");
                    sb.Append(item.GetSubItemText(ColumnType.FileSize));
                    sb.AppendLine();

                    sb.Append("Modified: ");
                    sb.Append(item.GetSubItemText(ColumnType.DateModified));
                    sb.AppendLine();

                    sb.Append("Created: ");
                    sb.Append(item.GetSubItemText(ColumnType.DateCreated));
                    sb.AppendLine();
                    sb.AppendLine();

                    g.DrawString(sb.ToString(), mImageListView.Font, Brushes.Black, rect, sf);
                }
            }
        }
        #endregion
    }
}