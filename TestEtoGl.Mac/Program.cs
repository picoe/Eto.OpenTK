using System;
using Eto.Forms;
using Eto.Drawing;
using Eto.Gl;
using Eto.Gl.Mac;

namespace TestEtoGl.Mac
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var gen = new Eto.Mac.Platform();

            // shouldn't be needed, Eto needs fixing
			gen.Add<GLSurface.IHandler>(() => new MacGLSurfaceHandler());

			// run application with our main form
			new Application(gen).Run(new MainForm());
		}
	}
}
