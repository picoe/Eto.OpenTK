using System;
using System.Diagnostics;
using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Gtk;
using OpenTK;

namespace etoViewport_demo_lin
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
            //check
            try
            {
                Toolkit.Init();
            }
            catch	
            {
                Debugger.Break();
            }
            var gen = new Eto.GtkSharp.Platform();

            //gen.Add<GLSurface.IHandler>(() => new MacGLSurfaceHandler());
            gen.Add<GLSurface.IHandler>(() => new GtkGlSurfaceHandler());

            new Application(gen).Run(new MainForm());
            // run application with our main form
            // new Application().Run(new MainForm());
		}
	}
}
