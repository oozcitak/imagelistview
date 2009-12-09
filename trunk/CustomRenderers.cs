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
                : this(150)
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
                    itemSize.Height = Math.Max(mTextHeight, mImageListView.ThumbnailSize.Height) + 2 * itemPadding.Height;
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
                        using (StringFormat sf = new StringFormat())
                        {
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
                                if (!string.IsNullOrEmpty(item.FileType))
                                {
                                    g.DrawString(item.GetSubItemText(ColumnType.FileType),
                                        mImageListView.Font, bItemDetails, rt, sf);
                                    rt.Offset(0, 1.1f * lineHeight);
                                }
                                if (item.Dimensions != Size.Empty || item.Resolution != SizeF.Empty)
                                {
                                    string text = "";
                                    if (item.Dimensions != Size.Empty)
                                        text += item.GetSubItemText(ColumnType.Dimensions) + " pixels ";
                                    if (item.Resolution != SizeF.Empty)
                                        text += item.Resolution.Width + " dpi";
                                    g.DrawString(text, mImageListView.Font, bItemDetails, rt, sf);
                                    rt.Offset(0, 1.1f * lineHeight);
                                }
                                if (item.FileSize != 0)
                                {
                                    g.DrawString(item.GetSubItemText(ColumnType.FileSize),
                                        mImageListView.Font, bItemDetails, rt, sf);
                                    rt.Offset(0, 1.1f * lineHeight);
                                }
                                if (item.DateModified != DateTime.MinValue)
                                {
                                    g.DrawString(item.GetSubItemText(ColumnType.DateModified),
                                        mImageListView.Font, bItemDetails, rt, sf);
                                }
                            }
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
                    using (StringFormat sf = new StringFormat())
                    {
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
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Alignment = StringAlignment.Near;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        // Sub text
                        List<ImageListView.ImageListViewColumnHeader> uicolumns = mImageListView.Columns.GetUIColumns();
                        RectangleF rt = new RectangleF(bounds.Left + offset.Width, bounds.Top + offset.Height, uicolumns[0].Width - 2 * offset.Width, bounds.Height - 2 * offset.Height);
                        foreach (ImageListView.ImageListViewColumnHeader column in uicolumns)
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
                    Image img = null;
                    if ((state & ItemState.Hovered) != ItemState.None)
                        img = GetImageAsync(item, new Size(bounds.Width - 8, bounds.Height - 8));
                    else
                        img = item.ThumbnailImage;

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
                        using (StringFormat sf = new StringFormat())
                        {
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
                            string details = "";
                            if (item.Dimensions != Size.Empty)
                                details += item.GetSubItemText(ColumnType.Dimensions) + " pixels ";
                            if (item.FileSize != 0)
                                details += item.GetSubItemText(ColumnType.FileSize);
                            using (Brush bGrayText = new SolidBrush(Color.Gray))
                            {
                                g.DrawString(details, mImageListView.Font, bGrayText, rt, sf);
                            }
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
                : this(240)
            {
                ;
            }

            public PanelRenderer(int panelWidth)
            {
                mPanelWidth = panelWidth;
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
            /// Draws the large preview image of the focused item in Gallery mode.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="image">The image to draw.</param>
            /// <param name="bounds">The bounding rectangle of the preview area.</param>
            public override void DrawGalleryImage(Graphics g, ImageListViewItem item, Image image, Rectangle bounds)
            {
                // Allocate some space for the panel
                Rectangle panel = new Rectangle(bounds.Left, bounds.Bottom - 3 * mImageListView.Font.Height, bounds.Width, 3 * mImageListView.Font.Height);
                bounds.Height -= panel.Height;
                base.DrawGalleryImage(g, item, image, bounds);

                // Draw panel background
                using (Brush bBack = new LinearGradientBrush(panel, Color.FromArgb(32, SystemColors.Control), Color.FromArgb(196, SystemColors.Control), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bBack, panel);
                }
                using (Pen pBorder = new Pen(Color.FromArgb(64, SystemColors.GrayText)))
                {
                    g.DrawLine(pBorder, panel.Left, panel.Top, panel.Right, panel.Top);
                    g.DrawLine(pBorder, panel.Left, panel.Bottom - 1, panel.Right, panel.Bottom - 1);
                }

                // Image information
                string space = " ";
                string colon = ":";
                List<string> texts = new List<string>();
                texts.Add(item.Text);
                if (item.Dimensions != Size.Empty)
                {
                    texts.Add(mImageListView.Columns.GetDefaultText(ColumnType.Dimensions) + colon);
                    texts.Add(item.GetSubItemText(ColumnType.Dimensions) + space);
                }
                if (item.FileSize != 0)
                {
                    texts.Add(mImageListView.Columns.GetDefaultText(ColumnType.FileSize) + colon);
                    texts.Add(item.GetSubItemText(ColumnType.FileSize) + space);
                }
                if (item.DateModified != DateTime.MinValue)
                {
                    texts.Add(mImageListView.Columns.GetDefaultText(ColumnType.DateModified) + colon);
                    texts.Add(item.GetSubItemText(ColumnType.DateModified) + space);
                }

                List<int> widths = new List<int>();
                int totalwidth = 0;
                int height = 0;
                for (int i = 0; i < texts.Count; i++)
                {
                    int width;
                    if (i == 0)
                    {
                        SizeF sz = g.MeasureString(texts[i], CaptionFont);
                        width = (int)sz.Width;
                        height = (int)sz.Height;
                        widths.Add(width);
                    }
                    else
                    {
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
                            width = (int)g.MeasureString(texts[i], mImageListView.Font, panel.Width, sf).Width;
                            widths.Add(width);
                            totalwidth += width;
                        }
                    }
                }
                if (totalwidth > panel.Width - 4) totalwidth = panel.Width - 4;

                // Draw item text
                int x = panel.X + (panel.Width - widths[0]) / 2;
                int y = panel.Y + (panel.Height / 2 - height) / 2;
                g.DrawString(texts[0], CaptionFont, SystemBrushes.WindowText, x, y);

                // Draw item details
                x = panel.X + (panel.Width - totalwidth) / 2;
                y = panel.Y + panel.Height / 2 + (panel.Height / 2 - height) / 2;

                for (int i = 1; i < texts.Count; i++)
                {
                    if (i % 2 == 1)
                        g.DrawString(texts[i], mImageListView.Font, SystemBrushes.GrayText, x, y);
                    else
                        g.DrawString(texts[i], mImageListView.Font, SystemBrushes.WindowText, x, y);
                    x += widths[i];
                }
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
                using (Brush bBack = new LinearGradientBrush(rect, Color.FromArgb(196, SystemColors.Control), Color.FromArgb(32, SystemColors.Control), LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(bBack, rect);
                }
                using (Pen pBorder = new Pen(Color.FromArgb(64, SystemColors.GrayText)))
                {
                    g.DrawLine(pBorder, rect.Right, rect.Top, rect.Right, rect.Bottom);
                }
                rect.Inflate(-4, -4);

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

                    Image img = GetImageAsync(item, new Size(rect.Width, rect.Width));

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

                    // File properties
                    if (rect.Height > 0 && !string.IsNullOrEmpty(item.FileType))
                    {
                        int y = DrawStringPair(g, rect, "", item.FileType);
                        rect.Y += y;
                        rect.Height -= y;
                    }

                    foreach (ImageListView.ImageListViewColumnHeader column in mImageListView.Columns)
                    {
                        if (column.Type == ColumnType.ImageDescription)
                        {
                            rect.Y += 8;
                            rect.Height -= 8;
                        }

                        if (rect.Height <= 0) break;

                        if (column.Type != ColumnType.FileType &&
                            column.Type != ColumnType.DateAccessed &&
                            column.Type != ColumnType.FileName &&
                            column.Type != ColumnType.FilePath &&
                            column.Type != ColumnType.Name)
                        {
                            string caption = column.Text;
                            string text = item.GetSubItemText(column.Type);
                            if (!string.IsNullOrEmpty(text))
                            {
                                int y = DrawStringPair(g, rect, caption + ": ", text);
                                rect.Y += y;
                                rect.Height -= y;
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// Draws the given caption/text inside the given rectangle.
            /// </summary>
            private int DrawStringPair(Graphics g, Rectangle r, string caption, string text)
            {
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Near;
                    sf.LineAlignment = StringAlignment.Near;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    sf.FormatFlags = StringFormatFlags.NoWrap;

                    SizeF szc = g.MeasureString(caption, mImageListView.Font, r.Size, sf);
                    int y = (int)szc.Height;
                    if (szc.Width > r.Width) szc.Width = r.Width;
                    Rectangle txrect = new Rectangle(r.Location, Size.Ceiling(szc));
                    g.DrawString(caption, mImageListView.Font, SystemBrushes.GrayText, txrect, sf);
                    txrect.X += txrect.Width;
                    txrect.Width = r.Width;
                    if (txrect.X < r.Right)
                    {
                        SizeF szt = g.MeasureString(text, mImageListView.Font, r.Size, sf);
                        y = Math.Max(y, (int)szt.Height);
                        txrect = Rectangle.Intersect(r, txrect);
                        g.DrawString(text, mImageListView.Font, SystemBrushes.WindowText, txrect, sf);
                    }

                    return y;
                }
            }
        }
        #endregion
    }
}