using System;
using System.Diagnostics;
using Eto.Drawing;
using Eto.Mac.Forms;
using OpenTK.Graphics;
using OpenTK;
using Eto.Mac;

#if MONOMAC
using MonoMac.AppKit;

namespace Eto.Gl.Mac
#elif XAMMAC2
using AppKit;

namespace Eto.Gl.XamMac
#endif
{
	public class MacGLSurfaceHandler : MacView<MacGLView8, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
	{
		static MacGLSurfaceHandler()
		{
			Toolkit.Init(new ToolkitOptions
			{
				Backend = PlatformBackend.PreferNative // prevent SDL2 from being detected if it is installed.
			});
		}

		protected override void Initialize()
		{
			base.Initialize();

			HandleEvent(GLSurface.GLDrawEvent);
		}

		public void CreateWithParams(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
		{
			Control = new MacGLView8(mode, major, minor, flags);
			Control.CanFocus = true;
		}

		public override bool Enabled { get; set; }

		public override NSView ContainerControl
		{
			get { return Control; }
		}

		public bool IsInitialized
		{
			get { return Control.IsInitialized; }
		}

		public void MakeCurrent()
		{
			Control.MakeCurrent();
		}

		public void SwapBuffers()
		{
			Control.SwapBuffers();
		}

		public bool CanFocus
		{
			get { return Control.CanFocus; }
			set { Control.CanFocus = value; }
		}

		public override void Invalidate(bool invalidateChildren)
		{
			Control.NeedsNewContext = true;
			base.Invalidate(invalidateChildren);
		}

		public override void Invalidate(Rectangle rect, bool invalidateChildren)
		{
			Control.NeedsNewContext = true;
			base.Invalidate(rect, invalidateChildren);
			var needsDisplayInRect = rect.ToNS();
			if (!Control.IsFlipped)
				needsDisplayInRect.Y = Control.Frame.Height - needsDisplayInRect.Y - needsDisplayInRect.Height;
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
					Control.DrawNow += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
					break;

				case Eto.Forms.Control.SizeChangedEvent:
					Control.SizeChanged += (sender, e) => Callback.OnSizeChanged(Widget, EventArgs.Empty);
					break;

				default:
					base.AttachEvent(id);
					break;
			}
		}
	}
}