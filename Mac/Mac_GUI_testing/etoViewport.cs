using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Mac;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;

using System;
namespace Mac_GUI_testing
{
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

				/*
				for (int y = 0; y < this.ClientSize.Height; y += background.Height) {
					for (int x = 0; x < this.ClientSize.Width; x += background.Width) {
						gpx.DrawImage (background, x, y);
					}
				}
				*/

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
			/*
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
			};*/
			//GLSurface.MouseMove += ViewInfo.HandleMouseMove;
			GLSurface.MouseMove += HandleMouseMove;
			/*GLSurface.MouseWheel += ViewInfo.HandleMouseWheel;

			GLSurface.LostFocus += ViewInfo.HandleLostFocus;
			GLSurface.MouseLeave += ViewInfo.HandleMouseLeave;

			GLSurface.GLInitalized += InitalizeGlSurface;
			*/
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

			/*
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
			*/
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
}

