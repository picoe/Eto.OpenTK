#if OPENTK3
using Eto.Drawing;
using Eto.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using Eto.Wpf.Forms;
using Eto.OpenTK;
using Eto.OpenTK.WinForms;

[assembly: Eto.ExportHandler(typeof(GLSurface), typeof(Eto.OpenTK.Wpf.WpfWinGLSurfaceHandler))]

namespace Eto.OpenTK.Wpf
{
    public class WpfWinGLSurfaceHandler : WindowsFormsHostHandler<WinGLUserControl, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
    {
        public void Create()
        {
            WinFormsControl = new WinGLUserControl(GraphicsMode.Default, 3, 0, GraphicsContextFlags.Default);
            Control.Focusable = true;
            Control.Background = System.Windows.SystemColors.ControlBrush;
        }

        protected override void Initialize()
        {
            base.Initialize();
            HandleEvent(GLSurface.GLDrawEvent);
        }

        public bool IsInitialized => WinFormsControl.IsInitialized;

        public void MakeCurrent() => WinFormsControl.MakeCurrent();

        public void SwapBuffers() => WinFormsControl.SwapBuffers();

        public void UpdateView()
        {
            if (!WinFormsControl.IsInitialized)
                return;

            MakeCurrent();
            GL.Viewport(WinFormsControl.Size);
            Callback.OnDraw(Widget, EventArgs.Empty);
            SwapBuffers();
        }

        public override void AttachEvent(string id)
        {
            switch (id)
            {
                case GLSurface.GLInitializedEvent:
                    WinFormsControl.Initialized += (sender, e) => Callback.OnInitialized(Widget, EventArgs.Empty);
                    break;

                case GLSurface.GLShuttingDownEvent:
                    WinFormsControl.ShuttingDown += (sender, e) => Callback.OnShuttingDown(Widget, e);
                    break;

                case GLSurface.GLDrawEvent:
                    WinFormsControl.Paint += (sender, e) => UpdateView();
                    break;

                default:
                    base.AttachEvent(id);
                    break;
            }
        }
    }
}
#endif