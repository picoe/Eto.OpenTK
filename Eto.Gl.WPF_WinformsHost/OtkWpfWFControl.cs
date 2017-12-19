using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Eto.Wpf;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OpenTK.Platform;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace Eto.Gl.WPF_WFControl
{


	public class OtkWpfWFControl : WindowsFormsHost
	{
		public GLControl glControl;

        public OtkWpfWFControl(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        {
			glControl = new GLControl();
            glControl.Dock = DockStyle.Fill;
			// XXX: Should we add a glControl.Paint that invokes OnDraw()? Have not done for now, since OnDraw is
			// already getting called and seems to work -- adding another call would result in double-rendering
			// everything unless we disable the other callsite.

			Child = glControl;
        }

        public OtkWpfWFControl(GraphicsMode mode) : this(mode, 1, 0, GraphicsContextFlags.Default)
        { }

		public OtkWpfWFControl() : this(GraphicsMode.Default)
		{ }

        public void MakeCurrent()
		{
			glControl.MakeCurrent();
		}

		public void SwapBuffers()
		{
			glControl.SwapBuffers();
		}
	}
}
