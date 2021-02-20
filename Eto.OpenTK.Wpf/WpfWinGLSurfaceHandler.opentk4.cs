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
			Settings = new GLWpfControlSettings { RenderContinuously = false };
			Control.Ready += Control_Ready;
        }

		protected override void Initialize()
		{
			base.Initialize();
			// not getting handled automatically via an override.. :|
            HandleEvent(GLSurface.GLDrawEvent);
		}

		public override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Control.Start(Settings);
		}

		private void Control_Ready()
		{
			IsInitialized = true;
			Callback.OnInitialized(Widget, EventArgs.Empty);
		}

		public override Color BackgroundColor { get; set; }

        public bool IsInitialized { get; private set; }

        public void MakeCurrent() => Settings.ContextToUse?.MakeCurrent();

        public void SwapBuffers() => Settings.ContextToUse?.SwapBuffers();

        void UpdateView()
        {
            if (!Control.IsInitialized)
                return;

			// viewport size is automatically set
            Callback.OnDraw(Widget, EventArgs.Empty);
        }

        public override void AttachEvent(string id)
        {
            switch (id)
            {
                case GLSurface.GLInitializedEvent:
                    // handled implicitly
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