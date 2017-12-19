using Eto.Drawing;
using Eto.Forms;
using Eto.Wpf.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;

namespace Eto.Gl.WPF_WFControl
{
	public class WPFWFGLSurfaceHandler : WindowsFormsHostHandler<GLControl, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
	{
		public void CreateWithParams(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
		{
			WinFormsControl = new GLControl(mode, major, minor, flags);
			Control.Focusable = true;
		}

		protected override void Initialize()
		{
			base.Initialize();
			HandleEvent(GLSurface.GLDrawEvent);
		}

		public bool IsInitialized => Control.IsInitialized;

		public void MakeCurrent() => WinFormsControl.MakeCurrent();

		public void SwapBuffers() => WinFormsControl.SwapBuffers();

		public void UpdateWpfHandler(object sender, EventArgs e)
		{
			UpdateWpf();
		}

		public void UpdateWpf()
		{
			MakeCurrent();
			GL.Viewport(WinFormsControl.ClientSize);
			Callback.OnDraw(Widget, EventArgs.Empty);
			SwapBuffers();
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case GLSurface.GLInitializedEvent:
					Control.Initialized += (sender, e) => Callback.OnInitialized(Widget, EventArgs.Empty);
					break;

				case GLSurface.GLShuttingDownEvent:
					// Control.ShuttingDown += (sender, e) => Callback.OnShuttingDown(Widget, e);
					break;

				case GLSurface.ShownEvent:
				case GLSurface.SizeChangedEvent:
				case GLSurface.GLDrawEvent:
					WinFormsControl.Paint += UpdateWpfHandler;
					break;

				default:
					base.AttachEvent(id);
					break;
			}
		}
	}
}