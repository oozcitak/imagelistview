#if DEBUG
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;

namespace Manina.Windows.Forms
{
    public class DebugRenderer : ImageListView.ImageListViewRenderer
    {
        private long baseMem;

        public DebugRenderer()
        {
            Process p = Process.GetCurrentProcess();
            p.Refresh();
            baseMem = p.PrivateMemorySize64;
        }

        public override void DrawItem(Graphics g, ImageListViewItem item, ItemState state, Rectangle bounds)
        {
            if (item.Index == mImageListView.layoutManager.FirstPartiallyVisible ||
                item.Index == mImageListView.layoutManager.LastPartiallyVisible)
                g.FillRectangle(Brushes.Yellow, bounds);
            if (item.Index == mImageListView.layoutManager.FirstVisible ||
                item.Index == mImageListView.layoutManager.LastVisible)
                g.FillRectangle(Brushes.Green, bounds);

            base.DrawItem(g, item, state, bounds);
        }

        public override void DrawOverlay(Graphics g, Rectangle bounds)
        {
            Process p = Process.GetCurrentProcess();
            p.Refresh();
            long mem = Math.Max(0, p.PrivateMemorySize64 - baseMem);
            string s = string.Format("Total: {0}\r\nCache: {1}\r\nCache*: {2}", Utility.FormatSize(baseMem), Utility.FormatSize(mem), Utility.FormatSize(mImageListView.cacheManager.MemoryUsed));
            SizeF sz = g.MeasureString(s, ImageListView.Font);
            Rectangle r = new Rectangle(ItemAreaBounds.Right - (int)sz.Width - 5, ItemAreaBounds.Top + 5, (int)sz.Width, (int)sz.Height);
            using (Brush b = new SolidBrush(Color.FromArgb(220, Color.LightGray)))
            {
                g.FillRectangle(b, r);
            }
            using (Pen pen = new Pen(Color.FromArgb(128, Color.Gray)))
            {
                g.DrawRectangle(pen, r);
            }
            g.DrawString(s, ImageListView.Font, Brushes.Black, r.Location);

            r = new Rectangle(ItemAreaBounds.Right - 60, ItemAreaBounds.Top + 5 + (int)sz.Height + 10, 55, 55);
            using (Brush b = new SolidBrush(Color.FromArgb(220, Color.LightGray)))
            {
                g.FillRectangle(b, r);
            }
            using (Pen pen = new Pen(Color.FromArgb(128, Color.Gray)))
            {
                g.DrawRectangle(pen, r);
            }
            r = new Rectangle(r.Left + 5, r.Top + 5, 15, 15);
            if (mImageListView.navigationManager.LeftButton)
            {
                g.FillRectangle(Brushes.DarkGray, r);
            }
            g.DrawRectangle(Pens.Black, r);
            r.Offset(15, 0);
            r.Offset(15, 0);
            if (mImageListView.navigationManager.RightButton)
            {
                g.FillRectangle(Brushes.DarkGray, r);
            }
            g.DrawRectangle(Pens.Black, r);
            r.Offset(-30, 22);

            Color tColor = Color.Gray;
            if (mImageListView.navigationManager.ShiftKey)
                tColor = Color.Black;
            using (Brush b = new SolidBrush(tColor))
            {
                g.DrawString("Shift", mImageListView.Font, b, r.Location);
            }
            r.Offset(0, 12);

            tColor = Color.Gray;
            if (mImageListView.navigationManager.ControlKey)
                tColor = Color.Black;
            using (Brush b = new SolidBrush(tColor))
            {
                g.DrawString("Control", mImageListView.Font, b, r.Location);
            }
        }
    }
}
#endif