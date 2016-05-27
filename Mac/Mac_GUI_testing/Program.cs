using System;
using Eto.Forms;
using Eto.Drawing;
using Eto.Gl;
using Eto.Gl.Mac;
using OpenTK;

namespace Mac_GUI_testing
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			Toolkit.Init ();
			var gen = new Eto.Mac.Platform();

			gen.Add<GLSurface.IHandler> (() => new MacGLSurfaceHandler ());
			// run application with our main form
			new Application(gen).Run(new MainForm());
		}
	}

	/*
	public class MainForm : Form
	{
		public GLSurface glControl1;
		etoViewport oVP;

		public MainForm ()
		{
			this.ClientSize = new Size (1024, 768);

			var leftlayout = new DynamicLayout ();
			leftlayout.BackgroundColor = Color.FromArgb (0, 255, 0);

			var cmdImgButton = new Button () {
				Text = "test",
			};

			leftlayout.Add (cmdImgButton);

			Panel left = new Panel () {
				BackgroundColor = Color.FromArgb (255, 0, 0),
				Content = leftlayout
			};

			//var viewport = new etoViewport ();

			//var rightLayout = new DynamicLayout ();
			//rightLayout.BackgroundColor = Color.FromArgb (0, 255, 255);
			// rightLayout.Add (gl);

			/ *Panel right = new Panel () {
				BackgroundColor = Color.FromArgb (255, 255, 0),
				Content = rightLayout
			};* /

			glControl1 = new GLSurface ();
			glControl1.Size = new Size (200, 200);

			etoViewport viewport = new etoViewport (glControl1);//new Size(200,200));
			viewport.Size = new Size (200, 200);

			var splitter = new Splitter () {
				Position = 392,
				FixedPanel = SplitterFixedPanel.Panel1,
				Panel1 = left,
				Panel2 = viewport,
			};

			// bool status = gl.IsInitialized;

			//viewport.updateViewport ();

			this.Content = splitter;

		}
	}
	*/
}
