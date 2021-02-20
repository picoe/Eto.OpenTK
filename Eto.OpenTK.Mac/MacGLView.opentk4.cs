#if OPENTK4
using System;
using Eto.Mac.Forms;
using OpenTK.Graphics.OpenGL;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using System.Threading;
#elif XAMMAC2
using AppKit;
using Foundation;
using CoreGraphics;
#endif

namespace Eto.OpenTK.Mac
{
    public class MacGLView : NSOpenGLView, IMacControl
    {
		public static bool UseSharedContext = true;
        public event EventHandler Initialized;
        public event EventHandler ShuttingDown;

        public event EventHandler Draw;

        class MacBindingsContext : global::OpenTK.IBindingsContext
        {
            IntPtr? libraryPtr;
            public IntPtr GetProcAddress(string procName)
            {
                if (libraryPtr == null)
                {
                    libraryPtr = MonoMac.ObjCRuntime.Dlfcn.dlopen("/System/Library/Frameworks/OpenGL.framework/OpenGL", 0);
                }

                if (libraryPtr != IntPtr.Zero)
                {
                    var ptr = MonoMac.ObjCRuntime.Dlfcn.dlsym(libraryPtr.Value, procName);
					// if (ptr == IntPtr.Zero)
					// 	throw new InvalidOperationException($"Could not find procedure name {procName}");
					return ptr;
                }
                return IntPtr.Zero;
            }
        }

        static MacGLView()
        {
            GL.LoadBindings(new MacBindingsContext());
        }

		public MacGLView()
		{
			if (UseSharedContext)
			{
				var pixelFormat = new NSOpenGLPixelFormat(
					NSOpenGLPixelFormatAttribute.DoubleBuffer,
					NSOpenGLPixelFormatAttribute.FullScreen
					// what else?
					// NSOpenGLPixelFormatAttribute.OpenGLProfile, NSOpenGLProfile.Version3_2Core
				);

				Interlocked.Increment(ref sharedContextCount);
				if (sharedContext == null)
				{
					sharedContext = new NSOpenGLContext(pixelFormat, null);
				}
				OpenGLContext = new NSOpenGLContext(pixelFormat, sharedContext);
			}
		}

		static NSOpenGLContext sharedContext;
		static int sharedContextCount;

		~MacGLView() => Dispose(false);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

			if (disposing)
			{
				MakeCurrent();
				ShuttingDown?.Invoke(this, EventArgs.Empty);

				if (UseSharedContext && Interlocked.Decrement(ref sharedContextCount) == 0)
				{
					sharedContext?.Dispose();
					sharedContext = null;
				}
			}
        }

        bool needsViewportUpdate = true;
        public override void Reshape()
        {
            base.Reshape();
            if (IsInitialized)
            {
                needsViewportUpdate = true;
                NeedsDisplay = true;
            }
        }

        public override void DrawRect(CGRect dirtyRect)
        {
            if (!IsInitialized)
                InitGL();

            if (needsViewportUpdate)
            {
				var logicalPixelSize = Window?.Screen?.BackingScaleFactor ?? 1;
				var size = Frame.Size;
                GL.Viewport(0, 0, (int)(size.Width * logicalPixelSize), (int)(size.Height * logicalPixelSize));
                needsViewportUpdate = false;
            }

            OpenGLContext.MakeCurrentContext();

            Draw?.Invoke(this, EventArgs.Empty);
        }

        public bool IsInitialized { get; private set; }

        public void MakeCurrent() => OpenGLContext.MakeCurrentContext();

        public void SwapBuffers() => OpenGLContext.FlushBuffer();

        void InitGL()
        {
            if (IsInitialized || Window == null)
                return;

            IsInitialized = true;

            Initialized?.Invoke(this, EventArgs.Empty);
        }

        public bool CanFocus { get; set; }

        public override bool AcceptsFirstResponder() => CanFocus;

        public override bool AcceptsFirstMouse(NSEvent theEvent) => CanFocus;

        public WeakReference WeakHandler { get; set; }
    }
}
#endif