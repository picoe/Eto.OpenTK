#if OPENTK3
using Eto.Drawing;
using Eto.GtkSharp.Forms;
using OpenTK.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Eto.OpenTK;

[assembly: Eto.ExportHandler(typeof(GLSurface), typeof(Eto.OpenTK.Gtk.GtkGlSurfaceHandler))]

namespace Eto.OpenTK.Gtk
{
	public class GtkGlSurfaceHandler : GtkControl<GLDrawingArea, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
	{
		static GtkGlSurfaceHandler()
		{
			GraphicsContext.ShareContexts = true;
		}

		protected override void Initialize()
		{
			base.Initialize();
			HandleEvent(GLSurface.GLDrawEvent);
		}

		public void Create()
		{
			CreateWithParams(GraphicsMode.Default, 1, 0, GraphicsContextFlags.Default);
		}

		public void CreateWithParams(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
		{
			Control = new GLDrawingArea(mode, major, minor, flags);
		}

		public bool IsInitialized
		{
			get { return Control.IsInitialized; }
		}

		public void MakeCurrent()
		{
			Control.MakeCurrent();
		}

		public void SwapBuffers()
		{
			Control.SwapBuffers();
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case GLSurface.GLInitializedEvent:
					Control.Initialized += (sender, args) => Callback.OnInitialized(this.Widget, args);
					break;

				case GLSurface.GLShuttingDownEvent:
					Control.ShuttingDown += (sender, args) => Callback.OnShuttingDown(this.Widget, args);
					break;

				case GLSurface.GLDrawEvent:
					Control.Draw += (sender, args) =>
					{
						if (Control.IsInitialized)
						{
							MakeCurrent();
							var size = Widget.Size * (Widget.ParentWindow?.Screen?.LogicalPixelSize ?? 1f);

							GL.Viewport(0, 0, (int)size.Width, (int)size.Height);
							Callback.OnDraw(this.Widget, args);
							SwapBuffers();
						}
					};
					break;

				default:
					base.AttachEvent(id);
					break;
			}
		}
	}

}
#endif