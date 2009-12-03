using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;

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

                if (view == View.Details)
                    return base.MeasureItem(view);
                else
                {
                    // Calculate item size
                    Size itemPadding = new Size(4, 4);
                    itemSize = mImageListView.ThumbnailSize + itemPadding + itemPadding;
                    itemSize.Height += textHeight + System.Math.Max(4, textHeight / 3) + itemPadding.Height; // textHeight / 3 = vertical space between thumbnail and text
                    return itemSize;
                }
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

                if (mImageListView.View == View.Thumbnails || mImageListView.View == View.Gallery)
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
            /// <summary>
            /// Draws the large preview image of the focused item in Gallery mode.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="image">The image to draw.</param>
            /// <param name="bounds">The bounding rectangle of the preview area.</param>
            public override void DrawGalleryImage(Graphics g, ImageListViewItem item, Image image, Rectangle bounds)
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
                if (image.Width > 32)
                {
                    using (Pen pBorder = new Pen(Color.Black))
                    {
                        g.DrawRectangle(pBorder, imageX, imageY, imageWidth, imageHeight);
                    }
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
            private float mZoomRatio;

            BackgroundWorker worker;
            private string cachedFileName;
            private Image cachedImage;

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

                cachedFileName = null;
                cachedImage = null;
                worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            }

            void worker_DoWork(object sender, DoWorkEventArgs e)
            {
                Utility.Pair<string, Size> request = (Utility.Pair<string, Size>)e.Argument;
                string requestedGalleryFile = request.First;
                Size size = request.Second;

                // Calculate image bounds
                Image img = Image.FromFile(requestedGalleryFile);
                float xscale = (float)size.Width / (float)img.Width;
                float yscale = (float)size.Height / (float)img.Height;
                float scale = Math.Min(xscale, yscale);
                if (scale > 1.0f) scale = 1.0f;
                int imageWidth = (int)((float)img.Width * scale);
                int imageHeight = (int)((float)img.Height * scale);
                Image scaled = new Bitmap(imageWidth, imageHeight);
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.High;
                    g.DrawImage(img, 0, 0, imageWidth, imageHeight);
                }
                img.Dispose();
                e.Result = new Utility.Pair<string, Image>(requestedGalleryFile, scaled);
            }

            void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                if (cachedImage != null)
                    cachedImage.Dispose();

                Utility.Pair<string, Image> result = (Utility.Pair<string, Image>)e.Result;
                cachedFileName = result.First;
                cachedImage = result.Second;
                mImageListView.BeginInvoke(new RefreshEventHandlerInternal(mImageListView.OnRefreshInternal));
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
                    Image img = item.ThumbnailImage;
                    if ((state & ItemState.Hovered) != ItemState.None)
                    {
                        if (cachedImage == null || cachedFileName == null || string.Compare(cachedFileName, item.FileName, StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            if (!worker.IsBusy)
                                worker.RunWorkerAsync(new Utility.Pair<string, Size>(item.FileName, new Size(bounds.Width - 8, bounds.Height - 8)));
                        }
                        else
                            img = cachedImage;
                    }

                    // Calculate image bounds
                    int imageWidth = img.Width;
                    int imageHeight = img.Height;
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
            private const int PropertyTagImageDescription = 0x010E;
            private const int PropertyTagEquipModel = 0x0110;
            private const int PropertyTagDateTime = 0x0132;
            private const int PropertyTagArtist = 0x013B;
            private const int PropertyTagCopyright = 0x8298;
            private const int PropertyTagExifExposureTime = 0x829A;
            private const int PropertyTagExifFNumber = 0x829D;
            private const int PropertyTagExifISOSpeed = 0x8827;
            private const int PropertyTagExifShutterSpeed = 0x9201;
            private const int PropertyTagExifAperture = 0x9202;
            private const int PropertyTagExifUserComment = 0x9286;

            private int mPanelWidth;
            private bool mPanelVisible;
            private Font mCaptionFont;
            private Dictionary<int, string> mTags;

            BackgroundWorker worker;
            private string cachedFileName;
            private Image cachedImage;

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
                : this(240)
            {
                ;
            }

            public PanelRenderer(int panelWidth)
            {
                mPanelWidth = panelWidth;
                mTags = new Dictionary<int, string>()
                {
                    {PropertyTagImageDescription, "Description"},
                    {PropertyTagEquipModel, "Camera Model"},
                    {PropertyTagDateTime, "Date Taken"},
                    {PropertyTagArtist, "Artist"},
                    {PropertyTagCopyright, "Copyright"},
                    {PropertyTagExifExposureTime, "Exposure Time"},
                    {PropertyTagExifFNumber, "F Number"},
                    {PropertyTagExifISOSpeed, "ISO Speed"},
                    {PropertyTagExifShutterSpeed, "Shutter Speed"},
                    {PropertyTagExifAperture, "Aperture"},
                    {PropertyTagExifUserComment, "Comments"},
                };

                cachedFileName = null;
                cachedImage = null;
                worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            }

            void worker_DoWork(object sender, DoWorkEventArgs e)
            {
                Utility.Pair<string, int> request = (Utility.Pair<string, int>)e.Argument;
                string requestedGalleryFile = (string)request.First;
                int width = request.Second;

                // Calculate image bounds
                Image img = Image.FromFile(requestedGalleryFile);
                float scale = (float)width / (float)img.Width;
                if (scale > 1.0f) scale = 1.0f;
                int imageWidth = (int)((float)img.Width * scale);
                int imageHeight = (int)((float)img.Height * scale);
                Image scaled = new Bitmap(imageWidth, imageHeight);
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.High;
                    g.DrawImage(img, 0, 0, imageWidth, imageHeight);
                }
                img.Dispose();
                e.Result = new Utility.Pair<string, Image>(requestedGalleryFile, scaled);
            }

            void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                if (cachedImage != null)
                    cachedImage.Dispose();

                Utility.Pair<string, Image> result = (Utility.Pair<string, Image>)e.Result;
                cachedFileName = result.First;
                cachedImage = result.Second;
                mImageListView.BeginInvoke(new RefreshEventHandlerInternal(mImageListView.OnRefreshInternal));
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
                if (mImageListView.View == View.Thumbnails && e.ItemAreaBounds.Width > mPanelWidth + iwidth &&
                    mImageListView.Items.Count > 0)
                {
                    Rectangle r = e.ItemAreaBounds;
                    r.X += mPanelWidth;
                    r.Width -= mPanelWidth;
                    e.ItemAreaBounds = r;
                    mPanelVisible = true;
                }
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
                    using (Pen pBorder = new Pen(bBorder))
                    {
                        g.DrawLine(pBorder, rect.Right, rect.Top, rect.Right, rect.Bottom);
                    }
                }
                using (Pen pWhite = new Pen(Color.FromArgb(64, Color.White)))
                {
                    g.DrawRectangle(pWhite, rect.Left + 2, rect.Top + 2, rect.Width - 5, rect.Height - 4);
                }
                rect.Inflate(-4, -4);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                ImageListViewItem item = null;
                if (mImageListView.Items.FocusedItem != null)
                    item = mImageListView.Items.FocusedItem;
                else if (mImageListView.SelectedItems.Count > 0)
                    item = mImageListView.SelectedItems[0];
                else if (mImageListView.Items.Count != 0)
                    item = mImageListView.Items[0];

                if (item != null)
                {
                    rect.Inflate(-4, -4);

                    Image img = item.ThumbnailImage;
                    if (cachedImage == null || cachedFileName == null || string.Compare(cachedFileName, item.FileName, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        if (!worker.IsBusy)
                            worker.RunWorkerAsync(new Utility.Pair<string, int>(item.FileName, rect.Width));
                    }
                    else
                        img = cachedImage;

                    // Draw image
                    g.DrawImageUnscaled(img, rect.Location);

                    // Draw image border
                    if (img.Width > 32)
                    {
                        using (Pen pGray128 = new Pen(Color.FromArgb(128, Color.Gray)))
                        {
                            g.DrawRectangle(pGray128, rect.Left, rect.Top, img.Width, img.Height);
                        }
                        using (Pen pWhite128 = new Pen(Color.FromArgb(128, Color.White)))
                        {
                            g.DrawRectangle(pWhite128, rect.Left + 1, rect.Top + 1, img.Width - 2, img.Height - 2);
                        }
                    }
                    rect.Y = img.Height + 16;
                    rect.Height -= img.Height + 16;

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
                    // File properties
                    sb.AppendLine(item.FileType);

                    sb.Append(mImageListView.Columns.GetDefaultText(ColumnType.Dimensions));
                    sb.Append(": ");
                    sb.Append(item.GetSubItemText(ColumnType.Dimensions));
                    sb.AppendLine();

                    sb.Append(mImageListView.Columns.GetDefaultText(ColumnType.Resolution));
                    sb.Append(": ");
                    sb.Append(item.Resolution.Width);
                    sb.AppendLine(" dpi");

                    sb.Append(mImageListView.Columns.GetDefaultText(ColumnType.FileSize));
                    sb.Append(": ");
                    sb.Append(item.GetSubItemText(ColumnType.FileSize));
                    sb.AppendLine();

                    sb.Append(mImageListView.Columns.GetDefaultText(ColumnType.DateModified));
                    sb.Append(": ");
                    sb.Append(item.GetSubItemText(ColumnType.DateModified));
                    sb.AppendLine();

                    sb.Append(mImageListView.Columns.GetDefaultText(ColumnType.DateCreated));
                    sb.Append(": ");
                    sb.Append(item.GetSubItemText(ColumnType.DateCreated));
                    sb.AppendLine();

                    // Exif info
                    sb.AppendLine();
                    using (FileStream stream = new FileStream(item.FileName, FileMode.Open, FileAccess.Read))
                    {
                        using (Image tempImage = Image.FromStream(stream, false, false))
                        {
                            foreach (PropertyItem prop in tempImage.PropertyItems)
                            {
                                if (mTags.ContainsKey(prop.Id))
                                {
                                    string tagName = mTags[prop.Id];
                                    short tagType = prop.Type;
                                    int tagLen = prop.Len;
                                    byte[] tagBytes = prop.Value;
                                    string tagValue = string.Empty;
                                    switch (tagType)
                                    {
                                        case 1: // byte
                                            foreach (byte b in tagBytes)
                                                tagValue += b.ToString() + " ";
                                            break;
                                        case 2: // ascii
                                            int len = Array.IndexOf(tagBytes, (byte)0);
                                            if (len == -1) len = tagLen;
                                            tagValue = Encoding.ASCII.GetString(tagBytes, 0, len);
                                            if (prop.Id == PropertyTagDateTime)
                                            {
                                                tagValue = DateTime.ParseExact(tagValue, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString("g");
                                            }
                                            break;
                                        case 3: // ushort
                                            for (int i = 0; i < tagLen; i += 2)
                                                tagValue += BitConverter.ToUInt16(tagBytes, i).ToString() + " ";
                                            break;
                                        case 4: // uint
                                            for (int i = 0; i < tagLen; i += 4)
                                                tagValue += BitConverter.ToUInt32(tagBytes, i).ToString() + " ";
                                            break;
                                        case 5: // uint rational
                                            for (int i = 0; i < tagLen; i += 8)
                                                tagValue += BitConverter.ToUInt32(tagBytes, i).ToString() + "/" +
                                                    BitConverter.ToUInt32(tagBytes, i + 4).ToString() + " ";
                                            break;
                                        case 7: // undefined as ascii
                                            int lenu = Array.IndexOf(tagBytes, (byte)0);
                                            if (lenu == -1) len = tagLen;
                                            tagValue = Encoding.ASCII.GetString(tagBytes, 0, lenu);
                                            if (prop.Id == PropertyTagDateTime)
                                            {
                                                tagValue = DateTime.ParseExact(tagValue, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString("g");
                                            }
                                            break;
                                        case 9: // int
                                            for (int i = 0; i < tagLen; i += 4)
                                                tagValue += BitConverter.ToInt32(tagBytes, i).ToString() + " ";
                                            break;
                                        case 10: // int rational
                                            for (int i = 0; i < tagLen; i += 8)
                                                tagValue += BitConverter.ToInt32(tagBytes, i).ToString() + "/" +
                                                    BitConverter.ToInt32(tagBytes, i + 4).ToString() + " ";
                                            break;
                                    }
                                    tagValue = tagValue.Trim();

                                    if (tagValue != string.Empty)
                                    {
                                        sb.Append(tagName);
                                        sb.Append(": ");
                                        sb.Append(tagValue);
                                        sb.AppendLine();
                                    }
                                }
                            }
                        }
                    }
                    // Print image details
                    g.DrawString(sb.ToString(), mImageListView.Font, Brushes.Black, rect, sf);
                }
            }
        }
        #endregion
    }
}