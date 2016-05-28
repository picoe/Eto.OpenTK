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
    public class openTKViewPort2
    {
        public bool ok;
        Vector3[] polyArray;
        Vector3[] polyColorArray;
        int poly_vbo_size;

        Vector3[] gridArray;
        Vector3[] gridColorArray;
        int grid_vbo_size;

        Vector3[] axesArray;
        Vector3[] axesColorArray;
        int axes_vbo_size;

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
            try
            {
                glControl.MakeCurrent();
                init();
                drawGrid();
                drawAxes();
                drawPolygons();

                // Fix in case of nulls
                if (polyArray == null)
                {
                    polyArray = new Vector3[2];
                    polyColorArray = new Vector3[polyArray.Length];
                    for (int i = 0; i < polyArray.Length; i++)
                    {
                        polyArray[i] = new Vector3(0.0f);
                        polyColorArray[i] = new Vector3(1.0f);
                    }
                }

                // Now we wrangle our VBOs
                grid_vbo_size = gridArray.Length; // Necessary for rendering later on
                axes_vbo_size = axesArray.Length; // Necessary for rendering later on
                poly_vbo_size = polyArray.Length; // Necessary for rendering later on

                int[] vbo_id = new int[3]; // three buffers to be applied
                GL.GenBuffers(3, vbo_id);

                int[] col_id = new int[3];
                GL.GenBuffers(3, col_id);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[0]);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              new IntPtr(gridArray.Length * BlittableValueType.StrideOf(gridArray)),
                              gridArray, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[0]);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              new IntPtr(gridColorArray.Length * BlittableValueType.StrideOf(gridColorArray)),
                              gridColorArray, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[1]);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              new IntPtr(axesArray.Length * BlittableValueType.StrideOf(axesArray)),
                              axesArray, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[1]);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              new IntPtr(axesColorArray.Length * BlittableValueType.StrideOf(axesColorArray)),
                              axesColorArray, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[2]);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              new IntPtr(polyArray.Length * BlittableValueType.StrideOf(polyArray)),
                              polyArray, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[2]);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              new IntPtr(polyColorArray.Length * BlittableValueType.StrideOf(polyColorArray)),
                              polyColorArray, BufferUsageHint.StaticDraw);

                // To draw a VBO:
                // 1) Ensure that the VertexArray client state is enabled.
                // 2) Bind the vertex and element buffer handles.
                // 3) Set up the data pointers (vertex, normal, color) according to your vertex format.


                try
                {
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[0]);
                    GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[0]);
                    GL.ColorPointer(3, ColorPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
                    GL.DrawArrays(PrimitiveType.Lines, 0, grid_vbo_size);
                    GL.DisableClientState(ArrayCap.VertexArray);
                    GL.DisableClientState(ArrayCap.ColorArray);

                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[1]);
                    GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[1]);
                    GL.ColorPointer(3, ColorPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
                    GL.DrawArrays(PrimitiveType.Lines, 0, axes_vbo_size);
                    GL.DisableClientState(ArrayCap.VertexArray);
                    GL.DisableClientState(ArrayCap.ColorArray);

                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[2]);
                    GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[2]);
                    GL.ColorPointer(3, ColorPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
                    GL.DrawArrays(PrimitiveType.Lines, 0, poly_vbo_size);
                    GL.DisableClientState(ArrayCap.VertexArray);
                    GL.DisableClientState(ArrayCap.ColorArray);
                }
                catch (Exception)
                {

                }

                glControl.SwapBuffers();
                GL.Flush();
                GL.DeleteBuffers(3, vbo_id);
                GL.DeleteBuffers(3, col_id);
            }
            catch (Exception)
            {
                ok = false;
            }
        }

        private void drawPolygons()
        {
            try
            {
                List<Vector3> polyList = new List<Vector3>();
                List<Vector3> polyColorList = new List<Vector3>();
                float polyZStep = 1.0f / ovpSettings.polyList.Count();
                for (int poly = 0; poly < ovpSettings.polyList.Count(); poly++)
                {
                    float polyZ = poly * polyZStep;
                    for (int pt = 0; pt < ovpSettings.polyList[poly].poly.Length - 1; pt++)
                    {
                        polyList.Add(new Vector3(ovpSettings.polyList[poly].poly[pt].X, ovpSettings.polyList[poly].poly[pt].Y, polyZ));
                        polyColorList.Add(new Vector3(ovpSettings.polyList[poly].color.R / 255.0f, ovpSettings.polyList[poly].color.G / 255.0f, ovpSettings.polyList[poly].color.B / 255.0f));
                        polyList.Add(new Vector3(ovpSettings.polyList[poly].poly[pt + 1].X, ovpSettings.polyList[poly].poly[pt + 1].Y, polyZ));
                        polyColorList.Add(new Vector3(ovpSettings.polyList[poly].color.R / 255.0f, ovpSettings.polyList[poly].color.G / 255.0f, ovpSettings.polyList[poly].color.B / 255.0f));
                    }
                }

                polyArray = polyList.ToArray();
                polyColorArray = polyColorList.ToArray();
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
            //GL.Enable(EnableCap.DepthTest);
            //GL.DepthMask(true);
            //GL.DepthFunc(DepthFunction.Less);
            //GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(0.0f, 0.5f);
            //GL.EnableClientState(ArrayCap.VertexArray);
            //GL.EnableClientState(ArrayCap.ColorArray);
            GL.LineStipple(1, 61680);
            gridZ = -0.95f;
            axisZ = gridZ + 0.01f;
        }

        public openTKViewPort2(OpenTK.GLControl viewportControl, OVPSettings svpSettings)
        {
            try
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
                ok = true;
            }
            catch (Exception)
            {
                ok = false;
            }
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

                List<Vector3> grid = new List<Vector3>();
                List<Vector3> gridColors = new List<Vector3>();

                if (WorldToScreen(new SizeF(spacing, 0.0f)).Width >= 4.0f)
                {
                    int k = 0;
                    for (float i = 0; i > ovpSettings.bounds.Left; i -= spacing)
                    {
                        float r = 0.0f;
                        float g = 0.0f;
                        float b = 0.0f;
                        if (k <= 9)
                        {
                            r = ovpSettings.minorGridColor.R / 255.0f;
                            g = ovpSettings.minorGridColor.G / 255.0f;
                            b = ovpSettings.minorGridColor.B / 255.0f;
                        }
                        if (k == 10)
                        {
                            r = ovpSettings.majorGridColor.R / 255.0f;
                            g = ovpSettings.majorGridColor.G / 255.0f;
                            b = ovpSettings.majorGridColor.B / 255.0f;
                            k = 0;
                        }
                        k++;
                        grid.Add(new Vector3(i, ovpSettings.bounds.Top, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                        grid.Add(new Vector3(i, ovpSettings.bounds.Bottom, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                    }
                    k = 0;
                    for (float i = 0; i < ovpSettings.bounds.Right; i += spacing)
                    {
                        float r = 0.0f;
                        float g = 0.0f;
                        float b = 0.0f;
                        if (k <= 9)
                        {
                            r = ovpSettings.minorGridColor.R / 255.0f;
                            g = ovpSettings.minorGridColor.G / 255.0f;
                            b = ovpSettings.minorGridColor.B / 255.0f;
                        }
                        if (k == 10)
                        {
                            r = ovpSettings.majorGridColor.R / 255.0f;
                            g = ovpSettings.majorGridColor.G / 255.0f;
                            b = ovpSettings.majorGridColor.B / 255.0f;
                            k = 0;
                        }
                        k++;
                        grid.Add(new Vector3(i, ovpSettings.bounds.Top, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                        grid.Add(new Vector3(i, ovpSettings.bounds.Bottom, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                    }
                    k = 0;
                    for (float i = 0; i > ovpSettings.bounds.Bottom; i -= spacing)
                    {
                        float r = 0.0f;
                        float g = 0.0f;
                        float b = 0.0f;
                        if (k <= 9)
                        {
                            r = ovpSettings.minorGridColor.R / 255.0f;
                            g = ovpSettings.minorGridColor.G / 255.0f;
                            b = ovpSettings.minorGridColor.B / 255.0f;
                        }
                        if (k == 10)
                        {
                            r = ovpSettings.majorGridColor.R / 255.0f;
                            g = ovpSettings.majorGridColor.G / 255.0f;
                            b = ovpSettings.majorGridColor.B / 255.0f;
                            k = 0;
                        }
                        k++;
                        grid.Add(new Vector3(ovpSettings.bounds.Left, i, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                        grid.Add(new Vector3(ovpSettings.bounds.Right, i, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                    }
                    k = 0;
                    for (float i = 0; i < ovpSettings.bounds.Top; i += spacing)
                    {
                        float r = 0.0f;
                        float g = 0.0f;
                        float b = 0.0f;
                        if (k <= 9)
                        {
                            r = ovpSettings.minorGridColor.R / 255.0f;
                            g = ovpSettings.minorGridColor.G / 255.0f;
                            b = ovpSettings.minorGridColor.B / 255.0f;
                        }
                        if (k == 10)
                        {
                            r = ovpSettings.majorGridColor.R / 255.0f;
                            g = ovpSettings.majorGridColor.G / 255.0f;
                            b = ovpSettings.majorGridColor.B / 255.0f;
                            k = 0;
                        }
                        k++;
                        grid.Add(new Vector3(ovpSettings.bounds.Left, i, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                        grid.Add(new Vector3(ovpSettings.bounds.Right, i, gridZ));
                        gridColors.Add(new Vector3(r, g, b));
                    }
                    gridArray = grid.ToArray();
                    gridColorArray = gridColors.ToArray();
                }
            }
        }

        public void drawAxes()
        {
            if (ovpSettings.showAxes)
            {
                axesArray = new Vector3[4];
                axesColorArray = new Vector3[4];
                for (int i = 0; i < axesColorArray.Length; i++)
                {
                    axesColorArray[i] = new Vector3(ovpSettings.axisColor.R / 255.0f, ovpSettings.axisColor.G / 255.0f, ovpSettings.axisColor.B / 255.0f);
                }
                axesArray[0] = new Vector3(0.0f, ovpSettings.bounds.Top, axisZ);
                axesArray[1] = new Vector3(0.0f, ovpSettings.bounds.Bottom, axisZ);
                axesArray[2] = new Vector3(ovpSettings.bounds.Left, 0.0f, axisZ);
                axesArray[3] = new Vector3(ovpSettings.bounds.Right, 0.0f, axisZ);
            }
        }
    }
}
