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
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Windows.Forms.VisualStyles;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the built-in renderers.
    /// </summary>
    public static partial class ImageListViewRenderers
    {
        #region DefaultRenderer
        /// <summary>
        /// The default renderer.
        /// </summary>
        public class DefaultRenderer : ImageListView.ImageListViewRenderer
        {
            /// <summary>
            /// Initializes a new instance of the DefaultRenderer class.
            /// </summary>
            public DefaultRenderer()
            {
                ;
            }
        }
        #endregion

        #region NoirRenderer
        /// <summary>
        /// A renderer with a dark theme.
        /// This renderer cannot be themed.
        /// </summary>
        public class NoirRenderer : ImageListView.ImageListViewRenderer
        {
            private int padding;
            private int mReflectionSize;

            /// <summary>
            /// Gets or sets the size of image reflections.
            /// </summary>
            public int ReflectionSize { get { return mReflectionSize; } set { mReflectionSize = value; } }

            /// <summary>
            /// Initializes a new instance of the NoirRenderer class.
            /// </summary>
            public NoirRenderer()
                : this(20)
            {
                ;
            }
            /// <summary>
            /// Initializes a new instance of the NoirRenderer class.
            /// </summary>
            /// <param name="reflectionSize">Size of image reflections.</param>
            public NoirRenderer(int reflectionSize)
            {
                mReflectionSize = reflectionSize;
                padding = 4;
            }

            /// <summary>
            /// Gets a value indicating whether this renderer can apply custom colors.
            /// </summary>
            /// <value></value>
            public override bool CanApplyColors { get { return false; } }

            /// <summary>
            /// Draws the background of the control.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The client coordinates of the item area.</param>
            public override void DrawBackground(Graphics g, Rectangle bounds)
            {
                g.Clear(Color.Black);
            }
            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the measurement should be made.</param>
            /// <returns></returns>
            public override Size MeasureItem(View view)
            {
                if (view == View.Details)
                    return base.MeasureItem(view);
                else
                    return new Size(ImageListView.ThumbnailSize.Width + 2 * padding,
                        ImageListView.ThumbnailSize.Height + 2 * padding + mReflectionSize);
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
                // Item background color
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    g.FillRectangle(brush, bounds);
                }

                if (ImageListView.View == View.Details)
                {
                    // Item background
                    if ((state & ItemState.Selected) == ItemState.Selected)
                    {
                        using (Brush brush = new LinearGradientBrush(bounds,
                            Color.FromArgb(64, 96, 160), Color.FromArgb(64, 64, 96, 160), LinearGradientMode.Horizontal))
                        {
                            g.FillRectangle(brush, bounds);
                        }
                    }
                    else if ((state & ItemState.Hovered) == ItemState.Hovered)
                    {
                        using (Brush brush = new LinearGradientBrush(bounds,
                            Color.FromArgb(64, Color.White), Color.FromArgb(16, Color.White), LinearGradientMode.Horizontal))
                        {
                            g.FillRectangle(brush, bounds);
                        }
                    }

                    // Shade sort column
                    List<ImageListView.ImageListViewColumnHeader> uicolumns = ImageListView.Columns.GetDisplayedColumns();
                    int x = bounds.Left - 1;
                    foreach (ImageListView.ImageListViewColumnHeader column in uicolumns)
                    {
                        if (ImageListView.SortColumn >= 0 && ImageListView.SortColumn < ImageListView.Columns.Count &&
                            ImageListView.Columns[ImageListView.SortColumn].columnID == column.columnID &&
                            ImageListView.SortOrder != SortOrder.None &&
                            (state & ItemState.Hovered) == ItemState.None && (state & ItemState.Selected) == ItemState.None)
                        {
                            Rectangle subItemBounds = bounds;
                            subItemBounds.X = x;
                            subItemBounds.Width = column.Width;
                            using (Brush brush = new SolidBrush(Color.FromArgb(32, 128, 128, 128)))
                            {
                                g.FillRectangle(brush, subItemBounds);
                            }
                            break;
                        }
                        x += column.Width;
                    }
                    // Separators 
                    x = ImageListView.layoutManager.ColumnHeaderBounds.Left;
                    foreach (ImageListView.ImageListViewColumnHeader column in uicolumns)
                    {
                        x += column.Width;
                        if (!ReferenceEquals(column, uicolumns[uicolumns.Count - 1]))
                        {
                            using (Pen pen = new Pen(Color.FromArgb(64, 128, 128, 128)))
                            {
                                g.DrawLine(pen, x, bounds.Top, x, bounds.Bottom);
                            }
                        }
                    }

                    // Item texts
                    Size offset = new Size(2, (bounds.Height - ImageListView.Font.Height) / 2);
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Alignment = StringAlignment.Near;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        // Sub text
                        RectangleF rt = new RectangleF(bounds.Left + offset.Width, bounds.Top + offset.Height, uicolumns[0].Width - 2 * offset.Width, bounds.Height - 2 * offset.Height);
                        foreach (ImageListView.ImageListViewColumnHeader column in uicolumns)
                        {
                            rt.Width = column.Width - 2 * offset.Width;
                            using (Brush bItemFore = new SolidBrush(Color.White))
                            {
                                if (column.Type == ColumnType.Rating && ImageListView.RatingImage != null && ImageListView.EmptyRatingImage != null)
                                {
                                    string srating = item.GetSubItemText(ColumnType.Rating);
                                    if (!string.IsNullOrEmpty(srating))
                                    {
                                        int w = ImageListView.RatingImage.Width;
                                        int y = (int)(rt.Top + (rt.Height - ImageListView.RatingImage.Height) / 2.0f);
                                        int rating = item.StarRating;
                                        if (rating < 0) rating = 0;
                                        if (rating > 5) rating = 5;
                                        for (int i = 1; i <= rating; i++)
                                            g.DrawImage(ImageListView.RatingImage, rt.Left + (i - 1) * w, y);
                                        for (int i = rating + 1; i <= 5; i++)
                                            g.DrawImage(ImageListView.EmptyRatingImage, rt.Left + (i - 1) * w, y);
                                    }
                                }
                                else if (column.Type == ColumnType.Custom)
                                    g.DrawString(item.GetSubItemText(column.columnID), ImageListView.Font, bItemFore, rt, sf);
                                else
                                    g.DrawString(item.GetSubItemText(column.Type), ImageListView.Font, bItemFore, rt, sf);
                            }
                            rt.X += column.Width;
                        }
                    }

                    // Border
                    if ((state & ItemState.Hovered) == ItemState.Hovered)
                    {
                        using (Pen pen = new Pen(Color.FromArgb(128, Color.White)))
                        {
                            g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                        }
                    }
                    else if ((state & ItemState.Selected) == ItemState.Hovered)
                    {
                        using (Pen pen = new Pen(Color.FromArgb(96, 144, 240)))
                        {
                            g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                        }
                    }
                }
                else // if (ImageListView.View != View.Details)
                {
                    // Align images to bottom of bounds
                    Image img = item.GetCachedImage(CachedImageType.Thumbnail);
                    if (img != null)
                    {
                        Rectangle pos = Utility.GetSizedImageBounds(img,
                            new Rectangle(bounds.X + padding, bounds.Y + padding, bounds.Width - 2 * padding, bounds.Height - 2 * padding - mReflectionSize),
                            50.0f, 100.0f);

                        int x = pos.X;
                        int y = pos.Y;

                        // Item background
                        if ((state & ItemState.Selected) == ItemState.Selected)
                        {
                            using (Brush brush = new LinearGradientBrush(
                                new Point(x - padding, y - padding), new Point(x - padding, y + pos.Height + 2 * padding),
                                Color.FromArgb(64, 96, 160), Color.FromArgb(16, 16, 16)))
                            {
                                g.FillRectangle(brush, x - padding, y - padding, pos.Width + 2 * padding, pos.Height + 2 * padding);
                            }
                        }
                        else if ((state & ItemState.Hovered) == ItemState.Hovered)
                        {
                            using (Brush brush = new LinearGradientBrush(
                                new Point(x - padding, y - padding), new Point(x - padding, y + pos.Height + 2 * padding),
                                Color.FromArgb(64, Color.White), Color.FromArgb(16, 16, 16)))
                            {
                                g.FillRectangle(brush, x - padding, y - padding, pos.Width + 2 * padding, pos.Height + 2 * padding);
                            }
                        }

                        // Border
                        if ((state & ItemState.Hovered) == ItemState.Hovered)
                        {
                            using (Brush brush = new LinearGradientBrush(
                                new Point(x - padding, y - padding), new Point(x - padding, y + pos.Height + 2 * padding),
                                Color.FromArgb(128, Color.White), Color.FromArgb(16, 16, 16)))
                            using (Pen pen = new Pen(brush))
                            {
                                g.DrawRectangle(pen, x - padding, y - padding + 1, pos.Width + 2 * padding - 1, pos.Height + 2 * padding - 1);
                            }
                        }
                        else if ((state & ItemState.Selected) == ItemState.Selected)
                        {
                            using (Brush brush = new LinearGradientBrush(
                                new Point(x - padding, y - padding), new Point(x - padding, y + pos.Height + 2 * padding),
                                Color.FromArgb(96, 144, 240), Color.FromArgb(16, 16, 16)))
                            using (Pen pen = new Pen(brush))
                            {
                                g.DrawRectangle(pen, x - padding, y - padding + 1, pos.Width + 2 * padding - 1, pos.Height + 2 * padding - 1);
                            }
                        }

                        // Draw item image
                        DrawImageWithReflection(g, img, pos, mReflectionSize);

                        // Highlight
                        using (Pen pen = new Pen(Color.FromArgb(160, Color.White)))
                        {
                            g.DrawLine(pen, pos.X, pos.Y + 1, pos.X + pos.Width - 1, pos.Y + 1);
                            g.DrawLine(pen, pos.X, pos.Y + 1, pos.X, pos.Y + pos.Height);
                        }
                    }
                }
            }
            /// <summary>
            /// Draws the checkbox icon for the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="bounds">The bounding rectangle of the checkbox in client coordinates.</param>
            public override void DrawCheckBox(Graphics g, ImageListViewItem item, Rectangle bounds)
            {
                ;
            }
            /// <summary>
            /// Draws the file icon for the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="bounds">The bounding rectangle of the file icon in client coordinates.</param>
            public override void DrawFileIcon(Graphics g, ImageListViewItem item, Rectangle bounds)
            {
                ;
            }
            /// <summary>
            /// Draws the column headers.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="column">The ImageListViewColumnHeader to draw.</param>
            /// <param name="state">The current view state of column.</param>
            /// <param name="bounds">The bounding rectangle of column in client coordinates.</param>
            public override void DrawColumnHeader(Graphics g, ImageListView.ImageListViewColumnHeader column, ColumnState state, Rectangle bounds)
            {
                // Paint background
                if (ImageListView.Focused && ((state & ColumnState.Hovered) == ColumnState.Hovered))
                {
                    using (Brush bHovered = new LinearGradientBrush(bounds,
                        Color.FromArgb(64, 96, 144, 240), Color.FromArgb(196, 96, 144, 240), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bHovered, bounds);
                    }
                }
                else
                {
                    using (Brush bNormal = new LinearGradientBrush(bounds,
                        Color.FromArgb(32, 128, 128, 128), Color.FromArgb(196, 128, 128, 128), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bNormal, bounds);
                    }
                }
                using (Brush bBorder = new LinearGradientBrush(bounds,
                    Color.FromArgb(96, 128, 128, 128), Color.FromArgb(128, 128, 128), LinearGradientMode.Vertical))
                using (Pen pBorder = new Pen(bBorder))
                {
                    g.DrawLine(pBorder, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                    g.DrawLine(pBorder, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                }
                using (Pen pen = new Pen(Color.FromArgb(16, Color.White)))
                {
                    g.DrawLine(pen, bounds.Left + 1, bounds.Top + 1, bounds.Left + 1, bounds.Bottom - 2);
                    g.DrawLine(pen, bounds.Right - 1, bounds.Top + 1, bounds.Right - 1, bounds.Bottom - 2);
                }

                // Draw the sort arrow
                int textOffset = 4;
                if (ImageListView.SortOrder != SortOrder.None &&
                    ImageListView.SortColumn >= 0 && ImageListView.SortColumn < ImageListView.Columns.Count &&
                    ImageListView.Columns[ImageListView.SortColumn].columnID == column.columnID)
                {
                    Image img = null;
                    if (ImageListView.SortOrder == SortOrder.Ascending)
                        img = ImageListViewResources.SortAscending;
                    else if (ImageListView.SortOrder == SortOrder.Descending)
                        img = ImageListViewResources.SortDescending;
                    g.DrawImageUnscaled(img, bounds.X + 4, bounds.Top + (bounds.Height - img.Height) / 2);
                    textOffset += img.Width;
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
                        using (Brush brush = new SolidBrush(Color.White))
                        {
                            g.DrawString(column.Text,
                                (ImageListView.HeaderFont == null ? ImageListView.Font : ImageListView.HeaderFont),
                                brush, bounds, sf);
                        }
                    }
                }
            }
            /// <summary>
            /// Draws the extender after the last column.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of extender column in client coordinates.</param>
            public override void DrawColumnExtender(Graphics g, Rectangle bounds)
            {
                using (Brush bNormal = new LinearGradientBrush(bounds,
                    Color.FromArgb(32, 128, 128, 128), Color.FromArgb(196, 128, 128, 128), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bNormal, bounds);
                }
                using (Brush bBorder = new LinearGradientBrush(bounds,
                    Color.FromArgb(96, 128, 128, 128), Color.FromArgb(128, 128, 128), LinearGradientMode.Vertical))
                using (Pen pBorder = new Pen(bBorder))
                {
                    g.DrawLine(pBorder, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                    g.DrawLine(pBorder, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                }
                using (Pen pen = new Pen(Color.FromArgb(16, Color.White)))
                {
                    g.DrawLine(pen, bounds.Left + 1, bounds.Top + 1, bounds.Left + 1, bounds.Bottom - 2);
                    g.DrawLine(pen, bounds.Right - 1, bounds.Top + 1, bounds.Right - 1, bounds.Bottom - 2);
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
                if (item != null && image != null)
                {
                    Size itemMargin = MeasureItemMargin(ImageListView.View);
                    Rectangle pos = Utility.GetSizedImageBounds(image, new Rectangle(bounds.X + itemMargin.Width, bounds.Y + itemMargin.Height, bounds.Width - 2 * itemMargin.Width, bounds.Height - 2 * itemMargin.Height - mReflectionSize), 50.0f, 100.0f);
                    DrawImageWithReflection(g, image, pos, mReflectionSize);
                }
            }
            /// <summary>
            /// Draws the left pane in Pane view mode.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="image">The image to draw.</param>
            /// <param name="bounds">The bounding rectangle of the pane.</param>
            public override void DrawPane(Graphics g, ImageListViewItem item, Image image, Rectangle bounds)
            {
                // Draw resize handle
                using (Brush bBorder = new SolidBrush(Color.FromArgb(64, 64, 64)))
                {
                    g.FillRectangle(bBorder, bounds.Right - 2, bounds.Top, 2, bounds.Height);
                }
                bounds.Width -= 2;

                if (item != null && image != null)
                {
                    // Calculate image bounds
                    Size itemMargin = MeasureItemMargin(ImageListView.View);
                    Rectangle pos = Utility.GetSizedImageBounds(image, new Rectangle(bounds.Location + itemMargin, bounds.Size - itemMargin - itemMargin), 50.0f, 0.0f);
                    // Draw image
                    g.DrawImage(image, pos);

                    bounds.X += itemMargin.Width;
                    bounds.Width -= 2 * itemMargin.Width;
                    bounds.Y = pos.Height + 16;
                    bounds.Height -= pos.Height + 16;

                    // Item text
                    if (ImageListView.Columns.HasType(ColumnType.Name) && ImageListView.Columns[ColumnType.Name].Visible && bounds.Height > 0)
                    {
                        string text = item.GetSubItemText(ColumnType.Name);
                        int y = Utility.DrawStringPair(g, bounds, "", text, ImageListView.Font,
                            Brushes.White, Brushes.White);
                        bounds.Y += 2 * y;
                        bounds.Height -= 2 * y;
                    }

                    // File type
                    string fileType = item.GetSubItemText(ColumnType.FileType);
                    if (ImageListView.Columns.HasType(ColumnType.FileType) && ImageListView.Columns[ColumnType.FileType].Visible && bounds.Height > 0 && !string.IsNullOrEmpty(fileType))
                    {
                        using (Brush bCaption = new SolidBrush(Color.FromArgb(196, 196, 196)))
                        using (Brush bText = new SolidBrush(Color.White))
                        {
                            int y = Utility.DrawStringPair(g, bounds, ImageListView.Columns[ColumnType.FileType].Text + ": ",
                                fileType, ImageListView.Font, bCaption, bText);
                            bounds.Y += y;
                            bounds.Height -= y;
                        }
                    }

                    // Metatada
                    foreach (ImageListView.ImageListViewColumnHeader column in ImageListView.Columns)
                    {
                        if (column.Type == ColumnType.ImageDescription)
                        {
                            bounds.Y += 8;
                            bounds.Height -= 8;
                        }

                        if (bounds.Height <= 0) break;

                        if (column.Visible &&
                            column.Type != ColumnType.Custom &&
                            column.Type != ColumnType.FileType &&
                            column.Type != ColumnType.DateAccessed &&
                            column.Type != ColumnType.FileName &&
                            column.Type != ColumnType.FilePath &&
                            column.Type != ColumnType.Name)
                        {
                            string caption = column.Text;
                            string text = item.GetSubItemText(column.Type);
                            if (!string.IsNullOrEmpty(text))
                            {
                                using (Brush bCaption = new SolidBrush(Color.FromArgb(196, 196, 196)))
                                using (Brush bText = new SolidBrush(Color.White))
                                {
                                    int y = Utility.DrawStringPair(g, bounds, caption + ": ", text,
                                        ImageListView.Font, bCaption, bText);
                                    bounds.Y += y;
                                    bounds.Height -= y;
                                }
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// Draws the selection rectangle.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="selection">The client coordinates of the selection rectangle.</param>
            public override void DrawSelectionRectangle(Graphics g, Rectangle selection)
            {
                using (Brush brush = new LinearGradientBrush(selection,
                    Color.FromArgb(160, 96, 144, 240), Color.FromArgb(32, 96, 144, 240),
                    LinearGradientMode.ForwardDiagonal))
                {
                    g.FillRectangle(brush, selection);
                }
                using (Brush brush = new LinearGradientBrush(selection,
                    Color.FromArgb(96, 144, 240), Color.FromArgb(128, 96, 144, 240),
                    LinearGradientMode.ForwardDiagonal))
                using (Pen pen = new Pen(brush))
                {
                    g.DrawRectangle(pen, selection);
                }
            }
            /// <summary>
            /// Draws the insertion caret for drag and drop operations.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of the insertion caret.</param>
            public override void DrawInsertionCaret(Graphics g, Rectangle bounds)
            {
                using (Brush b = new SolidBrush(Color.FromArgb(96, 144, 240)))
                {
                    g.FillRectangle(b, bounds);
                }
            }

            /// <summary>
            /// Draws an image with a reflection effect at the bottom.
            /// </summary>
            /// <param name="g">The graphics to draw on.</param>
            /// <param name="img">The image to draw.</param>
            /// <param name="x">The x coordinate of the upper left corner of the image.</param>
            /// <param name="y">The y coordinate of the upper left corner of the image.</param>
            /// <param name="width">Width of the drawn image.</param>
            /// <param name="height">Height of the drawn image.</param>
            /// <param name="reflection">Height of the reflection.</param>
            private void DrawImageWithReflection(Graphics g, Image img, int x, int y, int width, int height, int reflection)
            {
                // Draw the image
                g.DrawImage(img, x, y + 1, width, height);

                // Draw the reflection
                if (img.Width > 32 && img.Height > 32)
                {
                    int reflectionHeight = height / 2;
                    if (reflectionHeight > reflection) reflectionHeight = reflection;

                    Region prevClip = g.Clip;
                    g.SetClip(new Rectangle(x, y + height + 1, width, reflectionHeight));
                    g.DrawImage(img, x, y + height + height / 2 + 1, width, -height / 2);
                    g.Clip = prevClip;

                    using (Brush brush = new LinearGradientBrush(
                        new Point(x, y + height + 1), new Point(x, y + height + reflectionHeight + 1),
                        Color.FromArgb(128, 0, 0, 0), Color.Black))
                    {
                        g.FillRectangle(brush, x, y + height + 1, width, reflectionHeight);
                    }
                }
            }
            /// <summary>
            /// Draws an image with a reflection effect at the bottom.
            /// </summary>
            /// <param name="g">The graphics to draw on.</param>
            /// <param name="img">The image to draw.</param>
            /// <param name="bounds">The target bounding rectangle.</param>
            /// <param name="reflection">Height of the reflection.</param>
            private void DrawImageWithReflection(Graphics g, Image img, Rectangle bounds, int reflection)
            {
                DrawImageWithReflection(g, img, bounds.X, bounds.Y, bounds.Width, bounds.Height, reflection);
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

            /// <summary>
            /// Gets or sets the width of the tile.
            /// </summary>
            public int TileWidth { get { return mTileWidth; } set { mTileWidth = value; } }

            private Font CaptionFont
            {
                get
                {
                    if (mCaptionFont == null)
                        mCaptionFont = new Font(ImageListView.Font, FontStyle.Bold);
                    return mCaptionFont;
                }
            }

            /// <summary>
            /// Initializes a new instance of the TilesRenderer class.
            /// </summary>
            public TilesRenderer()
                : this(150)
            {
                ;
            }

            /// <summary>
            /// Initializes a new instance of the TilesRenderer class.
            /// </summary>
            /// <param name="tileWidth">Width of tiles in pixels.</param>
            public TilesRenderer(int tileWidth)
            {
                mTileWidth = tileWidth;
            }

            /// <summary>
            /// Releases managed resources.
            /// </summary>
            public override void Dispose()
            {
                if (mCaptionFont != null)
                    mCaptionFont.Dispose();

                base.Dispose();
            }
            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the item measurement should be made.</param>
            public override Size MeasureItem(Manina.Windows.Forms.View view)
            {
                if (view == Manina.Windows.Forms.View.Thumbnails)
                {
                    Size itemSize = new Size();
                    mTextHeight = (int)(5.8f * (float)CaptionFont.Height);

                    // Calculate item size
                    Size itemPadding = new Size(4, 4);
                    itemSize.Width = ImageListView.ThumbnailSize.Width + 4 * itemPadding.Width + mTileWidth;
                    itemSize.Height = Math.Max(mTextHeight, ImageListView.ThumbnailSize.Height) + 2 * itemPadding.Height;
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
                if (ImageListView.View == Manina.Windows.Forms.View.Thumbnails)
                {
                    Size itemPadding = new Size(4, 4);

                    // Paint background
                    using (Brush bItemBack = new SolidBrush(ImageListView.Colors.BackColor))
                    {
                        g.FillRectangle(bItemBack, bounds);
                    }
                    if ((ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None)) ||
                        (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None) && ((state & ItemState.Hovered) != ItemState.None)))
                    {
                        using (Brush bSelected = new LinearGradientBrush(bounds, ImageListView.Colors.SelectedColor1, ImageListView.Colors.SelectedColor2, LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bSelected, bounds, 4);
                        }
                    }
                    else if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Brush bGray64 = new LinearGradientBrush(bounds, ImageListView.Colors.UnFocusedColor1, ImageListView.Colors.UnFocusedColor2, LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bGray64, bounds, 4);
                        }
                    }
                    if (((state & ItemState.Hovered) != ItemState.None))
                    {
                        using (Brush bHovered = new LinearGradientBrush(bounds, ImageListView.Colors.HoverColor1, ImageListView.Colors.HoverColor2, LinearGradientMode.Vertical))
                        {
                            Utility.FillRoundedRectangle(g, bHovered, bounds, 4);
                        }
                    }

                    // Draw the image
                    Image img = item.GetCachedImage(CachedImageType.Thumbnail);
                    if (img != null)
                    {
                        Rectangle pos = Utility.GetSizedImageBounds(img, new Rectangle(bounds.Location + itemPadding, ImageListView.ThumbnailSize), 0.0f, 50.0f);
                        g.DrawImage(img, pos);
                        // Draw image border
                        if (Math.Min(pos.Width, pos.Height) > 32)
                        {
                            using (Pen pOuterBorder = new Pen(ImageListView.Colors.ImageOuterBorderColor))
                            {
                                g.DrawRectangle(pOuterBorder, pos);
                            }
                            if (System.Math.Min(ImageListView.ThumbnailSize.Width, ImageListView.ThumbnailSize.Height) > 32)
                            {
                                using (Pen pInnerBorder = new Pen(ImageListView.Colors.ImageInnerBorderColor))
                                {
                                    g.DrawRectangle(pInnerBorder, Rectangle.Inflate(pos, -1, -1));
                                }
                            }
                        }
                    }

                    // Draw item text
                    int lineHeight = CaptionFont.Height;
                    RectangleF rt;
                    using (StringFormat sf = new StringFormat())
                    {
                        rt = new RectangleF(bounds.Left + 2 * itemPadding.Width + ImageListView.ThumbnailSize.Width,
                            bounds.Top + itemPadding.Height + (Math.Max(ImageListView.ThumbnailSize.Height, mTextHeight) - mTextHeight) / 2,
                            mTileWidth, lineHeight);
                        sf.Alignment = StringAlignment.Near;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        using (Brush bItemFore = new SolidBrush(ImageListView.Colors.ForeColor))
                        {
                            g.DrawString(item.Text, CaptionFont, bItemFore, rt, sf);
                        }
                        using (Brush bItemDetails = new SolidBrush(ImageListView.Colors.PaneLabelColor))
                        {
                            rt.Offset(0, 1.5f * lineHeight);
                            string fileType = item.GetSubItemText(ColumnType.FileType);
                            if (!string.IsNullOrEmpty(fileType))
                            {
                                g.DrawString(fileType, ImageListView.Font, bItemDetails, rt, sf);
                                rt.Offset(0, 1.1f * lineHeight);
                            }
                            string dimensions = item.GetSubItemText(ColumnType.Dimensions);
                            string resolution = item.GetSubItemText(ColumnType.Resolution);
                            if (!string.IsNullOrEmpty(dimensions) || !string.IsNullOrEmpty(resolution))
                            {
                                string text = "";
                                if (!string.IsNullOrEmpty(dimensions))
                                    text += dimensions + " pixels ";
                                if (!string.IsNullOrEmpty(resolution))
                                    text += resolution.Split(new char[] { ' ', 'x' }, StringSplitOptions.RemoveEmptyEntries)[0] + " dpi";
                                g.DrawString(text, ImageListView.Font, bItemDetails, rt, sf);
                                rt.Offset(0, 1.1f * lineHeight);
                            }
                            string fileSize = item.GetSubItemText(ColumnType.FileSize);
                            if (!string.IsNullOrEmpty(fileSize))
                            {
                                g.DrawString(fileSize, ImageListView.Font, bItemDetails, rt, sf);
                                rt.Offset(0, 1.1f * lineHeight);
                            }
                            string dateModified = item.GetSubItemText(ColumnType.DateModified);
                            if (!string.IsNullOrEmpty(dateModified))
                            {
                                g.DrawString(dateModified, ImageListView.Font, bItemDetails, rt, sf);
                            }
                        }
                    }

                    // Item border
                    using (Pen pWhite128 = new Pen(Color.FromArgb(128, ImageListView.Colors.ControlBackColor)))
                    {
                        Utility.DrawRoundedRectangle(g, pWhite128, bounds.Left + 1, bounds.Top + 1, bounds.Width - 3, bounds.Height - 3, 4);
                    }
                    if (ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pHighlight128 = new Pen(ImageListView.Colors.SelectedBorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pGray128 = new Pen(ImageListView.Colors.UnFocusedBorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pGray128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if ((state & ItemState.Selected) == ItemState.None)
                    {
                        using (Pen pGray64 = new Pen(ImageListView.Colors.BorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pGray64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }

                    if (ImageListView.Focused && ((state & ItemState.Hovered) != ItemState.None))
                    {
                        using (Pen pHighlight64 = new Pen(ImageListView.Colors.HoverBorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }

                    // Focus rectangle
                    if (ImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
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
        /// This renderer cannot be themed.
        /// </summary>
        public class XPRenderer : ImageListView.ImageListViewRenderer
        {
            /// <summary>
            /// Gets a value indicating whether this renderer can apply custom colors.
            /// </summary>
            /// <value></value>
            public override bool CanApplyColors { get { return false; } }

            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the item measurement should be made.</param>
            public override Size MeasureItem(Manina.Windows.Forms.View view)
            {
                Size itemSize = new Size();

                // Reference text height
                int textHeight = ImageListView.Font.Height;

                if (view == Manina.Windows.Forms.View.Details)
                    return base.MeasureItem(view);
                else
                {
                    // Calculate item size
                    Size itemPadding = new Size(4, 4);
                    itemSize = ImageListView.ThumbnailSize + itemPadding + itemPadding;
                    itemSize.Height += textHeight + System.Math.Max(4, textHeight / 3) + itemPadding.Height; // textHeight / 3 = vertical space between thumbnail and text
                    return itemSize;
                }
            }
            /// <summary>
            /// Draws the background of the control.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The client coordinates of the item area.</param>
            public override void DrawBackground(Graphics g, Rectangle bounds)
            {
                g.Clear(SystemColors.Window);
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
                using (Brush bItemBack = new SolidBrush(SystemColors.Window))
                {
                    g.FillRectangle(bItemBack, bounds);
                }

                if (ImageListView.View != Manina.Windows.Forms.View.Details)
                {
                    Size itemPadding = new Size(4, 4);

                    // Draw the image
                    Image img = item.GetCachedImage(CachedImageType.Thumbnail);
                    if (img != null)
                    {
                        Rectangle border = new Rectangle(bounds.Location + itemPadding, ImageListView.ThumbnailSize);
                        Rectangle pos = Utility.GetSizedImageBounds(img, border);
                        g.DrawImage(img, pos);

                        // Draw image border
                        if (ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                        {
                            using (Pen pen = new Pen(SystemColors.Highlight, 3))
                            {
                                g.DrawRectangle(pen, border);
                            }
                        }
                        else if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                        {
                            using (Pen pen = new Pen(SystemColors.GrayText, 3))
                            {
                                pen.Alignment = PenAlignment.Center;
                                g.DrawRectangle(pen, border);
                            }
                        }
                        else
                        {
                            using (Pen pGray128 = new Pen(Color.FromArgb(128, SystemColors.GrayText)))
                            {
                                g.DrawRectangle(pGray128, border);
                            }
                        }
                    }

                    // Draw item text
                    SizeF szt = TextRenderer.MeasureText(g, item.Text, ImageListView.Font);
                    RectangleF rt;
                    using (StringFormat sf = new StringFormat())
                    {
                        rt = new RectangleF(bounds.Left + itemPadding.Width, bounds.Top + 3 * itemPadding.Height + ImageListView.ThumbnailSize.Height, ImageListView.ThumbnailSize.Width, szt.Height);
                        sf.Alignment = StringAlignment.Center;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        rt.Width += 1;
                        rt.Inflate(1, 2);
                        if (ImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
                            rt.Inflate(-1, -1);
                        if (ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                        {
                            g.FillRectangle(SystemBrushes.Highlight, rt);
                        }
                        else if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                        {
                            g.FillRectangle(SystemBrushes.GrayText, rt);
                        }
                        if (((state & ItemState.Selected) != ItemState.None))
                        {
                            g.DrawString(item.Text, ImageListView.Font, SystemBrushes.HighlightText, rt, sf);
                        }
                        else
                        {
                            using (Brush bItemFore = new SolidBrush(SystemColors.WindowText))
                            {
                                g.DrawString(item.Text, ImageListView.Font, bItemFore, rt, sf);
                            }
                        }
                    }

                    if (ImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
                    {
                        Rectangle fRect = Rectangle.Round(rt);
                        fRect.Inflate(1, 1);
                        ControlPaint.DrawFocusRectangle(g, fRect);
                    }
                }
                else // if (ImageListView.View == Manina.Windows.Forms.View.Details)
                {
                    if (ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        g.FillRectangle(SystemBrushes.Highlight, bounds);
                    }
                    else if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        g.FillRectangle(SystemBrushes.GrayText, bounds);
                    }

                    Size offset = new Size(2, (bounds.Height - ImageListView.Font.Height) / 2);
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Alignment = StringAlignment.Near;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        // Sub text
                        List<ImageListView.ImageListViewColumnHeader> uicolumns = ImageListView.Columns.GetDisplayedColumns();
                        RectangleF rt = new RectangleF(bounds.Left + offset.Width, bounds.Top + offset.Height, uicolumns[0].Width - 2 * offset.Width, bounds.Height - 2 * offset.Height);
                        foreach (ImageListView.ImageListViewColumnHeader column in uicolumns)
                        {
                            rt.Width = column.Width - 2 * offset.Width;
                            using (Brush bItemFore = new SolidBrush(SystemColors.WindowText))
                            {
                                int iconOffset = 0;
                                if (column.Type == ColumnType.Name)
                                {
                                    // Allocate space for checkbox and file icon
                                    if (ImageListView.ShowCheckBoxes && ImageListView.ShowFileIcons)
                                        iconOffset += 2 * 16 + 3 * 2;
                                    else if (ImageListView.ShowCheckBoxes)
                                        iconOffset += 16 + 2 * 2;
                                    else if (ImageListView.ShowFileIcons)
                                        iconOffset += 16 + 2 * 2;
                                }
                                rt.X += iconOffset;
                                rt.Width -= iconOffset;

                                if (column.Type == ColumnType.Rating && ImageListView.RatingImage != null && ImageListView.EmptyRatingImage != null)
                                {
                                    int w = ImageListView.RatingImage.Width;
                                    int y = (int)(rt.Top + (rt.Height - ImageListView.RatingImage.Height) / 2.0f);
                                    int rating = item.StarRating;
                                    if (rating < 0) rating = 0;
                                    if (rating > 5) rating = 5;
                                    for (int i = 1; i <= rating; i++)
                                        g.DrawImage(ImageListView.RatingImage, rt.Left + (i - 1) * w, y);
                                    for (int i = rating + 1; i <= 5; i++)
                                        g.DrawImage(ImageListView.EmptyRatingImage, rt.Left + (i - 1) * w, y);
                                }
                                else if (column.Type == ColumnType.Custom)
                                    g.DrawString(item.GetSubItemText(column.columnID), ImageListView.Font, ((state & ItemState.Selected) == ItemState.None ? bItemFore : SystemBrushes.HighlightText), rt, sf);
                                else
                                    g.DrawString(item.GetSubItemText(column.Type), ImageListView.Font, ((state & ItemState.Selected) == ItemState.None ? bItemFore : SystemBrushes.HighlightText), rt, sf);

                                rt.X -= iconOffset;
                            }
                            rt.X += column.Width;
                        }
                    }

                    if (ImageListView.Focused && ((state & ItemState.Focused) != ItemState.None))
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
                if (item != null && image != null)
                {
                    // Calculate image bounds
                    Size itemMargin = MeasureItemMargin(ImageListView.View);
                    Rectangle pos = Utility.GetSizedImageBounds(image, new Rectangle(bounds.Location + itemMargin, bounds.Size - itemMargin - itemMargin));
                    // Draw image
                    g.DrawImage(image, pos);

                    // Draw image border
                    if (Math.Min(pos.Width, pos.Height) > 32)
                    {
                        using (Pen pBorder = new Pen(SystemColors.WindowText))
                        {
                            g.DrawRectangle(pBorder, pos);
                        }
                    }
                }
            }
            /// <summary>
            /// Draws the left pane in Pane view mode.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="image">The image to draw.</param>
            /// <param name="bounds">The bounding rectangle of the pane.</param>
            public override void DrawPane(Graphics g, ImageListViewItem item, Image image, Rectangle bounds)
            {
                // Draw pane background
                using (Brush bBack = new SolidBrush(SystemColors.Window))
                {
                    g.FillRectangle(bBack, bounds);
                }
                using (Brush bBorder = new SolidBrush(Color.FromArgb(128, SystemColors.GrayText)))
                {
                    g.FillRectangle(bBorder, bounds.Right - 2, bounds.Top, 2, bounds.Height);
                }
                bounds.Width -= 2;

                if (item != null && image != null)
                {
                    // Calculate image bounds
                    Size itemMargin = MeasureItemMargin(ImageListView.View);
                    Rectangle pos = Utility.GetSizedImageBounds(image, new Rectangle(bounds.Location + itemMargin, bounds.Size - itemMargin - itemMargin), 50.0f, 0.0f);
                    // Draw image
                    g.DrawImage(image, pos);
                    // Draw image border
                    if (Math.Min(pos.Width, pos.Height) > 32)
                    {
                        using (Pen pBorder = new Pen(SystemColors.WindowText))
                        {
                            g.DrawRectangle(pBorder, pos);
                        }
                    }
                    bounds.X += itemMargin.Width;
                    bounds.Width -= 2 * itemMargin.Width;
                    bounds.Y = pos.Height + 16;
                    bounds.Height -= pos.Height + 16;

                    // Item text
                    if (ImageListView.Columns.HasType(ColumnType.Name) && ImageListView.Columns[ColumnType.Name].Visible && bounds.Height > 0)
                    {
                        string text = item.GetSubItemText(ColumnType.Name);
                        using (SolidBrush bLabel = new SolidBrush(SystemColors.GrayText))
                        using (SolidBrush bText = new SolidBrush(SystemColors.WindowText))
                        {
                            int y = Utility.DrawStringPair(g, bounds, "", text, ImageListView.Font, bLabel, bText);
                            bounds.Y += 2 * y;
                            bounds.Height -= 2 * y;
                        }
                    }

                    // File type
                    string fileType = item.GetSubItemText(ColumnType.FileType);
                    if (ImageListView.Columns.HasType(ColumnType.FileType) && ImageListView.Columns[ColumnType.FileType].Visible && bounds.Height > 0 && !string.IsNullOrEmpty(fileType))
                    {
                        using (SolidBrush bLabel = new SolidBrush(SystemColors.GrayText))
                        using (SolidBrush bText = new SolidBrush(SystemColors.WindowText))
                        {
                            int y = Utility.DrawStringPair(g, bounds, ImageListView.Columns[ColumnType.FileType].Text + ": ",
                                fileType, ImageListView.Font, bLabel, bText);
                            bounds.Y += y;
                            bounds.Height -= y;
                        }
                    }

                    // Metatada
                    foreach (ImageListView.ImageListViewColumnHeader column in ImageListView.Columns)
                    {
                        if (column.Type == ColumnType.ImageDescription)
                        {
                            bounds.Y += 8;
                            bounds.Height -= 8;
                        }

                        if (bounds.Height <= 0) break;

                        if (column.Visible &&
                            column.Type != ColumnType.Custom &&
                            column.Type != ColumnType.FileType &&
                            column.Type != ColumnType.DateAccessed &&
                            column.Type != ColumnType.FileName &&
                            column.Type != ColumnType.FilePath &&
                            column.Type != ColumnType.Name)
                        {
                            string caption = column.Text;
                            string text = item.GetSubItemText(column.Type);
                            if (!string.IsNullOrEmpty(text))
                            {
                                using (SolidBrush bLabel = new SolidBrush(SystemColors.GrayText))
                                using (SolidBrush bText = new SolidBrush(SystemColors.WindowText))
                                {
                                    int y = Utility.DrawStringPair(g, bounds, caption + ": ", text,
                                        ImageListView.Font, bLabel, bText);
                                    bounds.Y += y;
                                    bounds.Height -= y;
                                }
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// Draws the column headers.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="column">The ImageListViewColumnHeader to draw.</param>
            /// <param name="state">The current view state of column.</param>
            /// <param name="bounds">The bounding rectangle of column in client coordinates.</param>
            public override void DrawColumnHeader(Graphics g, ImageListView.ImageListViewColumnHeader column, ColumnState state, Rectangle bounds)
            {
                // Paint background
                if (ImageListView.Focused && ((state & ColumnState.Hovered) == ColumnState.Hovered))
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
                using (Brush bBorder = new LinearGradientBrush(bounds, Color.FromArgb(196, SystemColors.Control), Color.FromArgb(32, SystemColors.Control), LinearGradientMode.Vertical))
                using (Pen pBorder = new Pen(bBorder))
                {
                    g.DrawLine(pBorder, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                    g.DrawLine(pBorder, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                }
                using (Pen pSpep = new Pen(Color.FromArgb(32, SystemColors.Control)))
                {
                    g.DrawLine(pSpep, bounds.Left + 1, bounds.Top + 1, bounds.Left + 1, bounds.Bottom - 2);
                    g.DrawLine(pSpep, bounds.Right - 1, bounds.Top + 1, bounds.Right - 1, bounds.Bottom - 2);
                }

                // Draw the sort arrow
                int textOffset = 4;
                if (ImageListView.SortOrder != SortOrder.None &&
                    ImageListView.SortColumn >= 0 && ImageListView.SortColumn < ImageListView.Columns.Count &&
                    ImageListView.Columns[ImageListView.SortColumn].columnID == column.columnID)
                {
                    Image img = null;
                    if (ImageListView.SortOrder == SortOrder.Ascending)
                        img = ImageListViewResources.SortAscending;
                    else if (ImageListView.SortOrder == SortOrder.Descending)
                        img = ImageListViewResources.SortDescending;
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
                        using (SolidBrush bText = new SolidBrush(SystemColors.WindowText))
                        {
                            g.DrawString(column.Text, (ImageListView.HeaderFont == null ? ImageListView.Font : ImageListView.HeaderFont), bText, bounds, sf);
                        }
                    }
                }
            }
            /// <summary>
            /// Draws the extender after the last column.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of extender column in client coordinates.</param>
            public override void DrawColumnExtender(Graphics g, Rectangle bounds)
            {
                // Paint background
                using (Brush bBack = new LinearGradientBrush(bounds, Color.FromArgb(32, SystemColors.Control), Color.FromArgb(196, SystemColors.Control), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bBack, bounds);
                }
                using (Brush bBorder = new LinearGradientBrush(bounds, Color.FromArgb(196, SystemColors.Control), Color.FromArgb(32, SystemColors.Control), LinearGradientMode.Vertical))
                using (Pen pBorder = new Pen(bBorder))
                {
                    g.DrawLine(pBorder, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                    g.DrawLine(pBorder, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                }
                using (Pen pSpep = new Pen(Color.FromArgb(32, SystemColors.Control)))
                {
                    g.DrawLine(pSpep, bounds.Left + 1, bounds.Top + 1, bounds.Left + 1, bounds.Bottom - 2);
                }
            }
            /// <summary>
            /// Draws the selection rectangle.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="selection">The client coordinates of the selection rectangle.</param>
            public override void DrawSelectionRectangle(Graphics g, Rectangle selection)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, SystemColors.Highlight)))
                using (Pen pen = new Pen(SystemColors.Highlight))
                {
                    g.FillRectangle(brush, selection);
                    g.DrawRectangle(pen, selection);
                }
            }
            /// <summary>
            /// Draws the insertion caret for drag and drop operations.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of the insertion caret.</param>
            public override void DrawInsertionCaret(Graphics g, Rectangle bounds)
            {
                using (Brush b = new SolidBrush(SystemColors.Highlight))
                {
                    g.FillRectangle(b, bounds);
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

            /// <summary>
            /// Gets or sets the relative zoom ratio.
            /// </summary>
            public float ZoomRatio
            {
                get
                {
                    return mZoomRatio;
                }
                set
                {
                    mZoomRatio = value;
                    if (mZoomRatio < 0.0f) mZoomRatio = 0.0f;
                }
            }

            /// <summary>
            /// Initializes a new instance of the ZoomingRenderer class.
            /// </summary>
            public ZoomingRenderer()
                : this(0.5f)
            {
                ;
            }

            /// <summary>
            /// Initializes a new instance of the ZoomingRenderer class.
            /// </summary>
            /// <param name="zoomRatio">Relative zoom ratio.</param>
            public ZoomingRenderer(float zoomRatio)
            {
                if (zoomRatio < 0.0f) zoomRatio = 0.0f;
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

                ItemDrawOrder = ItemDrawOrder.NormalSelectedHovered;
            }
            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the item measurement should be made.</param>
            /// <returns></returns>
            public override Size MeasureItem(Manina.Windows.Forms.View view)
            {
                if (view == Manina.Windows.Forms.View.Thumbnails)
                    return ImageListView.ThumbnailSize + new Size(8, 8);
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
                Clip = (ImageListView.View == Manina.Windows.Forms.View.Details);

                if (ImageListView.View != Manina.Windows.Forms.View.Details)
                {
                    // Zoom on mouse over
                    if ((state & ItemState.Hovered) != ItemState.None)
                    {
                        bounds.Inflate((int)(bounds.Width * mZoomRatio), (int)(bounds.Height * mZoomRatio));
                        Rectangle controlBounds = ImageListView.ClientRectangle;
                        if (bounds.Bottom > controlBounds.Bottom)
                            bounds.Y = controlBounds.Bottom - bounds.Height;
                        if (bounds.Top < controlBounds.Top)
                            bounds.Y = controlBounds.Top;
                        if (bounds.Right > controlBounds.Right)
                            bounds.X = controlBounds.Right - bounds.Width;
                        if (bounds.Left < controlBounds.Left)
                            bounds.X = controlBounds.Left;
                    }

                    // Get item image
                    Image img = null;
                    if ((state & ItemState.Hovered) != ItemState.None)
                        img = GetImageAsync(item, new Size(bounds.Width - 8, bounds.Height - 8));
                    if (img == null) img = item.GetCachedImage(CachedImageType.Thumbnail);

                    int imageWidth = 0;
                    int imageHeight = 0;
                    if (img != null)
                    {
                        // Calculate image bounds
                        Rectangle pos = Utility.GetSizedImageBounds(img, Rectangle.Inflate(bounds, -4, -4));
                        imageWidth = pos.Width;
                        imageHeight = pos.Height;
                        int imageX = pos.X;
                        int imageY = pos.Y;

                        // Allocate space for item text
                        if ((state & ItemState.Hovered) != ItemState.None &&
                            (bounds.Height - imageHeight) / 2 < ImageListView.Font.Height + 8)
                        {
                            int delta = (ImageListView.Font.Height + 8) - (bounds.Height - imageHeight) / 2;
                            bounds.Height += 2 * delta;
                            imageY += delta;
                        }

                        // Paint background
                        using (Brush bItemBack = new SolidBrush(ImageListView.Colors.BackColor))
                        {
                            Utility.FillRoundedRectangle(g, bItemBack, bounds, 5);
                        }
                        if ((ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None)) ||
                            (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None) && ((state & ItemState.Hovered) != ItemState.None)))
                        {
                            using (Brush bSelected = new LinearGradientBrush(bounds, ImageListView.Colors.SelectedColor1, ImageListView.Colors.SelectedColor2, LinearGradientMode.Vertical))
                            {
                                Utility.FillRoundedRectangle(g, bSelected, bounds, 5);
                            }
                        }
                        else if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                        {
                            using (Brush bGray64 = new LinearGradientBrush(bounds, ImageListView.Colors.UnFocusedColor1, ImageListView.Colors.UnFocusedColor2, LinearGradientMode.Vertical))
                            {
                                Utility.FillRoundedRectangle(g, bGray64, bounds, 5);
                            }
                        }
                        if (((state & ItemState.Hovered) != ItemState.None))
                        {
                            using (Brush bHovered = new LinearGradientBrush(bounds, ImageListView.Colors.HoverColor1, ImageListView.Colors.HoverColor2, LinearGradientMode.Vertical))
                            {
                                Utility.FillRoundedRectangle(g, bHovered, bounds, 5);
                            }
                        }

                        // Draw the image
                        g.DrawImage(img, imageX, imageY, imageWidth, imageHeight);

                        // Draw image border
                        if (Math.Min(imageWidth, imageHeight) > 32)
                        {
                            using (Pen pOuterBorder = new Pen(ImageListView.Colors.ImageOuterBorderColor))
                            {
                                g.DrawRectangle(pOuterBorder, imageX, imageY, imageWidth, imageHeight);
                            }
                            if (System.Math.Min(imageWidth, imageHeight) > 32)
                            {
                                using (Pen pInnerBorder = new Pen(ImageListView.Colors.ImageInnerBorderColor))
                                {
                                    g.DrawRectangle(pInnerBorder, imageX + 1, imageY + 1, imageWidth - 2, imageHeight - 2);
                                }
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
                            using (Brush bItemFore = new SolidBrush(ImageListView.Colors.ForeColor))
                            {
                                g.DrawString(item.Text, ImageListView.Font, bItemFore, rt, sf);
                            }
                            rt.Y = bounds.Bottom - (bounds.Height - imageHeight) / 2 + 4;
                            string details = "";
                            string dimensions = item.GetSubItemText(ColumnType.Dimensions);
                            if (!string.IsNullOrEmpty(dimensions))
                                details += dimensions + " pixels ";
                            string fileSize = item.GetSubItemText(ColumnType.FileSize);
                            if (!string.IsNullOrEmpty(fileSize))
                                details += item.GetSubItemText(ColumnType.FileSize);
                            using (Brush bGrayText = new SolidBrush(ImageListView.Colors.PaneLabelColor))
                            {
                                g.DrawString(details, ImageListView.Font, bGrayText, rt, sf);
                            }
                        }
                    }

                    // Item border
                    using (Pen pWhite128 = new Pen(Color.FromArgb(128, ImageListView.Colors.ControlBackColor)))
                    {
                        Utility.DrawRoundedRectangle(g, pWhite128, bounds.Left + 1, bounds.Top + 1, bounds.Width - 3, bounds.Height - 3, 4);
                    }
                    if (ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pHighlight128 = new Pen(ImageListView.Colors.SelectedBorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    {
                        using (Pen pGray128 = new Pen(ImageListView.Colors.UnFocusedBorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pGray128, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                    else if ((state & ItemState.Selected) == ItemState.None)
                    {
                        using (Pen pGray64 = new Pen(ImageListView.Colors.BorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pGray64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }

                    if (ImageListView.Focused && ((state & ItemState.Hovered) != ItemState.None))
                    {
                        using (Pen pHighlight64 = new Pen(ImageListView.Colors.HoverBorderColor))
                        {
                            Utility.DrawRoundedRectangle(g, pHighlight64, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, 4);
                        }
                    }
                }
                else
                    base.DrawItem(g, item, state, bounds);
            }
            /// <summary>
            /// Draws the checkbox icon for the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="bounds">The bounding rectangle of the checkbox in client coordinates.</param>
            public override void DrawCheckBox(Graphics g, ImageListViewItem item, Rectangle bounds)
            {
                if (ImageListView.View == View.Details)
                    base.DrawCheckBox(g, item, bounds);
            }
            /// <summary>
            /// Draws the file icon for the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="bounds">The bounding rectangle of the file icon in client coordinates.</param>
            public override void DrawFileIcon(Graphics g, ImageListViewItem item, Rectangle bounds)
            {
                if (ImageListView.View == View.Details)
                    base.DrawFileIcon(g, item, bounds);
            }
        }
        #endregion

        #region ThemeRenderer
        /// <summary>
        /// Displays the control in the current system theme.
        /// This renderer cannot be themed.
        /// </summary>
        public class ThemeRenderer : ImageListView.ImageListViewRenderer
        {
            // Check boxes
            private VisualStyleRenderer rCheckedNormal = null;
            private VisualStyleRenderer rUncheckedNormal = null;
            // File icons
            private VisualStyleRenderer rFileIcon = null;
            // Column headers
            private VisualStyleRenderer rColumnNormal = null;
            private VisualStyleRenderer rColumnHovered = null;
            private VisualStyleRenderer rColumnSorted = null;
            private VisualStyleRenderer rColumnSortedHovered = null;
            private VisualStyleRenderer rSortAscending = null;
            private VisualStyleRenderer rSortDescending = null;
            // Items
            private VisualStyleRenderer rItemNormal = null;
            private VisualStyleRenderer rItemHovered = null;
            private VisualStyleRenderer rItemSelected = null;
            private VisualStyleRenderer rItemHoveredSelected = null;
            private VisualStyleRenderer rItemSelectedHidden = null;

            /// <summary>
            /// Gets whether visual styles are supported.
            /// </summary>
            public bool VisualStylesEnabled { get; private set; }

            /// <summary>
            /// Gets a value indicating whether this renderer can apply custom colors.
            /// </summary>
            /// <value></value>
            public override bool CanApplyColors { get { return false; } }

            /// <summary>
            /// Initializes a new instance of the ThemeRenderer class.
            /// </summary>
            public ThemeRenderer()
            {
                VisualStylesEnabled = Application.RenderWithVisualStyles;

                // Create renderers
                if (VisualStylesEnabled)
                {
                    // Check boxes
                    rCheckedNormal = GetRenderer(VisualStyleElement.Button.CheckBox.CheckedNormal);
                    rUncheckedNormal = GetRenderer(VisualStyleElement.Button.CheckBox.UncheckedNormal);
                    // File icons
                    rFileIcon = GetRenderer(VisualStyleElement.Button.PushButton.Normal);
                    // Column headers
                    rColumnNormal = GetRenderer("Header", 1, 1);
                    rColumnHovered = GetRenderer("Header", 1, 2);
                    rColumnSorted = GetRenderer("Header", 1, 4);
                    rColumnSortedHovered = GetRenderer("Header", 1, 5);
                    rSortAscending = GetRenderer(VisualStyleElement.Header.SortArrow.SortedUp);
                    rSortDescending = GetRenderer(VisualStyleElement.Header.SortArrow.SortedDown);
                    // Items
                    rItemNormal = GetRenderer("Explorer::ListView", 1, 1);
                    rItemHovered = GetRenderer("Explorer::ListView", 1, 2);
                    rItemSelected = GetRenderer("Explorer::ListView", 1, 3);
                    rItemHoveredSelected = GetRenderer("Explorer::ListView", 1, 6);
                    rItemSelectedHidden = GetRenderer("Explorer::ListView", 1, 5);
                }
            }

            /// <summary>
            /// Returns a renderer for the given element.
            /// </summary>
            private VisualStyleRenderer GetRenderer(VisualStyleElement e)
            {
                if (VisualStyleRenderer.IsElementDefined(e))
                    return new VisualStyleRenderer(e);
                else
                    return null;
            }

            /// <summary>
            /// Returns a renderer for the given element.
            /// </summary>
            private VisualStyleRenderer GetRenderer(string className, int part, int state)
            {
                VisualStyleElement e = VisualStyleElement.CreateElement(className, part, state);
                if (VisualStyleRenderer.IsElementDefined(e))
                    return new VisualStyleRenderer(e);
                else
                    return null;
            }

            /// <summary>
            /// Draws the checkbox icon for the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="bounds">The bounding rectangle of the checkbox in client coordinates.</param>
            public override void DrawCheckBox(Graphics g, ImageListViewItem item, Rectangle bounds)
            {
                VisualStyleRenderer renderer;
                if (item.Checked)
                    renderer = rCheckedNormal;
                else
                    renderer = rUncheckedNormal;

                if (VisualStylesEnabled && renderer != null)
                    renderer.DrawBackground(g, bounds, bounds);
                else
                    base.DrawCheckBox(g, item, bounds);
            }

            /// <summary>
            /// Draws the file icon for the specified item on the given graphics.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="item">The ImageListViewItem to draw.</param>
            /// <param name="bounds">The bounding rectangle of the file icon in client coordinates.</param>
            public override void DrawFileIcon(Graphics g, ImageListViewItem item, Rectangle bounds)
            {
                Image icon = item.GetCachedImage(CachedImageType.SmallIcon);

                if (icon != null && VisualStylesEnabled && rFileIcon != null)
                    rFileIcon.DrawImage(g, bounds, icon);
                else
                    base.DrawFileIcon(g, item, bounds);
            }

            /// <summary>
            /// Draws the column headers.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="column">The ImageListViewColumnHeader to draw.</param>
            /// <param name="state">The current view state of column.</param>
            /// <param name="bounds">The bounding rectangle of column in client coordinates.</param>
            public override void DrawColumnHeader(Graphics g, ImageListView.ImageListViewColumnHeader column, ColumnState state, Rectangle bounds)
            {
                SortOrder order = SortOrder.None;
                if (ImageListView.SortOrder != SortOrder.None &&
                    ImageListView.SortColumn >= 0 && ImageListView.SortColumn < ImageListView.Columns.Count &&
                    ImageListView.Columns[ImageListView.SortColumn].columnID == column.columnID)
                    order = ImageListView.SortOrder;

                VisualStyleRenderer rBack;
                if (((state & ColumnState.Hovered) == ColumnState.Hovered) && order != SortOrder.None)
                    rBack = rColumnSortedHovered;
                else if (((state & ColumnState.Hovered) == ColumnState.Hovered) && order == SortOrder.None)
                    rBack = rColumnHovered;
                else if (((state & ColumnState.Hovered) == ColumnState.None) && order != SortOrder.None)
                    rBack = rColumnSorted;
                else
                    rBack = rColumnNormal;

                VisualStyleRenderer rSort;
                if (order == SortOrder.Ascending)
                    rSort = rSortAscending;
                else
                    rSort = rSortDescending;

                // Background
                if (VisualStylesEnabled && rBack != null && rSort != null)
                {
                    // Background
                    rBack.DrawBackground(g, bounds, bounds);
                    // Sort arrow
                    if (order != SortOrder.None)
                    {
                        Size sz = rSort.GetPartSize(g, System.Windows.Forms.VisualStyles.ThemeSizeType.True);
                        Rectangle sortBounds = new Rectangle(new Point(0, 0), sz);
                        sortBounds.Offset(bounds.X + (bounds.Width - sz.Width) / 2, 0);
                        rSort.DrawBackground(g, sortBounds, sortBounds);
                    }

                    // Text
                    if (bounds.Width > 4)
                    {
                        Rectangle textBounds = bounds;
                        textBounds.Inflate(-3, 0);
                        TextRenderer.DrawText(g, column.Text,
                            SystemFonts.MenuFont, textBounds, SystemColors.ControlText,
                            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping);
                    }
                }
                else
                    base.DrawColumnHeader(g, column, state, bounds);
            }

            /// <summary>
            /// Draws the extender after the last column.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of extender column in client coordinates.</param>
            public override void DrawColumnExtender(Graphics g, Rectangle bounds)
            {
                if (VisualStylesEnabled && rColumnNormal != null)
                    rColumnNormal.DrawBackground(g, bounds, bounds);
                else
                    base.DrawColumnExtender(g, bounds);
            }

            /// <summary>
            /// Returns item size for the given view mode.
            /// </summary>
            /// <param name="view">The view mode for which the measurement should be made.</param>
            /// <returns></returns>
            public override Size MeasureItem(View view)
            {
                Size sz = base.MeasureItem(view);
                if (view != View.Details)
                {
                    sz.Width += 6;
                    sz.Height += 6;
                }
                return sz;
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
                VisualStyleRenderer rBack;

                if (!ImageListView.Focused && ((state & ItemState.Selected) != ItemState.None))
                    rBack = rItemSelectedHidden;
                else if (((state & ItemState.Selected) != ItemState.None) && ((state & ItemState.Hovered) != ItemState.None))
                    rBack = rItemHoveredSelected;
                else if ((state & ItemState.Selected) != ItemState.None)
                    rBack = rItemSelected;
                else if ((state & ItemState.Hovered) != ItemState.None)
                    rBack = rItemHovered;
                else
                    rBack = rItemNormal;

                if (VisualStylesEnabled && rBack != null)
                {
                    // Do not draw the background of normal items
                    if (((state & ItemState.Hovered) != ItemState.None) || ((state & ItemState.Selected) != ItemState.None))
                        rBack.DrawBackground(g, bounds, bounds);

                    Size itemPadding = new Size(7, 7);

                    // Draw the image
                    if (ImageListView.View != View.Details)
                    {
                        Image img = item.GetCachedImage(CachedImageType.Thumbnail);
                        if (img != null)
                        {
                            Rectangle pos = Utility.GetSizedImageBounds(img, new Rectangle(bounds.Location + itemPadding, ImageListView.ThumbnailSize));
                            // Image background
                            Rectangle imgback = pos;
                            imgback.Inflate(3, 3);
                            g.FillRectangle(SystemBrushes.Window, imgback);
                            // Image border
                            if (img.Width > 32 && img.Height > 32)
                            {
                                using (Pen pen = new Pen(Color.FromArgb(224, 224, 244)))
                                {
                                    g.DrawRectangle(pen, imgback.X, imgback.Y, imgback.Width - 1, imgback.Height - 1);
                                }
                            }
                            // Image
                            g.DrawImage(img, pos);
                        }

                        // Draw item text
                        Size szt = TextRenderer.MeasureText(item.Text, ImageListView.Font);
                        Rectangle rt = new Rectangle(
                            bounds.Left + itemPadding.Width, bounds.Top + 2 * itemPadding.Height + ImageListView.ThumbnailSize.Height,
                            ImageListView.ThumbnailSize.Width, szt.Height);
                        TextRenderer.DrawText(g, item.Text, ImageListView.Font, rt, SystemColors.ControlText,
                            TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping);
                    }
                    else // if (ImageListView.View == View.Details)
                    {
                        List<ImageListView.ImageListViewColumnHeader> uicolumns = ImageListView.Columns.GetDisplayedColumns();

                        // Separators 
                        int x = bounds.Left - 2;
                        foreach (ImageListView.ImageListViewColumnHeader column in uicolumns)
                        {
                            x += column.Width;
                            if (!ReferenceEquals(column, uicolumns[uicolumns.Count - 1]))
                            {
                                using (Pen pGray32 = new Pen(Color.FromArgb(32, 128, 128, 128)))
                                {
                                    g.DrawLine(pGray32, x, bounds.Top, x, bounds.Bottom);
                                }
                            }
                        }
                        Size offset = new Size(2, (bounds.Height - ImageListView.Font.Height) / 2);
                        // Sub text
                        int firstWidth = 0;
                        if (uicolumns.Count > 0)
                            firstWidth = uicolumns[0].Width;
                        Rectangle rt = new Rectangle(bounds.Left + offset.Width, bounds.Top + offset.Height, firstWidth - 2 * offset.Width, bounds.Height - 2 * offset.Height);
                        foreach (ImageListView.ImageListViewColumnHeader column in uicolumns)
                        {
                            rt.Width = column.Width - 2 * offset.Width;
                            using (Brush bItemFore = new SolidBrush(SystemColors.ControlText))
                            {
                                int iconOffset = 0;
                                if (column.Type == ColumnType.Name)
                                {
                                    // Allocate space for checkbox and file icon
                                    if (ImageListView.ShowCheckBoxes && ImageListView.ShowFileIcons)
                                        iconOffset += 2 * 16 + 3 * 2;
                                    else if (ImageListView.ShowCheckBoxes)
                                        iconOffset += 16 + 2 * 2;
                                    else if (ImageListView.ShowFileIcons)
                                        iconOffset += 16 + 2 * 2;
                                }
                                rt.X += iconOffset;
                                rt.Width -= iconOffset;
                                // Rating stars
                                if (column.Type == ColumnType.Rating && ImageListView.RatingImage != null && ImageListView.EmptyRatingImage != null)
                                {
                                    int rating = item.GetSimpleRating();
                                    if (rating > 0)
                                    {
                                        int w = ImageListView.RatingImage.Width;
                                        int y = (int)(rt.Top + (rt.Height - ImageListView.RatingImage.Height) / 2.0f);

                                        for (int i = 1; i <= 5; i++)
                                        {
                                            if (rating >= i)
                                                g.DrawImage(ImageListView.RatingImage, rt.Left + (i - 1) * w, y);
                                            else
                                                g.DrawImage(ImageListView.EmptyRatingImage, rt.Left + (i - 1) * w, y);
                                        }
                                    }
                                }
                                else if (column.Type == ColumnType.Custom)
                                    TextRenderer.DrawText(g, item.GetSubItemText(column.columnID), ImageListView.Font, rt, SystemColors.ControlText,
                                        TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping);
                                else
                                    TextRenderer.DrawText(g, item.GetSubItemText(column.Type), ImageListView.Font, rt, SystemColors.ControlText,
                                        TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping);

                                rt.X -= iconOffset;
                            }
                            rt.X += column.Width;
                        }
                    }
                }
                else
                    base.DrawItem(g, item, state, bounds);
            }
        }
        #endregion
    }
}