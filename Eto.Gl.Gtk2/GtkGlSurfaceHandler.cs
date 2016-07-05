using Eto.Drawing;
using Eto.GtkSharp.Forms;
using OpenTK.Graphics;
using OpenTK;

namespace Eto.Gl.Gtk
{
    public class GtkGlSurfaceHandler : GtkControl<GLDrawingArea, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
    {
		static GtkGlSurfaceHandler()
		{
			GraphicsContext.ShareContexts = true;
		}

		protected override void Initialize ()
		{
			base.Initialize ();
			HandleEvent (GLSurface.GLDrawEvent);
		}

        public void CreateWithParams (GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        {
            Control = new GLDrawingArea (mode, major, minor, flags);
        }

        public bool IsInitialized {
            get { return Control.IsInitialized; }
        }

        public void MakeCurrent ()
        {
            Control.MakeCurrent ();
        }

        public void SwapBuffers ()
        {
            Control.SwapBuffers ();
        }

        public override void AttachEvent (string id)
        {
            switch (id) {
            case GLSurface.GLInitializedEvent:
                Control.Initialized += (sender, args) => Callback.OnInitialized (this.Widget, args);
                break;

            case GLSurface.GLShuttingDownEvent:
                Control.ShuttingDown += (sender, args) => Callback.OnShuttingDown (this.Widget, args);
                break;

			case GLSurface.GLDrawEvent:
				Control.Resize += (sender, args) => {
					if (Control.IsInitialized)
						Callback.OnDraw (this.Widget, args);
				};
                break;

            default:
                base.AttachEvent (id);
                break;
            }
        }
    }

}
