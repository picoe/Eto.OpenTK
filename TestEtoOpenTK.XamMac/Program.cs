using System;
using Eto.Forms;
using Eto.Drawing;
using Eto.OpenTK;
using Eto.OpenTK.Mac;
using OpenTK;

namespace TestEtoGl.XamMac
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var gen = new Eto.Mac.Platform();

			gen.Add<GLSurface.IHandler> (() => new MacGLSurfaceHandler ());

			// run application with our main form
			new Application(gen).Run(new MainForm());
		}
	}
}
