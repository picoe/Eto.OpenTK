using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace otkViewPort
{
    public class openTKViewPort
    {
        bool loaded = false;
        private GLControl glControl;
        private OVPSettings ovpSettings;
        private float axisZ;
        private float gridZ;

        // Use for drag handling.
        bool dragging;
        private float x_orig;
        private float y_orig;

        private Point WorldToScreen(float x, float y)
        {
            return new Point((int)((x - ovpSettings.cameraPosition.X / ovpSettings.zoomFactor) + glControl.ClientRectangle.Width / 2),
                             (int)((y - ovpSettings.cameraPosition.Y / ovpSettings.zoomFactor) + glControl.ClientRectangle.Height / 2));
        }

        private Point WorldToScreen(PointF pt)
        {
            return WorldToScreen(pt.X, pt.Y);
        }

        private Size WorldToScreen(SizeF pt)
        {
            Point pt1 = WorldToScreen(0, 0);
            Point pt2 = WorldToScreen(pt.Width, pt.Height);
            return new Size(pt2.X - pt1.X, pt2.Y - pt1.Y);
        }

        private PointF ScreenToWorld(int x, int y)
        {
            return new PointF((float)(x - glControl.ClientRectangle.Width / 2) * ovpSettings.zoomFactor + ovpSettings.cameraPosition.X,
                              (float)(y - glControl.ClientRectangle.Height / 2) * ovpSettings.zoomFactor + ovpSettings.cameraPosition.Y);
        }

        private PointF ScreenToWorld(Point pt)
        {
            return ScreenToWorld(pt.X, pt.Y);
        }

        private RectangleF getViewPort()
        {
            glControl.MakeCurrent();
            PointF bl = ScreenToWorld(glControl.ClientRectangle.Left, glControl.ClientRectangle.Bottom);
            PointF tr = ScreenToWorld(glControl.ClientRectangle.Right, glControl.ClientRectangle.Top);
            return new RectangleF(bl.X, bl.Y, tr.X - bl.X, tr.Y - bl.Y);
        }

        private void setViewPort(float x1, float y1, float x2, float y2)
        {
            glControl.MakeCurrent();
            float h = Math.Abs(y1 - y2);
            float w = Math.Abs(x1 - x2);
            ovpSettings.cameraPosition = new PointF((x1 + x2) / 2, (y1 + y2) / 2);
            if ((glControl.ClientRectangle.Height != 0) && (glControl.ClientRectangle.Width != 0))
                ovpSettings.zoomFactor = Math.Max(h / (float)(glControl.ClientRectangle.Height), w / (float)(glControl.ClientRectangle.Width));
            else
                ovpSettings.zoomFactor = 1;
        }

        private void downHandler(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!dragging) // might not be needed, but seemed like a safe approach to avoid re-setting these in a drag event.
                {
                    x_orig = e.X;
                    y_orig = e.Y;
                    dragging = true;
                }
            }
        }

        private void dragHandler(object sender, MouseEventArgs e)
        {
            glControl.MakeCurrent();
            if (e.Button == MouseButtons.Left)
            {
                object locking = new object();
                lock (locking)
                {
                    // Scaling factor is arbitrary - just based on testing to avoid insane panning speeds.
                    float new_X = (ovpSettings.cameraPosition.X - (((float)e.X - x_orig) / 100.0f));
                    float new_Y = (ovpSettings.cameraPosition.Y + (((float)e.Y - y_orig) / 100.0f));
                    ovpSettings.cameraPosition = new PointF(new_X, new_Y);
                }
            }
            updateViewport();
        }

        private void upHandler(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;
            }
        }

        public void viewport_load()
        {
            loaded = true;
        }

        private void zoomIn()
        {
            glControl.MakeCurrent();
            ovpSettings.zoomFactor += (ovpSettings.zoomStep * 0.01f);
        }

        private void zoomOut()
        {
            ovpSettings.zoomFactor -= (ovpSettings.zoomStep * 0.01f);
            if (ovpSettings.zoomFactor < 0.0001)
            {
                ovpSettings.zoomFactor = 0.0001f; // avoid any chance of getting to zero.
            }
        }

        private void panVertical(float delta)
        {
            glControl.MakeCurrent();
            ovpSettings.cameraPosition.Y += delta / 10;
        }

        private void panHorizontal(float delta)
        {
            glControl.MakeCurrent();
            ovpSettings.cameraPosition.X += delta / 10;
        }

        private void addKeyHandler(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
            glControl.KeyDown += keyHandler;
        }

        private void removeKeyHandler(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
            glControl.KeyDown -= keyHandler;
        }

        private void keyHandler(object sender, KeyEventArgs e)
        {
            glControl.MakeCurrent();
            if (e.KeyCode == Keys.R)
            {
                ovpSettings.cameraPosition = new PointF(0, 0);
                ovpSettings.zoomFactor = 1.0f;
            }

            float stepping = 10.0f * ovpSettings.zoomFactor;

            if (e.KeyCode == Keys.A)
            {
                panHorizontal(-stepping);
            }
            if (e.KeyCode == Keys.D)
            {
                panHorizontal(stepping);
            }
            if (e.KeyCode == Keys.W)
            {
                panVertical(stepping);
            }
            if (e.KeyCode == Keys.S)
            {
                panVertical(-stepping);
            }
            updateViewport();
        }

        private void zoomHandler(object sender, MouseEventArgs e)
        {
            glControl.MakeCurrent();
            float wheelZoom = e.Delta / SystemInformation.MouseWheelScrollLines;
            if (wheelZoom > 0)
            {
                zoomIn();
            }
            if (wheelZoom < 0)
            {
                zoomOut();
            }
            updateViewport();
        }

        public void updateViewport()
        {
            glControl.MakeCurrent();
            init();
            drawGrid();
            drawAxes();
            drawPolygons();
            glControl.SwapBuffers();
        }

        private void drawPolygons()
        {
            glControl.MakeCurrent();
            try
            {
                GL.LoadIdentity();
                float polyZStep = 1.0f / ovpSettings.polyList.Count();
                for (int poly = 0; poly < ovpSettings.polyList.Count(); poly++)
                {
                    float polyZ = poly * polyZStep;
                    GL.Color3(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B);
                    GL.Begin(PrimitiveType.Lines);
                    for (int pt = 0; pt < ovpSettings.polyList[poly].poly.Length - 1; pt++)
                    {
                        GL.Vertex3(ovpSettings.polyList[poly].poly[pt].X, ovpSettings.polyList[poly].poly[pt].Y, polyZ);
                        GL.Vertex3(ovpSettings.polyList[poly].poly[pt + 1].X, ovpSettings.polyList[poly].poly[pt + 1].Y, polyZ);
                    }
                    GL.End();
                }
            }
            catch (Exception)
            {
                // Can ignore - not critical.
            }
        }

        public void defaults()
        {
            glControl.MakeCurrent();
            if (ovpSettings.antiAlias)
            {
                GL.Enable(EnableCap.LineSmooth);
            }
            else
            {
                GL.Disable(EnableCap.LineSmooth);
            }
            GL.Disable(EnableCap.Lighting);
            GL.ShadeModel(ShadingModel.Flat);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.DepthFunc(DepthFunction.Less);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(0.0f, 0.5f);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.LineStipple(1, 61680);
            gridZ = -0.95f;
            axisZ = gridZ + 0.01f;
        }

        public openTKViewPort(OpenTK.GLControl viewportControl, OVPSettings svpSettings)
        {
            viewportControl.MakeCurrent();
            glControl = viewportControl;
            ovpSettings = svpSettings;
            glControl.MouseDown += downHandler;
            glControl.MouseMove += dragHandler;
            glControl.MouseUp += upHandler;
            glControl.MouseWheel += zoomHandler;
            glControl.MouseEnter += addKeyHandler;
            glControl.MouseHover += addKeyHandler;
            glControl.MouseLeave += removeKeyHandler;
            defaults();
            GL.Viewport(0, 0, glControl.ClientRectangle.Width, glControl.ClientRectangle.Height);
        }

        public void init()
        {
            glControl.MakeCurrent();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(ovpSettings.cameraPosition.X - ((float)glControl.ClientRectangle.Width) * ovpSettings.zoomFactor / 2,
                          ovpSettings.cameraPosition.X + ((float)glControl.ClientRectangle.Width) * ovpSettings.zoomFactor / 2,
                          ovpSettings.cameraPosition.Y - ((float)glControl.ClientRectangle.Height) * ovpSettings.zoomFactor / 2,
                          ovpSettings.cameraPosition.Y + ((float)glControl.ClientRectangle.Height) * ovpSettings.zoomFactor / 2,
                          -1.0f, 1.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            ovpSettings.bounds = getViewPort();
            GL.ClearColor(ovpSettings.backColor.R / 255, ovpSettings.backColor.G / 255, ovpSettings.backColor.B / 255, ovpSettings.backColor.A / 255);
            GL.ClearDepth(1.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void drawGrid()
        {
            glControl.MakeCurrent();
            GL.LoadIdentity();
            if (ovpSettings.showGrid)
            {
                float spacing = ovpSettings.gridSpacing;
                if (ovpSettings.dynamicGrid)
                {
                    while (WorldToScreen(new SizeF(spacing, 0.0f)).Width > 12.0f)
                        spacing /= 10.0f;

                    while (WorldToScreen(new SizeF(spacing, 0.0f)).Width < 4.0f)
                        spacing *= 10.0f;
                }
                if (WorldToScreen(new SizeF(spacing, 0.0f)).Width >= 4.0f)
                {
                    int k = 0;
                    GL.Begin(PrimitiveType.Lines);
                    for (float i = 0; i > ovpSettings.bounds.Left; i -= spacing)
                    {
                        if (k <= 1)
                        {
                            GL.Color4(ovpSettings.minorGridColor.R, ovpSettings.minorGridColor.G, ovpSettings.minorGridColor.B, ovpSettings.minorGridColor.A);
                        }
                        if (k == 10)
                        {
                            GL.Color4(ovpSettings.majorGridColor.R, ovpSettings.majorGridColor.G, ovpSettings.majorGridColor.B, ovpSettings.majorGridColor.A);
                            k = 0;
                        }
                        k++;
                        GL.Vertex3(i, ovpSettings.bounds.Top, gridZ);
                        GL.Vertex3(i, ovpSettings.bounds.Bottom, gridZ);
                    }
                    GL.End();
                    k = 0;
                    GL.Begin(PrimitiveType.Lines);
                    for (float i = 0; i < ovpSettings.bounds.Right; i += spacing)
                    {
                        if (k <= 1)
                        {
                            GL.Color4(ovpSettings.minorGridColor.R, ovpSettings.minorGridColor.G, ovpSettings.minorGridColor.B, ovpSettings.minorGridColor.A);
                        }
                        if (k == 10)
                        {
                            GL.Color4(ovpSettings.majorGridColor.R, ovpSettings.majorGridColor.G, ovpSettings.majorGridColor.B, ovpSettings.majorGridColor.A);
                            k = 0;
                        }
                        k++;
                        GL.Vertex3(i, ovpSettings.bounds.Top, gridZ);
                        GL.Vertex3(i, ovpSettings.bounds.Bottom, gridZ);
                    }
                    GL.End();
                    k = 0;
                    GL.Begin(PrimitiveType.Lines);
                    for (float i = 0; i > ovpSettings.bounds.Bottom; i -= spacing)
                    {
                        if (k <= 1)
                        {
                            GL.Color4(ovpSettings.minorGridColor.R, ovpSettings.minorGridColor.G, ovpSettings.minorGridColor.B, ovpSettings.minorGridColor.A);
                        }
                        if (k == 10)
                        {
                            GL.Color4(ovpSettings.majorGridColor.R, ovpSettings.majorGridColor.G, ovpSettings.majorGridColor.B, ovpSettings.majorGridColor.A);
                            k = 0;
                        }
                        k++;
                        GL.Vertex3(ovpSettings.bounds.Left, i, gridZ);
                        GL.Vertex3(ovpSettings.bounds.Right, i, gridZ);
                    }
                    GL.End();
                    k = 0;
                    GL.Begin(PrimitiveType.Lines);
                    for (float i = 0; i < ovpSettings.bounds.Top; i += spacing)
                    {
                        if (k <= 1)
                        {
                            GL.Color4(ovpSettings.minorGridColor.R, ovpSettings.minorGridColor.G, ovpSettings.minorGridColor.B, ovpSettings.minorGridColor.A);
                        }
                        if (k == 10)
                        {
                            GL.Color4(ovpSettings.majorGridColor.R, ovpSettings.majorGridColor.G, ovpSettings.majorGridColor.B, ovpSettings.majorGridColor.A);
                            k = 0;
                        }
                        k++;
                        GL.Vertex3(ovpSettings.bounds.Left, i, gridZ);
                        GL.Vertex3(ovpSettings.bounds.Right, i, gridZ);
                    }
                    GL.End();
                }
            }
        }

        public void drawAxes()
        {
            glControl.MakeCurrent();
            GL.LoadIdentity();
            if (ovpSettings.showAxes)
            {
                GL.Color4(ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B, ovpSettings.axisColor.A);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(0.0f, ovpSettings.bounds.Top, axisZ);
                GL.Vertex3(0.0f, ovpSettings.bounds.Bottom, axisZ);
                GL.Vertex3(ovpSettings.bounds.Left, 0.0f, axisZ);
                GL.Vertex3(ovpSettings.bounds.Right, 0.0f, axisZ);
                GL.End();
            }
        }
    }
}
