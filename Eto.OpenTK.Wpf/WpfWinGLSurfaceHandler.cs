using Eto.Drawing;
using Eto.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using Eto.Wpf.Forms;
using Eto.OpenTK.WinForms;
using Eto.OpenTK;

[assembly: Eto.ExportHandler(typeof(GLSurface), typeof(Eto.OpenTK.Wpf.WpfWinGLSurfaceHandler))]

namespace Eto.OpenTK.Wpf
{
    public class WpfWinGLSurfaceHandler : WindowsFormsHostHandler<WinGLUserControl, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
    {
        public void CreateWithParams(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        {
            WinFormsControl = new WinGLUserControl(mode, major, minor, flags);
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