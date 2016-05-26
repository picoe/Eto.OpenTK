using System;
using System.Diagnostics;
using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Windows;
using OpenTK;

namespace etoViewport_demo_win
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
            var gen = new Eto.WinForms.Platform();

            //gen.Add<GLSurface.IHandler>(() => new MacGLSurfaceHandler());
            gen.Add<GLSurface.IHandler>(() => new WinGLSurfaceHandler());

            new Application(gen).Run(new MainForm());
            // run application with our main form
            // new Application().Run(new MainForm());
		}
	}
}
