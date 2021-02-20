using System;
using System.Diagnostics;
using Eto.Drawing;
using Eto.Mac.Forms;
using OpenTK.Graphics;
using OpenTK;
using Eto.Mac;
using Eto.OpenTK;

#if MONOMAC
using MonoMac.AppKit;
#elif XAMMAC2
using AppKit;
#endif

[assembly: Eto.ExportHandler(typeof(GLSurface), typeof(Eto.OpenTK.Mac.MacGLSurfaceHandler))]

namespace Eto.OpenTK.Mac
{
	public class MacGLSurfaceHandler : MacView<MacGLView, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
	{
		protected override void Initialize()
		{
			base.Initialize();

			HandleEvent(GLSurface.GLDrawEvent);
		}

        public void Create()
		{
			Control = new MacGLView { WeakHandler = new WeakReference(this) };
			Control.CanFocus = true;
		}

		public override bool Enabled { get; set; }

		public override NSView ContainerControl => Control;

		public bool IsInitialized => Control.IsInitialized;

		public void MakeCurrent() => Control.MakeCurrent();

		public void SwapBuffers() => Control.SwapBuffers();

		public bool CanFocus
		{
			get { return Control.CanFocus; }
			set { Control.CanFocus = value; }
		}

		public override void Invalidate(Rectangle rect, bool invalidateChildren)
		{
			Control.NeedsToDraw(rect.ToNS());
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case GLSurface.GLInitializedEvent:
					Control.Initialized += (sender, args) => Callback.OnInitialized(Widget, args);
					break;

				case GLSurface.GLShuttingDownEvent:
					Control.ShuttingDown += (sender, args) => Callback.OnShuttingDown(Widget, args);
					break;

				case GLSurface.GLDrawEvent:
					Control.Draw += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
					break;

				default:
					base.AttachEvent(id);
					break;
			}
		}
	}
}