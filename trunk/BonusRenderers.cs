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

#if BONUSPACK
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the built-in renderers.
    /// </summary>
    public static partial class ImageListViewRenderers
    {
        #region NewYear2010Renderer
        /// <summary>
        /// A renderer to celebrate the new year of 2010.
        /// </summary>
        /// <remarks>Compile with conditional compilation symbol BONUSPACK to
        /// include this renderer in the assembly.</remarks>
        public class NewYear2010Renderer : ImageListView.ImageListViewRenderer
        {
            /// <summary>
            /// Represents a snow flake
            /// </summary>
            private class SnowFlake : IDisposable
            {
                public Point Location { get; set; }
                public double Rotation { get; set; }
                public int Speed { get; set; }

                private GraphicsPath outline;
                public GraphicsPath Outline { get { return outline; } }

                private int size;
                public int Size { get { return size; } }

                public SnowFlake(int newSize)
                {
                    outline = CreateOutline(newSize);
                    Location = new Point(0, 0);
                    Rotation = 0.0;
                }

                private GraphicsPath CreateOutline(int newSize)
                {
                    size = newSize;
                    GraphicsPath path = new GraphicsPath();
                    int corners = 6;
                    float minsize = size;
                    float maxsize = 1.6f * size;
                    float step = 2.0f * (float)Math.PI / (float)corners;
                    for (int i = 0; i < corners; i++)
                    {
                        float angle = ((float)i) * step;
                        path.AddLine((float)Math.Cos(angle) * maxsize, (float)Math.Sin(angle) * maxsize,
                            (float)Math.Cos(angle + step / 2) * minsize, (float)Math.Sin(angle + step / 2) * minsize);
                        path.AddLine((float)Math.Cos(angle + step / 2) * minsize, (float)Math.Sin(angle + step / 2) * minsize,
                            (float)Math.Cos(angle + step) * maxsize, (float)Math.Sin(angle + step) * maxsize);
                    }
                    path.CloseFigure();
                    return path;
                }

                public void Dispose()
                {
                    outline.Dispose();
                }
            }

            private int flakeCount = 500;
            private int minFlakeSize = 2;
            private int maxFlakeSize = 6;
            private int refreshPeriod = 50;
            private int flakeCreation = 3;
            private int cycleCount = 0;

            private DateTime lastPaintTime;
            private List<SnowFlake> flakes = null;
            private System.Threading.Timer timer;
            private Random random = new Random();

            private GraphicsPath terrain;

            /// <summary>
            /// Initializes a new instance of the NewYear2010Renderer class.
            /// </summary>
            public NewYear2010Renderer()
            {
                lastPaintTime = DateTime.Now;
                terrain = CreateTerrain();
                timer = new System.Threading.Timer(UpdateTimerCallback, null, 0, refreshPeriod);
            }

            /// <summary>
            /// Generates a random snowy terrain.
            /// </summary>
            private GraphicsPath CreateTerrain()
            {
                Random rnd = new Random();
                GraphicsPath path = new GraphicsPath();
                int width = 100;
                int height = 10;

                int count = 20;
                int step = width / count;
                int lastx = 0, lasty = 0;
                Point[] points = new Point[count];
                for (int i = 0; i < count; i++)
                {
                    int x = i * (width + 2 * step) / count - step;
                    int y = rnd.Next(-height / 2, height / 2);
                    points[i] = new Point(x, y);
                    lastx = x;
                    lasty = y;
                }
                path.AddCurve(points);

                path.AddLine(lastx, lasty, width + step, 0);
                path.AddLine(width + step, 0, width + step, 200);
                path.AddLine(width + step, 200, -step, 200);

                path.CloseFigure();
                return path;
            }

            /// <summary>
            /// Updates the timer callback.
            /// </summary>
            private void UpdateTimerCallback(object state)
            {
                if (ImageListView != null)
                {
                    Rectangle rec = ImageListView.DisplayRectangle;

                    if (flakes == null)
                        flakes = new List<SnowFlake>();

                    cycleCount++;
                    if (cycleCount == flakeCreation)
                    {
                        cycleCount = 0;
                        if (flakes.Count < flakeCount)
                        {
                            SnowFlake flake = new SnowFlake(random.Next(minFlakeSize, maxFlakeSize));
                            flake.Rotation = 2.0 * Math.PI * random.NextDouble();
                            flake.Location = new Point(random.Next(rec.Left, rec.Right), -20);
                            flake.Speed = flake.Size;
                            flakes.Add(flake);
                        }
                    }

                    for (int i = flakes.Count - 1; i >= 0; i--)
                    {
                        SnowFlake flake = flakes[i];
                        if (flake.Location.Y > rec.Height)
                            flakes.Remove(flake);
                        else
                        {
                            flake.Location = new Point(flake.Location.X, flake.Location.Y + flake.Speed);
                            flake.Rotation += 360.0 / 40.0;
                            if (flake.Rotation > 360.0) flake.Rotation -= 360.0;
                        }
                    }

                    if ((DateTime.Now - lastPaintTime).Milliseconds > refreshPeriod)
                    {
                        try
                        {
                            if (ImageListView.InvokeRequired)
                                ImageListView.Invoke((MethodInvoker)delegate { ImageListView.Refresh(); });
                            else
                                ImageListView.Refresh();
                        }
                        catch
                        {
                            ;
                        }
                    }
                }
            }

            /// <summary>
            /// Draws an overlay image over the client area.
            /// </summary>
            /// <param name="g">The System.Drawing.Graphics to draw on.</param>
            /// <param name="bounds">The bounding rectangle of the client area.</param>
            public override void DrawOverlay(Graphics g, Rectangle bounds)
            {
                lastPaintTime = DateTime.Now;

                DrawTerrain(g, terrain);

                if (flakes != null)
                {
                    for (int i = 0; i < flakes.Count; i++)
                        DrawSnowFlake(g, flakes[i]);
                }

                DrawTerrainOverlay(g, terrain);
            }

            /// <summary>
            /// Draws the terrain.
            /// </summary>
            private void DrawTerrain(Graphics g, GraphicsPath path)
            {
                g.ResetTransform();
                using (SolidBrush brush = new SolidBrush(Color.White))
                using (Pen pen = new Pen(Color.Gray))
                {
                    Rectangle rec = ImageListView.DisplayRectangle;
                    g.ScaleTransform((float)rec.Width / 100.0f, 3.0f, MatrixOrder.Append);
                    g.TranslateTransform(0, rec.Height - 30, MatrixOrder.Append);
                    g.FillPath(brush, terrain);
                    g.DrawPath(pen, terrain);
                }
                g.ResetTransform();
            }

            /// <summary>
            /// Draws the overlay on the terrain.
            /// </summary>
            private void DrawTerrainOverlay(Graphics g, GraphicsPath path)
            {
                g.ResetTransform();
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    Rectangle rec = ImageListView.DisplayRectangle;
                    g.ScaleTransform((float)rec.Width / 100.0f, 3.0f, MatrixOrder.Append);
                    g.TranslateTransform(0, rec.Height - 20, MatrixOrder.Append);
                    g.FillPath(brush, terrain);
                }
                g.ResetTransform();
            }

            /// <summary>
            /// Draws a snow flake.
            /// </summary>
            private void DrawSnowFlake(Graphics g, SnowFlake flake)
            {
                using (SolidBrush brush = new SolidBrush(Color.White))
                using (Pen glow = new Pen(Color.FromArgb(96, Color.White), 2.0f))
                using (Pen pen = new Pen(Color.Gray))
                {
                    g.ResetTransform();
                    g.TranslateTransform(-flake.Size / 2, -flake.Size / 2, MatrixOrder.Append);
                    g.RotateTransform((float)flake.Rotation, MatrixOrder.Append);
                    g.TranslateTransform(flake.Location.X, flake.Location.Y, MatrixOrder.Append);
                    using (GraphicsPath glowPath = (GraphicsPath)flake.Outline.Clone())
                    {
                        glowPath.Widen(glow);
                        g.DrawPath(glow, glowPath);
                    }
                    g.FillPath(brush, flake.Outline);
                    g.DrawPath(pen, flake.Outline);
                    g.ResetTransform();
                }
            }

            /// <summary>
            /// Releases managed resources.
            /// </summary>
            public override void OnDispose()
            {
                base.OnDispose();
                foreach (SnowFlake flake in flakes)
                    flake.Dispose();
                terrain.Dispose();
                timer.Dispose();
            }
        }
        #endregion
    }
}
#endif