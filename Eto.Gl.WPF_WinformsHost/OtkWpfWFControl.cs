using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Eto.Wpf;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Platform;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using OpenGLviaFramebuffer;

namespace Eto.Gl.WPF_WFControl
{
	public class OtkWpfWFControl : UserControl
	{
		private GLControl glControl;

		private Renderer renderer;

		public OtkWpfWFControl()
		{
			System.Drawing.Size t = new System.Drawing.Size(this.GetSize().Width, this.GetSize().Height);
			this.renderer = new Renderer(t);

			glControl = new GLControl();
			glControl.Paint += GLcontrolOnPaint;
		}

		public void MakeCurrent()
		{
			glControl.MakeCurrent();
		}

		public void SwapBuffers()
		{
			glControl.SwapBuffers();
		}

		private void GLcontrolOnPaint(object sender, EventArgs e)
		{
			glControl.MakeCurrent();
			GL.LoadIdentity();
			renderer.Render();
			glControl.SwapBuffers();
		}
	}
}
