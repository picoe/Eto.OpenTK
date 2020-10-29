#if OPENTK3
using System;
using Eto.Mac.Forms;
using OpenTK.Graphics;
using OpenTK.Platform;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
#elif XAMMAC2
using AppKit;
using Foundation;
using CoreGraphics;
#endif

namespace Eto.OpenTK.Mac
{
    public class MacGLView : NSView, IMacControl
    {
        GraphicsMode mode;
        int major;
        int minor;
        GraphicsContextFlags flags;
        GraphicsContext context;
        IWindowInfo windowInfo;

        public event EventHandler Initialized;

        public event EventHandler ShuttingDown;

        public event EventHandler Draw;

        public event EventHandler SizeChanged;


        public MacGLView()
            : this(GraphicsMode.Default, 1, 0, GraphicsContextFlags.Default)
        {

        }

        public MacGLView(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        {
            this.mode = mode;
            this.major = major;
            this.minor = minor;
            this.flags = flags;
        }

        static MacGLView()
        {
            GraphicsContext.ShareContexts = true;
        }

        public override bool IsOpaque
        {
            get { return true; }
        }

        public override void DrawRect(CGRect dirtyRect)
        {
            // only init on the first draw, otherwise we're not able to init properly?
            if (!IsInitialized)
                InitGL();
            else
            {
                if (NeedsNewContext)
                {
                    windowInfo = Utilities.CreateMacOSWindowInfo(Window.Handle, Handle);
                    context.Update(windowInfo);
                    NeedsNewContext = false;
                }
                MakeCurrent();
            }

            Draw?.Invoke(this, EventArgs.Empty);
        }

        public bool IsInitialized => context != null;

        public bool NeedsNewContext { get; set; } = true;

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
            NeedsNewContext = true;
        }

        public override void SetFrameSize(CGSize newSize)
        {
            base.SetFrameSize(newSize);
            NeedsNewContext = true;
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }

        void InitGL()
        {
            if (IsInitialized || Window == null)
                return;

            NeedsNewContext = false;

            windowInfo = Utilities.CreateMacOSWindowInfo(Window.Handle, Handle);

            context = new GraphicsContext(mode, windowInfo, major, minor, flags);

            MakeCurrent();

            context.LoadAll();

            Initialized?.Invoke(this, EventArgs.Empty);
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

        public override void DidChangeBackingProperties()
        {
            // this is called when moving from a retina screen to non-retina 
            // or if the current screen is changed.

            base.DidChangeBackingProperties();
            NeedsNewContext = true;
            NeedsDisplay = true;
        }
    }
}
#endif