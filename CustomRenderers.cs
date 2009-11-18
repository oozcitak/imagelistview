using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Manina.Windows.Forms
{
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

            if (mImageListView.View == View.Thumbnails)
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
                rt.Inflate(0, 2);
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
                        if ((state & ItemState.Selected)== ItemState.None)
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
}
