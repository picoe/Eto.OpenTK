using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Mac;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;

using System;
namespace Mac_GUI_testing_XS
{
	public class etoViewport : Panel
	{
		public bool ok;
		Vector3 [] polyArray;
		Vector3 [] polyColorArray;
		int poly_vbo_size;

		Vector3 [] gridArray;
		Vector3 [] gridColorArray;
		int grid_vbo_size;

		Vector3 [] axesArray;
		Vector3 [] axesColorArray;
		int axes_vbo_size;

		bool loaded = false;
		private GLSurface glControl;
		private OVPSettings ovpSettings;
		private float axisZ;
		private float gridZ;

		// Use for drag handling.
		bool dragging;
		private float x_orig;
		private float y_orig;

		private Point WorldToScreen (float x, float y)
		{
			return new Point ((int)((x - ovpSettings.cameraPosition.X / ovpSettings.zoomFactor) + glControl.Width / 2),
					 (int)((y - ovpSettings.cameraPosition.Y / ovpSettings.zoomFactor) + glControl.Height / 2));
		}

		private Point WorldToScreen (PointF pt)
		{
			return WorldToScreen (pt.X, pt.Y);
		}

		private Size WorldToScreen (SizeF pt)
		{
			Point pt1 = WorldToScreen (0, 0);
			Point pt2 = WorldToScreen (pt.Width, pt.Height);
			return new Size (pt2.X - pt1.X, pt2.Y - pt1.Y);
		}

		private PointF ScreenToWorld (int x, int y)
		{
			return new PointF ((float)(x - glControl.Width / 2) * ovpSettings.zoomFactor + ovpSettings.cameraPosition.X,
					  (float)(y - glControl.Height / 2) * ovpSettings.zoomFactor + ovpSettings.cameraPosition.Y);
		}

		private PointF ScreenToWorld (Point pt)
		{
			return ScreenToWorld (pt.X, pt.Y);
		}

		private RectangleF getViewPort ()
		{
			glControl.MakeCurrent ();
			PointF bl = ScreenToWorld (glControl.Location.X - glControl.Width / 2, glControl.Location.Y - glControl.Height / 2);
			PointF tr = ScreenToWorld (glControl.Location.X + glControl.Width / 2, glControl.Location.Y + glControl.Height / 2);
			return new RectangleF (bl.X, bl.Y, tr.X - bl.X, tr.Y - bl.Y);
		}

		private void setViewPort (float x1, float y1, float x2, float y2)
		{
			glControl.MakeCurrent ();
			float h = Math.Abs (y1 - y2);
			float w = Math.Abs (x1 - x2);
			ovpSettings.cameraPosition = new PointF ((x1 + x2) / 2, (y1 + y2) / 2);
			if ((glControl.Height != 0) && (glControl.Width != 0))
				ovpSettings.zoomFactor = Math.Max (h / (float)(glControl.Height), w / (float)(glControl.Width));
			else
				ovpSettings.zoomFactor = 1;
		}

		private void downHandler (object sender, MouseEventArgs e)
		{
			if (e.Buttons == MouseButtons.Primary) {
				if (!dragging) // might not be needed, but seemed like a safe approach to avoid re-setting these in a drag event.
				{
					x_orig = e.Location.X;
					y_orig = e.Location.Y;
					dragging = true;
				}
			}
		}

		private void dragHandler (object sender, MouseEventArgs e)
		{
			glControl.MakeCurrent ();
			if (e.Buttons == MouseButtons.Primary) {
				object locking = new object ();
				lock (locking) {
					// Scaling factor is arbitrary - just based on testing to avoid insane panning speeds.
					float new_X = (ovpSettings.cameraPosition.X - (((float)e.Location.X - x_orig) / 100.0f));
					float new_Y = (ovpSettings.cameraPosition.Y + (((float)e.Location.Y - y_orig) / 100.0f));
					ovpSettings.cameraPosition = new PointF (new_X, new_Y);
				}
			}
			updateViewport ();
		}

		private void upHandler (object sender, MouseEventArgs e)
		{
			if (e.Buttons == MouseButtons.Primary) {
				dragging = false;
			}
		}

		public void viewport_load ()
		{
			loaded = true;
		}

		private void zoomIn ()
		{
			glControl.MakeCurrent ();
			ovpSettings.zoomFactor += (ovpSettings.zoomStep * 0.01f);
		}

		private void zoomOut ()
		{
			ovpSettings.zoomFactor -= (ovpSettings.zoomStep * 0.01f);
			if (ovpSettings.zoomFactor < 0.0001) {
				ovpSettings.zoomFactor = 0.0001f; // avoid any chance of getting to zero.
			}
		}

		private void panVertical (float delta)
		{
			glControl.MakeCurrent ();
			ovpSettings.cameraPosition.Y += delta / 10;
		}

		private void panHorizontal (float delta)
		{
			glControl.MakeCurrent ();
			ovpSettings.cameraPosition.X += delta / 10;
		}

		private void addKeyHandler (object sender, EventArgs e)
		{
			glControl.MakeCurrent ();
			glControl.KeyDown += keyHandler;
		}

		private void removeKeyHandler (object sender, EventArgs e)
		{
			glControl.MakeCurrent ();
			glControl.KeyDown -= keyHandler;
		}

		private void keyHandler (object sender, KeyEventArgs e)
		{
			glControl.MakeCurrent ();
			if (e.Key == Keys.R) {
				ovpSettings.cameraPosition = new PointF (0, 0);
				ovpSettings.zoomFactor = 1.0f;
			}

			float stepping = 10.0f * ovpSettings.zoomFactor;

			if (e.Key == Keys.A) {
				panHorizontal (-stepping);
			}
			if (e.Key == Keys.D) {
				panHorizontal (stepping);
			}
			if (e.Key == Keys.W) {
				panVertical (stepping);
			}
			if (e.Key == Keys.S) {
				panVertical (-stepping);
			}
			updateViewport ();
		}

		private void zoomHandler (object sender, MouseEventArgs e)
		{
			glControl.MakeCurrent ();
			float wheelZoom = e.Delta.Height / 3.0f; // SystemInformation.MouseWheelScrollLines;
			if (wheelZoom > 0) {
				zoomIn ();
			}
			if (wheelZoom < 0) {
				zoomOut ();
			}
			updateViewport ();
		}

		public void updateViewport ()
		{
			try {
				glControl.MakeCurrent ();
				init ();
				drawGrid ();
				drawAxes ();
				drawPolygons ();

				// Fix in case of nulls
				if (polyArray == null) {
					polyArray = new Vector3 [2];
					polyColorArray = new Vector3 [polyArray.Length];
					for (int i = 0; i < polyArray.Length; i++) {
						polyArray [i] = new Vector3 (0.0f);
						polyColorArray [i] = new Vector3 (1.0f);
					}
				}

				// Now we wrangle our VBOs
				grid_vbo_size = gridArray.Length; // Necessary for rendering later on
				axes_vbo_size = axesArray.Length; // Necessary for rendering later on
				poly_vbo_size = polyArray.Length; // Necessary for rendering later on

				int [] vbo_id = new int [3]; // three buffers to be applied
				GL.GenBuffers (3, vbo_id);

				int [] col_id = new int [3];
				GL.GenBuffers (3, col_id);

				GL.BindBuffer (BufferTarget.ArrayBuffer, vbo_id [0]);
				GL.BufferData (BufferTarget.ArrayBuffer,
					      new IntPtr (gridArray.Length * BlittableValueType.StrideOf (gridArray)),
					      gridArray, BufferUsageHint.StaticDraw);

				GL.BindBuffer (BufferTarget.ArrayBuffer, col_id [0]);
				GL.BufferData (BufferTarget.ArrayBuffer,
					      new IntPtr (gridColorArray.Length * BlittableValueType.StrideOf (gridColorArray)),
					      gridColorArray, BufferUsageHint.StaticDraw);

				GL.BindBuffer (BufferTarget.ArrayBuffer, vbo_id [1]);
				GL.BufferData (BufferTarget.ArrayBuffer,
					      new IntPtr (axesArray.Length * BlittableValueType.StrideOf (axesArray)),
					      axesArray, BufferUsageHint.StaticDraw);

				GL.BindBuffer (BufferTarget.ArrayBuffer, col_id [1]);
				GL.BufferData (BufferTarget.ArrayBuffer,
					      new IntPtr (axesColorArray.Length * BlittableValueType.StrideOf (axesColorArray)),
					      axesColorArray, BufferUsageHint.StaticDraw);

				GL.BindBuffer (BufferTarget.ArrayBuffer, vbo_id [2]);
				GL.BufferData (BufferTarget.ArrayBuffer,
					      new IntPtr (polyArray.Length * BlittableValueType.StrideOf (polyArray)),
					      polyArray, BufferUsageHint.StaticDraw);

				GL.BindBuffer (BufferTarget.ArrayBuffer, col_id [2]);
				GL.BufferData (BufferTarget.ArrayBuffer,
					      new IntPtr (polyColorArray.Length * BlittableValueType.StrideOf (polyColorArray)),
					      polyColorArray, BufferUsageHint.StaticDraw);

				// To draw a VBO:
				// 1) Ensure that the VertexArray client state is enabled.
				// 2) Bind the vertex and element buffer handles.
				// 3) Set up the data pointers (vertex, normal, color) according to your vertex format.


				try {
					GL.EnableClientState (EnableCap.VertexArray);
					GL.EnableClientState (EnableCap.ColorArray);
					GL.BindBuffer (BufferTarget.ArrayBuffer, vbo_id [0]);
					GL.VertexPointer (3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr (0));
					GL.BindBuffer (BufferTarget.ArrayBuffer, col_id [0]);
					GL.ColorPointer (3, ColorPointerType.Float, Vector3.SizeInBytes, new IntPtr (0));
					GL.DrawArrays (BeginMode.Lines, 0, grid_vbo_size);
					GL.DisableClientState (EnableCap.VertexArray);
					GL.DisableClientState (EnableCap.ColorArray);

					GL.EnableClientState (EnableCap.VertexArray);
					GL.EnableClientState (EnableCap.ColorArray);
					GL.BindBuffer (BufferTarget.ArrayBuffer, vbo_id [1]);
					GL.VertexPointer (3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr (0));
					GL.BindBuffer (BufferTarget.ArrayBuffer, col_id [1]);
					GL.ColorPointer (3, ColorPointerType.Float, Vector3.SizeInBytes, new IntPtr (0));
					GL.DrawArrays (BeginMode.Lines, 0, axes_vbo_size);
					GL.DisableClientState (EnableCap.VertexArray);
					GL.DisableClientState (EnableCap.ColorArray);

					GL.EnableClientState (EnableCap.VertexArray);
					GL.EnableClientState (EnableCap.ColorArray);
					GL.BindBuffer (BufferTarget.ArrayBuffer, vbo_id [2]);
					GL.VertexPointer (3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr (0));
					GL.BindBuffer (BufferTarget.ArrayBuffer, col_id [2]);
					GL.ColorPointer (3, ColorPointerType.Float, Vector3.SizeInBytes, new IntPtr (0));
					GL.DrawArrays (BeginMode.Lines, 0, poly_vbo_size);
					GL.DisableClientState (EnableCap.VertexArray);
					GL.DisableClientState (EnableCap.ColorArray);
				} catch (Exception) {

				}

				glControl.SwapBuffers ();
				GL.Flush ();
				GL.DeleteBuffers (3, vbo_id);
				GL.DeleteBuffers (3, col_id);
			} catch (Exception) {
				ok = false;
			}
		}

		private void drawPolygons ()
		{
			try {
				List<Vector3> polyList = new List<Vector3> ();
				List<Vector3> polyColorList = new List<Vector3> ();
				float polyZStep = 1.0f / ovpSettings.polyList.Count ();
				for (int poly = 0; poly < ovpSettings.polyList.Count (); poly++) {
					float polyZ = poly * polyZStep;
					for (int pt = 0; pt < ovpSettings.polyList [poly].poly.Length - 1; pt++) {
						polyList.Add (new Vector3 (ovpSettings.polyList [poly].poly [pt].X, ovpSettings.polyList [poly].poly [pt].Y, polyZ));
						polyColorList.Add (new Vector3 (ovpSettings.polyList [poly].color.R, ovpSettings.polyList [poly].color.G, ovpSettings.polyList [poly].color.B));
						polyList.Add (new Vector3 (ovpSettings.polyList [poly].poly [pt + 1].X, ovpSettings.polyList [poly].poly [pt + 1].Y, polyZ));
						polyColorList.Add (new Vector3 (ovpSettings.polyList [poly].color.R, ovpSettings.polyList [poly].color.G, ovpSettings.polyList [poly].color.B));
					}
				}

				polyArray = polyList.ToArray ();
				polyColorArray = polyColorList.ToArray ();
			} catch (Exception) {
				// Can ignore - not critical.
			}
		}

		public void defaults ()
		{
			glControl.MakeCurrent ();
			if (ovpSettings.antiAlias) {
				GL.Enable (EnableCap.LineSmooth);
			} else {
				GL.Disable (EnableCap.LineSmooth);
			}
			GL.Disable (EnableCap.Lighting);
			GL.ShadeModel (ShadingModel.Flat);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.PolygonOffset (0.0f, 0.5f);
			GL.LineStipple (1, 61680);
			gridZ = -0.95f;
			axisZ = gridZ + 0.01f;
		}

		public void updateVP (object sender, EventArgs e)
		{
			updateViewport ();
		}

		public etoViewport (GLSurface viewportControl, OVPSettings svpSettings)
		{
			try {
                Size = viewportControl.Size;
                Content = viewportControl;
				viewportControl.MakeCurrent ();
				glControl = viewportControl;
				ovpSettings = svpSettings;
				glControl.MouseDown += downHandler;
				glControl.MouseMove += dragHandler;
				glControl.MouseUp += upHandler;
				glControl.MouseWheel += zoomHandler;
				glControl.MouseEnter += addKeyHandler;
				// glControl.MouseHover += addKeyHandler;
				glControl.MouseLeave += removeKeyHandler;
				defaults ();
				GL.Viewport (0, 0, glControl.Width, glControl.Height);
				Content = glControl;
				ok = true;
			} catch (Exception) {
				ok = false;
			}
		}

		public void init ()
		{
			glControl.MakeCurrent ();
			GL.MatrixMode (MatrixMode.Projection);
			GL.LoadIdentity ();
			GL.Ortho (ovpSettings.cameraPosition.X - ((float)glControl.Width) * ovpSettings.zoomFactor / 2,
				      ovpSettings.cameraPosition.X + ((float)glControl.Width) * ovpSettings.zoomFactor / 2,
				      ovpSettings.cameraPosition.Y - ((float)glControl.Height) * ovpSettings.zoomFactor / 2,
				      ovpSettings.cameraPosition.Y + ((float)glControl.Height) * ovpSettings.zoomFactor / 2,
				      -1.0f, 1.0f);
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadIdentity ();
			ovpSettings.bounds = getViewPort ();
			GL.ClearColor (ovpSettings.backColor.R, ovpSettings.backColor.G, ovpSettings.backColor.B, ovpSettings.backColor.A);
			GL.ClearDepth (1.0);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}

		public void drawGrid ()
		{
			if (ovpSettings.showGrid) {
				float spacing = ovpSettings.gridSpacing;
				if (ovpSettings.dynamicGrid) {
					while (WorldToScreen (new SizeF (spacing, 0.0f)).Width > 12.0f)
						spacing /= 10.0f;

					while (WorldToScreen (new SizeF (spacing, 0.0f)).Width < 4.0f)
						spacing *= 10.0f;
				}

				List<Vector3> grid = new List<Vector3> ();
				List<Vector3> gridColors = new List<Vector3> ();

				if (WorldToScreen (new SizeF (spacing, 0.0f)).Width >= 4.0f) {
					int k = 0;
					for (float i = 0; i > -(Width + ovpSettings.cameraPosition.X) * ovpSettings.zoomFactor; i -= spacing) {
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9) {
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10) {
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add (new Vector3 (i, ovpSettings.zoomFactor * Height, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
						grid.Add (new Vector3 (i, ovpSettings.zoomFactor * -Height, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
					}
					k = 0;
					for (float i = 0; i < (Width + ovpSettings.cameraPosition.X) * ovpSettings.zoomFactor; i += spacing) {
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9) {
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10) {
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add (new Vector3 (i, ovpSettings.zoomFactor * Height, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
						grid.Add (new Vector3 (i, ovpSettings.zoomFactor * -Height, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
					}
					k = 0;
					for (float i = 0; i > -(Height + ovpSettings.cameraPosition.Y) * ovpSettings.zoomFactor; i -= spacing) {
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9) {
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10) {
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add (new Vector3 (ovpSettings.zoomFactor * Width, i, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
						grid.Add (new Vector3 (ovpSettings.zoomFactor * -Width, i, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
					}
					k = 0;
					for (float i = 0; i < (Height + ovpSettings.cameraPosition.Y) * ovpSettings.zoomFactor; i += spacing) {
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9) {
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10) {
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add (new Vector3 (ovpSettings.zoomFactor * Width, i, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
						grid.Add (new Vector3 (ovpSettings.zoomFactor * -Width, i, gridZ));
						gridColors.Add (new Vector3 (r, g, b));
					}
					gridArray = grid.ToArray ();
					gridColorArray = gridColors.ToArray ();
				}
			}
		}

		public void drawAxes ()
		{
			if (ovpSettings.showAxes) {
				axesArray = new Vector3 [4];
				axesColorArray = new Vector3 [4];
				for (int i = 0; i < axesColorArray.Length; i++) {
					axesColorArray [i] = new Vector3 (ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B);
				}
				axesArray [0] = new Vector3 (0.0f, Height * ovpSettings.zoomFactor, axisZ);
				axesArray [1] = new Vector3 (0.0f, -Height * ovpSettings.zoomFactor, axisZ);
				axesArray [2] = new Vector3 (Width * ovpSettings.zoomFactor, 0.0f, axisZ);
				axesArray [3] = new Vector3 (-Width * ovpSettings.zoomFactor, 0.0f, axisZ);
			}
		}
	}

	/*
	public class etoViewport : Panel
	{
		public class EmptyPlaceHolder : Drawable
		{
			public EmptyPlaceHolder ()
			{
				//this.background = Resources.NoMap;
				this.font = new Font (FontFamilies.Monospace, 12);
			}

			private Font font;

			protected override void OnPaint (PaintEventArgs e)
			{
				base.OnPaint (e);
				var gpx = e.Graphics;

				/ *
				for (int y = 0; y < this.ClientSize.Height; y += background.Height) {
					for (int x = 0; x < this.ClientSize.Width; x += background.Width) {
						gpx.DrawImage (background, x, y);
					}
				}
				* /

				gpx.DrawText (font,
					Eto.Drawing.Colors.White,
					this.ClientSize.Width / 2,
					this.ClientSize.Height / 2,
					"No Map");
			}
		}

		//[EventPublication (EventTopics.OnTextureDrawLater)]
		public event EventHandler OnTextureDrawLater;

		public event EventHandler OnSaveFMap;

		public GLSurface GLSurface { get; set; }

		//[EventSubscription (EventTopics.OnMapLoad, typeof (OnPublisher))]
		public void HandleMapLoad ()
		{
			this.panel.Content = this.GLSurface;

			//this.ViewInfo.SetGlSize (this.GLSurface.Size);

			SetViewPort ();
			DrawLater ();
		}


		private UITimer timDraw;

		private UITimer timKey;

		private UITimer timTool;

		protected Label lblTile;

		protected Label lblVertex;

		protected Label lblPos;

		private bool drawPending = false;

		private EmptyPlaceHolder emptyPlaceHolder;

		protected Panel panel;


		public etoViewport (GLSurface control)
		{
			emptyPlaceHolder = new EmptyPlaceHolder ();
			// this.panel = new Panel ();

			this.GLSurface = control;
			//this.Content = this.emptyPlaceHolder;
			this.Content = this.GLSurface;

			SetupEventHandlers ();
			SetupBindings ();
		}

		protected void MapPanelTool_Selection (object sender, EventArgs e)
		{
		}

		protected void MapPanelTool_SelectionCopy (object sender, EventArgs e)
		{
		}

		protected void MapPanelTool_SelectionPaste (object sender, EventArgs e)
		{
		}

		protected void MapPanelTool_SelectionPasteOptions (object sender, EventArgs e)
		{
		}

		protected void MapPanelTool_SelectionRotateAntiClockwise (object sender, EventArgs e)
		{
		}
		protected void MapPanelTool_SelectionRotateClockwise (object sender, EventArgs e)
		{
		}
		protected void MapPanelTool_SelectionFlipX (object sender, EventArgs e)
		{
		}
		protected void MapPanelTool_ObjectsSelect (object sender, EventArgs e)
		{
		}
		protected void MapPanelTool_Gateways (object sender, EventArgs e)
		{
		}
		protected void MapPanelTool_DrawAutoTexture (object sender, EventArgs e)
		{
		}

		protected void MapPanelTool_DrawTileOrentation (object sender, EventArgs e)
		{
		}

		protected void MapPanelTool_Save (object sender, EventArgs e)
		{
		}

		protected void minimapOptions_Click (object sender, EventArgs e)
		{
		}

		protected override void OnPreLoad (EventArgs e)
		{
			this.ParentWindow.GotFocus += ParentWindow_GotFocus;
			base.OnPreLoad (e);
		}

		void ParentWindow_GotFocus (object sender, EventArgs e)
		{
			this.DrawLater ();
		}

		private void SetupBindings ()
		{
		}
		private void SetupEventHandlers ()
		{
		}

		private void MakeGlFont ()
		{
			if (!this.GLSurface.IsInitialized) {
				return;
			}
			this.GLSurface.MakeCurrent ();
		}

		protected override void OnLoadComplete (EventArgs lcEventArgs)
		{
			base.OnLoadComplete (lcEventArgs);

			//GLSurface.KeyDown += KeyboardManager.HandleKeyDown;
			//GLSurface.KeyUp += KeyboardManager.HandleKeyUp;

			GLSurface.MouseEnter += (sender, e) => {
				GLSurface.Focus ();
			};
			/ *
			GLSurface.MouseDown += (sender, args) => {
				//make sure this manager sees the mouse event first
				//to get modifers such as CTRL/ALT/SHIFT and activate
				//any keys necessary before ViewInfo finds out
				//and queries if CTRL/ALT/SHIFT is active.
				KeyboardManager.HandleMouseDown (sender, args);
				ViewInfo.HandleMouseDown (sender, args);
			};
			GLSurface.MouseUp += (sender, args) => {
				ViewInfo.HandleMouseUp (sender, args);
			};* /
			//GLSurface.MouseMove += ViewInfo.HandleMouseMove;
			GLSurface.MouseMove += HandleMouseMove;
			/ *GLSurface.MouseWheel += ViewInfo.HandleMouseWheel;

			GLSurface.LostFocus += ViewInfo.HandleLostFocus;
			GLSurface.MouseLeave += ViewInfo.HandleMouseLeave;

			GLSurface.GLInitalized += InitalizeGlSurface;
			* /
			GLSurface.SizeChanged += ResizeMapView;

			//KeyboardManager.KeyDown += HandleKeyDown;
		}


		private void InitalizeGlSurface (object sender, EventArgs e)
		{
			this.GLSurface.MakeCurrent ();

			GL.PixelStore (PixelStoreParameter.PackAlignment, 1);
			GL.PixelStore (PixelStoreParameter.UnpackAlignment, 1);
			//GL.ClearColor(0.0F, 0.0F, 0.0F, 1.0F);
			GL.ClearColor (OpenTK.Graphics.Color4.CornflowerBlue);
			GL.Clear (ClearBufferMask.ColorBufferBit);
			GL.ShadeModel (ShadingModel.Smooth);
			GL.Hint (HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.Enable (EnableCap.DepthTest);

			var ambient = new float [4];
			var specular = new float [4];
			var diffuse = new float [4];

			ambient [0] = 0.333333343F;
			ambient [1] = 0.333333343F;
			ambient [2] = 0.333333343F;
			ambient [3] = 1.0F;
			specular [0] = 0.6666667F;
			specular [1] = 0.6666667F;
			specular [2] = 0.6666667F;
			specular [3] = 1.0F;
			diffuse [0] = 0.75F;
			diffuse [1] = 0.75F;
			diffuse [2] = 0.75F;
			diffuse [3] = 1.0F;
			GL.Light (LightName.Light0, LightParameter.Diffuse, diffuse);
			GL.Light (LightName.Light0, LightParameter.Specular, specular);
			GL.Light (LightName.Light0, LightParameter.Ambient, ambient);

			ambient [0] = 0.25F;
			ambient [1] = 0.25F;
			ambient [2] = 0.25F;
			ambient [3] = 1.0F;
			specular [0] = 0.5F;
			specular [1] = 0.5F;
			specular [2] = 0.5F;
			specular [3] = 1.0F;
			diffuse [0] = 0.5625F;
			diffuse [1] = 0.5625F;
			diffuse [2] = 0.5625F;
			diffuse [3] = 1.0F;
			GL.Light (LightName.Light1, LightParameter.Diffuse, diffuse);
			GL.Light (LightName.Light1, LightParameter.Specular, specular);
			GL.Light (LightName.Light1, LightParameter.Ambient, ambient);

			var matDiffuse = new float [4];
			var matSpecular = new float [4];
			var matAmbient = new float [4];
			var matShininess = new float [1];

			matSpecular [0] = 0.0F;
			matSpecular [1] = 0.0F;
			matSpecular [2] = 0.0F;
			matSpecular [3] = 0.0F;
			matAmbient [0] = 1.0F;
			matAmbient [1] = 1.0F;
			matAmbient [2] = 1.0F;
			matAmbient [3] = 1.0F;
			matDiffuse [0] = 1.0F;
			matDiffuse [1] = 1.0F;
			matDiffuse [2] = 1.0F;
			matDiffuse [3] = 1.0F;
			matShininess [0] = 0.0F;

			GL.Material (MaterialFace.FrontAndBack, MaterialParameter.Ambient, matAmbient);
			GL.Material (MaterialFace.FrontAndBack, MaterialParameter.Specular, matSpecular);
			GL.Material (MaterialFace.FrontAndBack, MaterialParameter.Diffuse, matDiffuse);
			GL.Material (MaterialFace.FrontAndBack, MaterialParameter.Shininess, matShininess);

			timDraw = new UITimer { Interval = 0.013 }; // Every Millisecond.
			timDraw.Elapsed += timDraw_Elapsed;
			timDraw.Start ();

			timKey = new UITimer { Interval = 0.030 }; // Every 30 milliseconds.
			timKey.Elapsed += timKey_Elapsed;
			timKey.Start ();

			timTool = new UITimer { Interval = 0.1 }; // Every 100 milliseconds.
			timTool.Elapsed += timTool_Elapsed;
			timTool.Start ();

			// Make the GL Font.
			MakeGlFont ();
			//SetViewPort();
			//DrawLater();
		}

		private void ResizeMapView (object sender, EventArgs e)
		{
			SetViewPort ();
			DrawLater ();
		}

		private void SetViewPort ()
		{
			if (!this.GLSurface.IsInitialized)
				return;

			this.GLSurface.MakeCurrent ();

			var glSize = GLSurface.Size;

			// send the resize event to the Graphics card.
			GL.Viewport (0, 0, glSize.Width, glSize.Height);
			GL.Clear (ClearBufferMask.ColorBufferBit);
			GL.Flush ();

			this.GLSurface.SwapBuffers ();
		}

		private Eto.Drawing.Size GLSize {
			get { return this.GLSurface.Size; }
		}

		private void DrawPlaceHolder ()
		{
			if (!this.GLSurface.IsInitialized)
				return;

			this.Content = this.emptyPlaceHolder;
		}

		//[EventSubscription (EventTopics.OnMapDrawLater, typeof (OnPublisher))]
		public void HandleDrawLater (EventArgs e)
		{
			this.DrawLater ();
		}

		private void DrawLater ()
		{
			drawPending = true;
		}

		private void timDraw_Elapsed (object sender, EventArgs e)
		{
			if (!drawPending || !this.GLSurface.IsInitialized) {
				return;
			}

			this.GLSurface.MakeCurrent ();

			var bgColour = new Color ();
			bgColour.R = 0.5F;
			bgColour.G = 0.5F;
			bgColour.B = 0.5F;

			GL.ClearColor (bgColour.R, bgColour.G, bgColour.B, 1.0F);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			/ *
			if (mainMap != null) // Else just clear the screen.
			{
				try {
					MainMap.GLDraw (new DrawContext {
						GlSize = this.GLSurface.Size,
						MinimapGl = this.minimapGl,
						ViewInfo = this.ViewInfo,
						ToolOptions = this.ToolOptions
					});
				} catch (Exception ex) {
					Debugger.Break ();
					Logger.Error (ex, "Got an exception");
				}
			}
			* /
			GL.Flush ();
			this.GLSurface.SwapBuffers ();

			drawPending = false;
		}

		private void timTool_Elapsed (object sender, EventArgs e)
		{
			this.GLSurface.MakeCurrent ();
		}

		private void timKey_Elapsed (object sender, EventArgs e)
		{
			this.GLSurface.MakeCurrent ();

			double Rate = 0;
			double Zoom = 0;
			double Move = 0;
			double Roll = 0;
			double Pan = 0;
			double OrbitRate = 0;

			DrawLater ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				if (GLSurface.IsInitialized) {
					timDraw.Stop ();
					timDraw = null;

					timKey.Stop ();
					timKey = null;
				}
			}

			base.Dispose (disposing);
		}

		private void HandleMouseMove (object sender, MouseEventArgs e)
		{
		}

		private void HandleKeyDown (object sender, KeyEventArgs e)
		{
		}

		public void RefreshMinimap ()
		{
		}

		// [EventSubscription (EventTopics.OnMapUpdate, typeof (OnPublisher))]
		public void HandleMapUpdate (EventArgs e)
		{
			this.UpdateMap ();
		}

		private void UpdateMap ()
		{
		}
	}
	*/
}

