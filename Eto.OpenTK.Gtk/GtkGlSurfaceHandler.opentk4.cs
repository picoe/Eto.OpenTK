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
			Control.AutoRender = false;
			
			Control.Realized += Control_Realized;
			Control.CreateContext += Control_CreateContext;
			Control.Resize += Control_Resize;
		}

		private void Control_Resize(object o, ResizeArgs args)
		{
			skipDraw = false;
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
			MakeCurrent();
			Callback.OnInitialized(Widget, EventArgs.Empty);
			IsInitialized = true;
			Control.Render += Control_Render;
		}

		bool skipDraw;

		private void Control_Render(object o, RenderArgs args)
		{
			if (!skipDraw)
			{
				skipDraw = true; // ensure we don't queue another render if we call SwapBuffers below..
				Callback.OnDraw(Widget, EventArgs.Empty);
			}

			skipDraw = false;
		}

		public void MakeCurrent() => Control.MakeCurrent();

		public void SwapBuffers()
		{
			// GLArea doesn't support drawing directly, so we queue a render but don't actually call OnDraw
			if (skipDraw)
				return;
				
			skipDraw = true;
			Control.QueueRender();
		}

		public bool IsInitialized { get; private set; }

		void Eto.Forms.Control.IHandler.Invalidate(Rectangle rect, bool invalidateChildren)
		{
			skipDraw = false;
			Control.QueueRender();
		}

		void Eto.Forms.Control.IHandler.Invalidate(bool invalidateChildren)
		{
			skipDraw = false;
			Control.QueueRender();
		}
	}

}
#endif