#if OPENTK4
using Eto.Drawing;
using Eto.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using Eto.Wpf.Forms;
using Eto.OpenTK;
using OpenTK.Wpf;
using System.Windows;

[assembly: Eto.ExportHandler(typeof(GLSurface), typeof(Eto.OpenTK.Wpf.WpfWinGLSurfaceHandler))]

namespace Eto.OpenTK.Wpf
{
    public class WpfWinGLSurfaceHandler : WpfFrameworkElement<global::OpenTK.Wpf.GLWpfControl, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
    {
		public GLWpfControlSettings Settings { get; private set; }

        public void Create()
        {
            Control = new global::OpenTK.Wpf.GLWpfControl();
            Control.Focusable = true;
			Control.Loaded += Control_Loaded;
			Settings = new GLWpfControlSettings { RenderContinuously = false };
        }

		private void Control_Loaded(object sender, EventArgs e)
		{
			Control.Ready += Control_Ready;
			Control.Start(Settings);
		}

		private void Control_Ready()
		{
			IsInitialized = true;
		}

		public override Color BackgroundColor { get; set; }

        public bool IsInitialized { get; private set; }

        public void MakeCurrent() => Settings.ContextToUse?.MakeCurrent();

        public void SwapBuffers() => Settings.ContextToUse?.SwapBuffers();

        void UpdateView()
        {
            if (!Control.IsInitialized)
                return;

			var size = Widget.Size;
            GL.Viewport(0, 0, size.Width, size.Height);
            Callback.OnDraw(Widget, EventArgs.Empty);
        }

        public override void AttachEvent(string id)
        {
            switch (id)
            {
                case GLSurface.GLInitializedEvent:
                    Control.Ready += () => Callback.OnInitialized(Widget, EventArgs.Empty);
                    break;

                case GLSurface.GLShuttingDownEvent:
                    // Control.ShuttingDown += (sender, e) => Callback.OnShuttingDown(Widget, e);
                    break;

                case GLSurface.GLDrawEvent:
                    Control.Render += (context) => UpdateView();
                    break;

                default:
                    base.AttachEvent(id);
                    break;
            }
        }
    }
}
#endif