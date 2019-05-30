using Eto.Drawing;
using Eto.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using Eto.Wpf.Forms;
using Eto.Gl.Windows;

namespace Eto.Gl.Wpf
{
    public class WpfWinGLSurfaceHandler : WindowsFormsHostHandler<WinGLUserControl, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
    {
        public void CreateWithParams(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        {
            WinFormsControl = new WinGLUserControl(mode, major, minor, flags);
        }

        protected override void Initialize()
        {
            base.Initialize();
            HandleEvent(GLSurface.GLDrawEvent);
        }

        public bool IsInitialized
        {
            get { return Control.IsInitialized; }
        }

        public void MakeCurrent()
        {
            WinFormsControl.MakeCurrent();
        }

        public void SwapBuffers()
        {
            WinFormsControl.SwapBuffers();
        }

        public void updateViewHandler(object sender, EventArgs e)
        {
            updateView();
        }

        public void updateView()
        {
            if (!Control.IsInitialized)
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

                case GLSurface.ShownEvent:
                case GLSurface.SizeChangedEvent:
                case GLSurface.GLDrawEvent:
                    WinFormsControl.SizeChanged += updateViewHandler;
                    WinFormsControl.Paint += updateViewHandler;
                    //Control.Resize += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
                    break;

                default:
                    base.AttachEvent(id);
                    break;
            }
        }
    }
}