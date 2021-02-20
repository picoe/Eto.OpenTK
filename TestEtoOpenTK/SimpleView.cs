using System;
using System.Diagnostics;
using Eto.Forms;
using Eto.OpenTK;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
#if OPENTK4
using OpenTK.Mathematics;
using OpenTK.Audio.OpenAL;
#endif

namespace TestEtoOpenTK
{
	public class SimpleView : GLSurface
	{
		Stopwatch _stopwatch = Stopwatch.StartNew();

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.DepthTest);
			// GL.Enable(EnableCap.ScissorTest);
		}

		protected override void OnDraw(EventArgs e)
		{
			base.OnDraw(e);

			var hue = (float)_stopwatch.Elapsed.TotalSeconds * 0.15f % 1;
			var c = Color4.FromHsv(new Vector4(hue, 0.75f, 0.75f, 1));
			GL.ClearColor(c);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.LoadIdentity();
			GL.Begin(PrimitiveType.Triangles);

			GL.Color4(Color4.Red);
			GL.Vertex2(0.0f, 0.5f);

			GL.Color4(Color4.Green);
			GL.Vertex2(0.58f, -0.5f);

			GL.Color4(Color4.Blue);
			GL.Vertex2(-0.58f, -0.5f);

			GL.End();
			GL.Finish();
      Application.Instance.AsyncInvoke(Invalidate);
		}
	}
}