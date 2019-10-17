using Eto.Forms;
using Eto.OpenTK;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;

using System;
namespace TestEtoGl
{
	public class TestViewport : GLSurface
	{
		public delegate void updateHost();
		public updateHost updateHostFunc { get; set; }

		public bool ok;
		bool immediateMode;
		public bool lockedViewport;
		public bool savedLocation_valid;
		PointF savedLocation;

		Vector3[] polyArray;
		Vector4[] polyColorArray;
		int[] first;
		int[] count;
		int poly_vbo_size;

		Vector3[] lineArray;
		Vector4[] lineColorArray;
		int[] lineFirst;
		int[] lineCount;
		int line_vbo_size;

		Vector3[] gridArray;
		Vector3[] gridColorArray;
		int grid_vbo_size;

		Vector3[] axesArray;
		Vector3[] axesColorArray;
		int axes_vbo_size;

		public OVPSettings ovpSettings; // note that this is a reference to the real settings.
		float axisZ;
		float gridZ;

		// Use for drag handling.
		bool dragging;
		float x_orig;
		float y_orig;

        ContextMenu menu;

		Point WorldToScreen(float x, float y)
		{
			return new Point((int)((x - ovpSettings.cameraPosition.X / (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + Width / 2),
					(int)((y - ovpSettings.cameraPosition.Y / (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + Height / 2));
		}

		Point WorldToScreen(PointF pt)
		{
			return WorldToScreen(pt.X, pt.Y);
		}

		Size WorldToScreen(SizeF pt)
		{
			Point pt1 = WorldToScreen(0, 0);
			Point pt2 = WorldToScreen(pt.Width, pt.Height);
			return new Size(pt2.X - pt1.X, pt2.Y - pt1.Y);
		}

		PointF ScreenToWorld(int x, int y)
		{
			return new PointF((float)(x - Width / 2) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) + ovpSettings.cameraPosition.X,
					 (float)(y - Height / 2) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) + ovpSettings.cameraPosition.Y);
		}

		PointF ScreenToWorld(Point pt)
		{
			return ScreenToWorld(pt.X, pt.Y);
		}

		RectangleF getViewPort()
		{
			PointF bl = ScreenToWorld(Location.X - Width / 2, Location.Y - Height / 2);
			PointF tr = ScreenToWorld(Location.X + Width / 2, Location.Y + Height / 2);
			return new RectangleF(bl.X, bl.Y, tr.X - bl.X, tr.Y - bl.Y);
		}

		void setViewPort(float x1, float y1, float x2, float y2)
		{
			float h = Math.Abs(y1 - y2);
			float w = Math.Abs(x1 - x2);
			ovpSettings.cameraPosition = new PointF((x1 + x2) / 2, (y1 + y2) / 2);
			if ((Height != 0) && (Width != 0))
			{
				ovpSettings.zoomFactor = Math.Max(h / (float)(Height), w / (float)(Width));
			}
			else
			{
				ovpSettings.zoomFactor = 1;
			}
		}

		void downHandler(object sender, MouseEventArgs e)
		{
			if (e.Buttons == MouseButtons.Primary)
			{
				if (!dragging && !lockedViewport) // might not be needed, but seemed like a safe approach to avoid re-setting these in a drag event.
				{
					x_orig = e.Location.X;
					y_orig = e.Location.Y;
					dragging = true;
				}
			}
			//e.Handled = true;
		}

		public void saveLocation()
		{
			savedLocation = new PointF(ovpSettings.cameraPosition.X, ovpSettings.cameraPosition.Y);
			savedLocation_valid = true;
		}

		public void zoomExtents()
		{
			getExtents();

			if (((ovpSettings.polyList.Count == 0) && (ovpSettings.lineList.Count == 0)) || 
                ((ovpSettings.minX == 0) && (ovpSettings.maxX == 0)) ||
				((ovpSettings.minY == 0) && (ovpSettings.maxY == 0)))
			{
				reset();
				return;
			}

			// Locate camera at center of the polygon field.
			float dX = ovpSettings.maxX - ovpSettings.minX;
			float dY = ovpSettings.maxY - ovpSettings.minY;
			float cX = (dX / 2.0f) + ovpSettings.minX;
			float cY = (dY / 2.0f) + ovpSettings.minY;

			// Now need to get the zoom level organized.
			float zoomLevel_x = dX / Width;
			float zoomLevel_y = dY / Height;

			if (zoomLevel_x > zoomLevel_y)
			{
				ovpSettings.zoomFactor = zoomLevel_x / ovpSettings.base_zoom;
			}
			else
			{
				ovpSettings.zoomFactor = zoomLevel_y / ovpSettings.base_zoom;
			}

			goToLocation(cX, cY);
		}

		public void loadLocation()
		{
			if (savedLocation_valid)
			{
				ovpSettings.cameraPosition = new PointF(savedLocation.X, savedLocation.Y);
				updateViewport();
			}
		}

		public void goToLocation(float x, float y)
		{
			ovpSettings.cameraPosition = new PointF(x, y);
			updateViewport();
		}

		void dragHandler(object sender, MouseEventArgs e)
		{
			if (lockedViewport)
			{
				return;
			}
			if (e.Buttons == MouseButtons.Primary)
			{
				object locking = new object();
				lock (locking)
				{
                    // Scaling factor is arbitrary - just based on testing to avoid insane panning speeds.
                    float new_X = (ovpSettings.cameraPosition.X - (((float)e.Location.X - x_orig) * ovpSettings.zoomFactor));
                    float new_Y = (ovpSettings.cameraPosition.Y + (((float)e.Location.Y - y_orig) * ovpSettings.zoomFactor));
                    ovpSettings.cameraPosition = new PointF(new_X, new_Y);
                    x_orig = e.Location.X;
                    y_orig = e.Location.Y;
                }
			}
			updateViewport();
			//e.Handled = true;
		}

		public void freeze_thaw()
		{
			lockedViewport = !lockedViewport;
		}

		void upHandler(object sender, MouseEventArgs e)
		{
			if (lockedViewport)
			{
				return;
			}
			if (e.Buttons == MouseButtons.Primary)
			{
				dragging = false;
			}
            if (e.Buttons == MouseButtons.Alternate)
            {
                if (menu != null)
                {
                    menu.Show(this);
                }
            }
			//e.Handled = true
		}

		public void zoomIn(float delta)
		{
			if (lockedViewport)
			{
				return;
			}
			ovpSettings.zoomFactor += (ovpSettings.zoomStep * 0.01f * delta);
		}

		public void zoomOut(float delta)
		{
			if (lockedViewport)
			{
				return;
			}
			ovpSettings.zoomFactor -= (ovpSettings.zoomStep * 0.01f * delta);
			if (ovpSettings.zoomFactor < 0.0001)
			{
				ovpSettings.zoomFactor = 0.0001f; // avoid any chance of getting to zero.
			}
		}

		void panVertical(float delta)
		{
			if (lockedViewport)
			{
				return;
			}
			ovpSettings.cameraPosition.Y += delta / 10;
		}

		void panHorizontal(float delta)
		{
			if (lockedViewport)
			{
				return;
			}
			ovpSettings.cameraPosition.X += delta / 10;
		}

		void addKeyHandler(object sender, EventArgs e)
		{
			KeyDown += keyHandler;
		}

		void removeKeyHandler(object sender, EventArgs e)
		{
			KeyDown -= keyHandler;
		}

		public void reset()
		{
			if (lockedViewport)
			{
				return;
			}
			ovpSettings.cameraPosition = new PointF(ovpSettings.default_cameraPosition.X, ovpSettings.default_cameraPosition.Y);
			ovpSettings.zoomFactor = 1.0f;
		}

		void keyHandler(object sender, KeyEventArgs e)
		{
			if (lockedViewport)
			{
				if (e.Key != Keys.F)
				{
					return;
				}
				lockedViewport = false;
				return;
			}

			if (e.Key == Keys.F)
			{
				lockedViewport = true;
				return;
			}

			if (e.Key == Keys.R)
			{
				reset();
			}

			float stepping = 10.0f * ovpSettings.zoomFactor;

			bool doUpdate = true;
			if (e.Key == Keys.A)
			{
				panHorizontal(-stepping);
			}
			if (e.Key == Keys.D)
			{
				panHorizontal(stepping);
			}
			if (e.Key == Keys.W)
			{
				panVertical(stepping);
			}
			if (e.Key == Keys.S)
			{
				panVertical(-stepping);
			}
			if (e.Key == Keys.N)
			{
				zoomOut(-1);
			}
			if (e.Key == Keys.M)
			{
				zoomIn(-1);
			}

			if (e.Key == Keys.X)
			{
				zoomExtents();
				doUpdate = false; // update performed in extents
			}

			if (doUpdate)
			{
				updateViewport();
			}
			e.Handled = true;
		}

		void zoomHandler(object sender, MouseEventArgs e)
		{
			if (lockedViewport)
			{
				return;
			}

			float wheelZoom = e.Delta.Height; // SystemInformation.MouseWheelScrollLines;
			if (wheelZoom > 0)
			{
				zoomIn(wheelZoom);
			}
			if (wheelZoom < 0)
			{
				zoomOut(-wheelZoom);
			}
			updateViewport();
			//e.Handled = true;
		}

		public void updateViewport()
		{
			if (immediateMode)
			{
				_updateVP_immediate();
			}
			else
			{
				try
				{
					_updateVP_VBO();
				}
				catch (Exception)
				{
					// Fallback in case VBO support blows up.
					immediateMode = true;
					_updateVP_immediate();
				}
			}
		}

		void _updateVP_immediate()
		{
			MakeCurrent();
			init();
			drawGrid_immediate();
			drawAxes_immediate();
			drawPolygons_immediate();
            drawLines_immediate();
			SwapBuffers();
		}

        // Need this to handle the OpenTK memory violation if VBO isn't supported. Without this, the exception is managed by the runtime and the tool crashes.
        // We can, however, handle this gracefully.
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        void _updateVP_VBO()
		{
			if (!IsInitialized)
				return;
			try
			{
				init();
				try
				{
					drawGrid_VBO();
					drawAxes_VBO();
					drawPolygons_VBO();
					drawLines_VBO();
				}
				catch (Exception)
				{
					throw new Exception("VBO had an issue. Aborting.");
				}

				// Fix in case of nulls
				if (polyArray == null)
				{
					polyArray = new Vector3[2];
					polyColorArray = new Vector4[polyArray.Length];
					for (int i = 0; i < polyArray.Length; i++)
					{
						polyArray[i] = new Vector3(0.0f);
						polyColorArray[i] = new Vector4(1.0f);
					}
				}
				if (lineArray == null)
				{
					lineArray = new Vector3[2];
					lineColorArray = new Vector4[lineArray.Length];
					for (int i = 0; i < lineArray.Length; i++)
					{
						lineArray[i] = new Vector3(0.0f);
						lineColorArray[i] = new Vector4(1.0f);
					}
				}


				// Now we wrangle our VBOs
				grid_vbo_size = gridArray.Length; // Necessary for rendering later on
				axes_vbo_size = axesArray.Length; // Necessary for rendering later on
				poly_vbo_size = polyArray.Length; // Necessary for rendering later on
				line_vbo_size = lineArray.Length;

				int numBuffers = 4;
				int[] vbo_id = new int[numBuffers];
				GL.GenBuffers(numBuffers, vbo_id);

				int[] col_id = new int[numBuffers];
				GL.GenBuffers(numBuffers, col_id);

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

				GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[3]);
				GL.BufferData(BufferTarget.ArrayBuffer,
						  new IntPtr(lineArray.Length * BlittableValueType.StrideOf(lineArray)),
						  lineArray, BufferUsageHint.StaticDraw);

				GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[3]);
				GL.BufferData(BufferTarget.ArrayBuffer,
						  new IntPtr(lineColorArray.Length * BlittableValueType.StrideOf(lineColorArray)),
						  lineColorArray, BufferUsageHint.StaticDraw);

				// To draw a VBO:
				// 1) Ensure that the VertexArray client state is enabled.
				// 2) Bind the vertex and element buffer handles.
				// 3) Set up the data pointers(vertex, normal, color) according to your vertex format.


				try
				{
					if (ovpSettings.antiAlias)
					{
						GL.Enable(EnableCap.Multisample);
						if (ovpSettings.drawPoints)
						{
							GL.Enable(EnableCap.PointSmooth); // should result in circles rather than squares. We shall see.
						}
						//GL.Enable(EnableCap.LineSmooth);
						//GL.Enable(EnableCap.PolygonSmooth);
					}

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

					if (ovpSettings.drawPoints)
					{
						GL.PointSize(2.0f);
					}

					// Allow alpha blending
					GL.Enable(EnableCap.Blend);
					GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

					// Poly data
					GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[2]);
					GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
					GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[2]);
					GL.ColorPointer(4, ColorPointerType.Float, Vector4.SizeInBytes, new IntPtr(0));
					if (ovpSettings.enableFilledPolys)
					{
						// Draw our filled shapes, using the first index and count. We have triangles from upstream tessellation.
						GL.MultiDrawArrays(PrimitiveType.Triangles, first, count, first.Length);
						// Disable the alpha blending to draw the border without blending.
						GL.Disable(EnableCap.Blend);
					}
					else
					{
						// Draw the border.
						GL.DrawArrays(PrimitiveType.Lines, 0, poly_vbo_size);
						if (ovpSettings.drawPoints)
						{
							GL.DrawArrays(PrimitiveType.Points, 0, poly_vbo_size);
						}
					}
					GL.DisableClientState(ArrayCap.VertexArray);
					GL.DisableClientState(ArrayCap.ColorArray);

					GL.EnableClientState(ArrayCap.VertexArray);
					GL.EnableClientState(ArrayCap.ColorArray);
					// Line data
					GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[3]);
					GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
					GL.BindBuffer(BufferTarget.ArrayBuffer, col_id[3]);
					GL.ColorPointer(4, ColorPointerType.Float, Vector4.SizeInBytes, new IntPtr(0));
					GL.DrawArrays(PrimitiveType.Lines, 0, line_vbo_size);
					
					GL.Disable(EnableCap.Blend);
					if (ovpSettings.antiAlias)
					{
						GL.Disable(EnableCap.Multisample);
						//GL.Disable(EnableCap.LineSmooth);
						//GL.Disable(EnableCap.PolygonSmooth);
					}
					GL.DisableClientState(ArrayCap.VertexArray);
					GL.DisableClientState(ArrayCap.ColorArray);
				}
				catch (Exception)
				{
					throw new Exception("VBO had an issue. Aborting.");
				}

				SwapBuffers();
				GL.Flush();
				GL.DeleteBuffers(numBuffers, vbo_id);
				GL.DeleteBuffers(numBuffers, col_id);
			}
			catch (Exception)
			{
				ok = false;
				throw new Exception("VBO had an issue. Aborting.");
			}
			updateHostFunc?.Invoke();
		}

		void getExtents()
		{
            float minX = 0;
            float maxX = 0;
            float minY = 0, maxY = 0;

			if ((ovpSettings.polyList.Count == 0) && (ovpSettings.lineList.Count == 0))
			{
				ovpSettings.minX = 0;
				ovpSettings.maxX = 0;
				ovpSettings.minY = 0;
				ovpSettings.maxY = 0;
				return;
			}

            if (ovpSettings.polyList.Count != 0)
            {
                minX = ovpSettings.polyList[0].poly[0].X;
                maxX = ovpSettings.polyList[0].poly[0].X;
                minY = ovpSettings.polyList[0].poly[0].Y;
                maxY = ovpSettings.polyList[0].poly[0].Y;
                for (int poly = 0; poly < ovpSettings.polyList.Count; poly++)
                {
                    float tMinX = ovpSettings.polyList[poly].poly.Min(p => p.X);
                    if (tMinX < minX)
                    {
                        minX = tMinX;
                    }
                    float tMaxX = ovpSettings.polyList[poly].poly.Max(p => p.X);
                    if (tMaxX > maxX)
                    {
                        maxX = tMaxX;
                    }
                    float tMinY = ovpSettings.polyList[poly].poly.Min(p => p.Y);
                    if (tMinY < minY)
                    {
                        minY = tMinY;
                    }
                    float tMaxY = ovpSettings.polyList[poly].poly.Max(p => p.Y);
                    if (tMaxY > maxY)
                    {
                        maxY = tMaxY;
                    }
                }
            }

            if (ovpSettings.lineList.Count != 0)
            {
                for (int line = 0; line < ovpSettings.lineList.Count; line++)
                {
                    float tMinX = ovpSettings.lineList[line].poly.Min(p => p.X);
                    if (tMinX < minX)
                    {
                        minX = tMinX;
                    }
                    float tMaxX = ovpSettings.lineList[line].poly.Max(p => p.X);
                    if (tMaxX > maxX)
                    {
                        maxX = tMaxX;
                    }
                    float tMinY = ovpSettings.lineList[line].poly.Min(p => p.Y);
                    if (tMinY < minY)
                    {
                        minY = tMinY;
                    }
                    float tMaxY = ovpSettings.lineList[line].poly.Max(p => p.Y);
                    if (tMaxY > maxY)
                    {
                        maxY = tMaxY;
                    }
                }
            }

            ovpSettings.minX = minX;
			ovpSettings.maxX = maxX;
			ovpSettings.minY = minY;
			ovpSettings.maxY = maxY;
		}

		void drawPolygons_VBO()
		{
			try
			{
				List<Vector3> polyList = new List<Vector3>();
				List<Vector4> polyColorList = new List<Vector4>();

				// Carve our Z-space up to stack polygons
				float polyZStep = 1.0f / ovpSettings.polyList.Count();

				// Create our first and count arrays for the vertex indices, to enable polygon separation when rendering.
				first = new int[ovpSettings.polyList.Count()];
				count = new int[ovpSettings.polyList.Count()];
				int counter = 0; // vertex count that will be used to define 'first' index for each polygon.
				int previouscounter = 0; // will be used to derive the number of vertices in each polygon.

				for (int poly = 0; poly < ovpSettings.polyList.Count(); poly++)
				{
					float alpha = ovpSettings.polyList[poly].alpha;
					float polyZ = poly * polyZStep;
					first[poly] = counter;
					previouscounter = counter;
					if ((ovpSettings.enableFilledPolys) && (!ovpSettings.drawnPoly[poly]))
					{
						polyList.Add(new Vector3(ovpSettings.polyList[poly].poly[0].X, ovpSettings.polyList[poly].poly[0].Y, polyZ));
						polyList.Add(new Vector3(ovpSettings.polyList[poly].poly[1].X, ovpSettings.polyList[poly].poly[1].Y, polyZ));
						polyList.Add(new Vector3(ovpSettings.polyList[poly].poly[2].X, ovpSettings.polyList[poly].poly[2].Y, polyZ));
						polyColorList.Add(new Vector4(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha));
						polyColorList.Add(new Vector4(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha));
						polyColorList.Add(new Vector4(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha));
						counter += 3;
						count[poly] = 3;
					}
					else
					{
						for (int pt = 0; pt < ovpSettings.polyList[poly].poly.Length - 1; pt++)
						{
							polyList.Add(new Vector3(ovpSettings.polyList[poly].poly[pt].X, ovpSettings.polyList[poly].poly[pt].Y, polyZ));
							counter++;
							polyColorList.Add(new Vector4(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha));
							polyList.Add(new Vector3(ovpSettings.polyList[poly].poly[pt + 1].X, ovpSettings.polyList[poly].poly[pt + 1].Y, polyZ));
							counter++;
							polyColorList.Add(new Vector4(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha));
						}
						count[poly] = counter - previouscounter; // set our vertex count for the polygon.
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

		void drawPolygons_immediate()
		{
			MakeCurrent();
			try
			{
				GL.LoadIdentity();
                if (ovpSettings.enableFilledPolys)
                {
                    float polyZ = 1.0f / (ovpSettings.polyList.Count() + 1); // push our filled polygons behind the boundary
                    for (int poly = 0; poly < ovpSettings.polyList.Count(); poly++)
                    {
                        GL.Color4(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, ovpSettings.polyList[poly].alpha);
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        GL.Begin(PrimitiveType.Triangles);
                        GL.Vertex3(new Vector3(ovpSettings.polyList[poly].poly[0].X, ovpSettings.polyList[poly].poly[0].Y, polyZ));
                        GL.Vertex3(new Vector3(ovpSettings.polyList[poly].poly[1].X, ovpSettings.polyList[poly].poly[1].Y, polyZ));
                        GL.Vertex3(new Vector3(ovpSettings.polyList[poly].poly[2].X, ovpSettings.polyList[poly].poly[2].Y, polyZ));
                        GL.End();
                    }
                }
                else
                {
                    // Carve our Z-space up to stack polygons
                    float polyZStep = 1.0f / ovpSettings.polyList.Count();
                    for (int poly = 0; poly < ovpSettings.polyList.Count(); poly++)
                    {
                        float polyZ = poly * polyZStep;
                        GL.Color4(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, ovpSettings.polyList[poly].alpha);
                        GL.Begin(PrimitiveType.Lines);
                        for (int pt = 0; pt < ovpSettings.polyList[poly].poly.Length - 1; pt++)
                        {
                            GL.Vertex3(ovpSettings.polyList[poly].poly[pt].X, ovpSettings.polyList[poly].poly[pt].Y, polyZ);
                            GL.Vertex3(ovpSettings.polyList[poly].poly[pt + 1].X, ovpSettings.polyList[poly].poly[pt + 1].Y, polyZ);
                        }
                        GL.End();
                    }
                }
            }
			catch (Exception)
			{
				// Can ignore - not critical.
			}
		}

		void drawLines_VBO()
		{
			try
			{
				List<Vector3> polyList = new List<Vector3>();
				List<Vector4> polyColorList = new List<Vector4>();

				// Carve our Z-space up to stack polygons
				float polyZStep = 1.0f / ovpSettings.lineList.Count();

				// Create our first and count arrays for the vertex indices, to enable polygon separation when rendering.
				int tmp = ovpSettings.lineList.Count();
				lineFirst = new int[tmp];
				lineCount = new int[tmp];
				int counter = 0; // vertex count that will be used to define 'first' index for each polygon.
				int previouscounter = 0; // will be used to derive the number of vertices in each polygon.

				for (int poly = 0; poly < ovpSettings.lineList.Count(); poly++)
				{
					float alpha = ovpSettings.lineList[poly].alpha;
					float polyZ = poly * polyZStep;
					lineFirst[poly] = counter;
					previouscounter = counter;
					for (int pt = 0; pt < ovpSettings.lineList[poly].poly.Length - 1; pt++)
					{
						polyList.Add(new Vector3(ovpSettings.lineList[poly].poly[pt].X, ovpSettings.lineList[poly].poly[pt].Y, polyZ));
						counter++;
						polyColorList.Add(new Vector4(ovpSettings.lineList[poly].color.R, ovpSettings.lineList[poly].color.G, ovpSettings.lineList[poly].color.B, alpha));
						polyList.Add(new Vector3(ovpSettings.lineList[poly].poly[pt + 1].X, ovpSettings.lineList[poly].poly[pt + 1].Y, polyZ));
						counter++;
						polyColorList.Add(new Vector4(ovpSettings.lineList[poly].color.R, ovpSettings.lineList[poly].color.G, ovpSettings.lineList[poly].color.B, alpha));
					}
					lineCount[poly] = counter - previouscounter; // set our vertex count for the polygon.
				}

				lineArray = polyList.ToArray();
				lineColorArray = polyColorList.ToArray();
			}
			catch (Exception)
			{
				// Can ignore - not critical.
			}
		}

        void drawLines_immediate()
        {
            try
            {
                // Carve our Z-space up to stack polygons
                float polyZStep = 1.0f / ovpSettings.lineList.Count();

                for (int poly = 0; poly < ovpSettings.lineList.Count(); poly++)
                {
                    float polyZ = poly * polyZStep;
                    GL.Color4(ovpSettings.lineList[poly].color.R, ovpSettings.lineList[poly].color.G, ovpSettings.lineList[poly].color.B, ovpSettings.lineList[poly].alpha);
                    GL.Begin(PrimitiveType.Lines);
                    for (int pt = 0; pt < ovpSettings.lineList[poly].poly.Length - 1; pt++)
                    {
                        GL.Vertex3(ovpSettings.lineList[poly].poly[pt].X, ovpSettings.lineList[poly].poly[pt].Y, polyZ);
                        GL.Vertex3(ovpSettings.lineList[poly].poly[pt + 1].X, ovpSettings.lineList[poly].poly[pt + 1].Y, polyZ);
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
			MakeCurrent();
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
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.PolygonOffset(0.0f, 0.5f);
			GL.LineStipple(1, 61680);
			gridZ = -0.95f;
			axisZ = gridZ + 0.01f;
		}

		public TestViewport(ref OVPSettings svpSettings)
		{
			try
			{
				immediateMode = svpSettings.immediateMode;
				ovpSettings = svpSettings;
				MouseDown += downHandler;
				MouseMove += dragHandler;
				MouseUp += upHandler;
				MouseWheel += zoomHandler;
				GotFocus += addKeyHandler;
				// MouseHover += addKeyHandler;
				LostFocus += removeKeyHandler;
				ok = true;
			}
			catch(Exception)
			{
				//Console.WriteLine($"Error: {ex}");
				ok = false;
			}
		}

        public void setContextMenu(ref ContextMenu menu_)
        {
            menu = menu_;
        }

		public void changeSettingsRef(ref OVPSettings newSettings)
		{
			ovpSettings = newSettings;
			updateViewport();
		}

		protected override void OnDraw(EventArgs e)
		{
			base.OnDraw(e);
			updateViewport();
		}

		public void init()
		{
			MakeCurrent();
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(ovpSettings.cameraPosition.X - ((float)Width) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) / 2,
					  ovpSettings.cameraPosition.X + ((float)Width) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) / 2,
					  ovpSettings.cameraPosition.Y - ((float)Height) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) / 2,
					  ovpSettings.cameraPosition.Y + ((float)Height) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) / 2,
					  -1.0f, 1.0f);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			ovpSettings.bounds = getViewPort();
			GL.ClearColor(ovpSettings.backColor.R, ovpSettings.backColor.G, ovpSettings.backColor.B, ovpSettings.backColor.A);
			GL.ClearDepth(1.0);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}

		public void drawGrid_VBO()
		{
			if (ovpSettings.showGrid)
			{
				float spacing = ovpSettings.gridSpacing;
				if (ovpSettings.dynamicGrid)
				{
					while(WorldToScreen(new SizeF(spacing, 0.0f)).Width > 12.0f)
						spacing /= 10.0f;

					while(WorldToScreen(new SizeF(spacing, 0.0f)).Width < 4.0f)
						spacing *= 10.0f;
				}

				List<Vector3> grid = new List<Vector3>();
				List<Vector3> gridColors = new List<Vector3>();

				if (WorldToScreen(new SizeF(spacing, 0.0f)).Width >= 4.0f)
				{
					int k = 0;
					for (float i = 0; i > - (Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.X; i -= spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Height, gridZ));
						gridColors.Add(new Vector3(r, g, b));
						grid.Add(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Height, gridZ));
						gridColors.Add(new Vector3(r, g, b));
					}
					k = 0;
					for (float i = 0; i <(Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.X; i += spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Height, gridZ));
						gridColors.Add(new Vector3(r, g, b));
						grid.Add(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Height, gridZ));
						gridColors.Add(new Vector3(r, g, b));
					}
					k = 0;
					for (float i = 0; i > - (Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.Y; i -= spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Width, i, gridZ));
						gridColors.Add(new Vector3(r, g, b));
						grid.Add(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Width, i, gridZ));
						gridColors.Add(new Vector3(r, g, b));
					}
					k = 0;
					for (float i = 0; i <(Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.Y; i += spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Width, i, gridZ));
						gridColors.Add(new Vector3(r, g, b));
						grid.Add(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Width, i, gridZ));
						gridColors.Add(new Vector3(r, g, b));
					}
					gridArray = grid.ToArray();
					gridColorArray = gridColors.ToArray();
				}
			}
		}

		public void drawAxes_VBO()
		{
			if (ovpSettings.showAxes)
			{
				axesArray = new Vector3 [4];
				axesColorArray = new Vector3 [4];
				for (int i = 0; i < axesColorArray.Length; i++)
				{
					axesColorArray [i] = new Vector3(ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B);
				}
				axesArray [0] = new Vector3(0.0f, ovpSettings.cameraPosition.Y + Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom), axisZ);
				axesArray [1] = new Vector3(0.0f, ovpSettings.cameraPosition.Y -Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom), axisZ);
				axesArray [2] = new Vector3(ovpSettings.cameraPosition.X + Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom), 0.0f, axisZ);
				axesArray [3] = new Vector3(ovpSettings.cameraPosition.X -Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom), 0.0f, axisZ);
			}
		}

		public void drawGrid_immediate()
		{
			MakeCurrent();
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
					for (float i = 0; i > -(Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.X; i -= spacing)
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
						GL.Vertex3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Height, gridZ);
						GL.Vertex3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Height, gridZ);
					}
					GL.End();
					k = 0;
					GL.Begin(PrimitiveType.Lines);
					for (float i = 0; i < (Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.X; i += spacing)
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
						GL.Vertex3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Height, gridZ);
						GL.Vertex3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Height, gridZ);
					}
					GL.End();
					k = 0;
					GL.Begin(PrimitiveType.Lines);
					for (float i = 0; i > -(Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.Y; i -= spacing)
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
						GL.Vertex3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Width, i, gridZ);
						GL.Vertex3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Width, i, gridZ);
					}
					GL.End();
					k = 0;
					GL.Begin(PrimitiveType.Lines);
					for (float i = 0; i < (Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.Y; i += spacing)
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
						GL.Vertex3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Width, i, gridZ);
						GL.Vertex3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Width, i, gridZ);
					}
					GL.End();
				}
			}
		}

		public void drawAxes_immediate()
		{
			MakeCurrent();
			GL.LoadIdentity();
			if (ovpSettings.showAxes)
			{
				GL.Color4(ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B, ovpSettings.axisColor.A);
				GL.Begin(PrimitiveType.Lines);
				GL.Vertex3(0.0f, ovpSettings.cameraPosition.Y + Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom), axisZ);
				GL.Vertex3(0.0f, ovpSettings.cameraPosition.Y - Height * (ovpSettings.zoomFactor * ovpSettings.base_zoom), axisZ);
				GL.Vertex3(ovpSettings.cameraPosition.X + Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom), 0.0f, axisZ);
				GL.Vertex3(ovpSettings.cameraPosition.X - Width * (ovpSettings.zoomFactor * ovpSettings.base_zoom), 0.0f, axisZ);
				GL.End();
			}
		}
	}
}

