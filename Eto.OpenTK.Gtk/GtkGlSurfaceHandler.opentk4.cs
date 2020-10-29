#if OPENTK4
using Eto.Drawing;
using Eto.GtkSharp.Forms;
using OpenTK.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Eto.OpenTK;
using Gtk;
using System;
using System.Runtime.InteropServices;

[assembly: Eto.ExportHandler(typeof(GLSurface), typeof(Eto.OpenTK.Gtk.GtkGlSurfaceHandler))]

namespace Eto.OpenTK.Gtk
{
	public class GtkGlSurfaceHandler : GtkControl<GLArea, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
	{
        class GtkBindingsContext : global::OpenTK.IBindingsContext
        {
			[DllImport("GL")]
			static public extern IntPtr glXGetProcAddress(string name);

            public IntPtr GetProcAddress(string procName)
            {
				return glXGetProcAddress(procName);
            }
        }

        static GtkGlSurfaceHandler()
        {
            GL.LoadBindings(new GtkBindingsContext());
        }

		public void Create()
		{
			Control = new GLArea();
			Control.CanFocus = true;
			Control.HasDepthBuffer = true;
			Control.HasStencilBuffer = true;
			
			Control.Realized += Control_Realized;
			Control.CreateContext += Control_CreateContext;
		}
		Gdk.GLContext context;

		private void Control_CreateContext(object o, CreateContextArgs args)
		{
			if (context == null)
			{
				// shared context
				context = Control.Window.CreateGlContext();
			}
			args.RetVal = context;
		}

		private void Control_Realized(object sender, EventArgs e)
		{
			Control.Context.MakeCurrent();
			Callback.OnInitialized(Widget, EventArgs.Empty);
			IsInitialized = true;
			Control.Render += Control_Render;
		}

		private void Control_Render(object o, RenderArgs args)
		{
			Callback.OnDraw(Widget, EventArgs.Empty);
		}

		public void MakeCurrent() => Control.MakeCurrent();

		public void SwapBuffers()
		{
			// nothing to do here
			Control.QueueRender();
		}

		public bool IsInitialized { get; private set; }

	}

}
#endif