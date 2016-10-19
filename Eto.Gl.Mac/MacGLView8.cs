using System;
using Eto.Mac.Forms;
using OpenTK.Graphics;
using OpenTK.Platform;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
using CGRect = System.Drawing.RectangleF;
using CGSize = System.Drawing.SizeF;

namespace Eto.Gl.Mac
#elif XAMMAC2
using AppKit;
using Foundation;
using CoreGraphics;

namespace Eto.Gl.XamMac
#endif
{
	public class MacGLView8 : NSView, IMacControl
	{
		GraphicsMode mode;
		int major;
		int minor;
		GraphicsContextFlags flags;

		public event EventHandler Initialized;

		public event EventHandler ShuttingDown;

		public event EventHandler DrawNow;


		public MacGLView8(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
		{
			this.mode = mode;
			this.major = major;
			this.minor = minor;
			this.flags = flags;
		}

		GraphicsContext context;
		IWindowInfo windowInfo;

		static MacGLView8()
		{
			GraphicsContext.ShareContexts = true;
		}

		public override bool IsOpaque
		{
			get { return true; }
		}

		public override void DrawRect(CGRect dirtyRect)
		{
			if (!IsInitialized)
				InitGL();

			MakeCurrent();
			DrawNow?.Invoke(this, EventArgs.Empty);
		}

		public bool IsInitialized { get { return context != null; } }

		public void MakeCurrent()
		{
			context?.MakeCurrent(windowInfo);
		}

		public void SwapBuffers()
		{
			context?.SwapBuffers();
		}

		public override void ViewDidMoveToWindow()
		{
			base.ViewDidMoveToWindow();
			UpdateContext();
		}

		public override void SetFrameSize(CGSize newSize)
		{
			base.SetFrameSize(newSize);
			UpdateContext();
		}

		public void InitGL()
		{
			if (IsInitialized || Window == null)
				return;

			windowInfo = Utilities.CreateMacOSWindowInfo(Window.Handle, Handle);

			context = new GraphicsContext(mode, windowInfo, major, minor, flags);

			MakeCurrent();

			context.LoadAll();

			Initialized?.Invoke(this, EventArgs.Empty);
		}

		void UpdateContext()
		{
			if (!IsInitialized)
				InitGL();
			else if (Window != null)
			{
				windowInfo = Utilities.CreateMacOSWindowInfo(Window.Handle, Handle);
				context.Update(windowInfo);
			}
		}

		public bool CanFocus { get; set; }

		public override bool AcceptsFirstResponder()
		{
			return CanFocus;
		}

		public override bool AcceptsFirstMouse(NSEvent theEvent)
		{
			return CanFocus;
		}

		public WeakReference WeakHandler { get; set; }

	}
}