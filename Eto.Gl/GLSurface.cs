using System;
using Eto.Drawing;
using Eto.Forms;
using OpenTK.Graphics;

namespace Eto.Gl
{
    [Handler (typeof (GLSurface.IHandler))]
    public class GLSurface : Control
    {
		public GLSurface () :
            this (GraphicsMode.Default)
        {
        }

        public GLSurface (GraphicsMode graphicsMode) :
            this (graphicsMode, 1, 0, GraphicsContextFlags.Default)
        {

        }

        public GLSurface (GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        {
            Handler.CreateWithParams (mode, major, minor, flags);
            Initialize ();
        }

        static GLSurface ()
        {
            RegisterEvent<GLSurface> (c => c.OnGLInitalized (null), GLInitializedEvent);
            RegisterEvent<GLSurface> (c => c.OnDraw (null), GLDrawEvent);
            RegisterEvent<GLSurface> (c => c.OnShuttingDown (null), GLShuttingDownEvent);
        }

        public const string GLShuttingDownEvent = "GL.ShuttingDown";
        public const string GLDrawEvent = "GL.DrawNow";
        public const string GLInitializedEvent = "GL.Initialized";

        public event EventHandler<EventArgs> GLInitalized {
            add { Properties.AddHandlerEvent (GLInitializedEvent, value); }
            remove { Properties.RemoveEvent (GLInitializedEvent, value); }
        }
        public event EventHandler<EventArgs> Draw {
            add { Properties.AddHandlerEvent (GLDrawEvent, value); }
            remove { Properties.RemoveEvent (GLDrawEvent, value); }
        }
        public event EventHandler<EventArgs> ShuttingDown {
            add { Properties.AddHandlerEvent (GLShuttingDownEvent, value); }
            remove { Properties.RemoveEvent (GLShuttingDownEvent, value); }
        }

        protected virtual void OnGLInitalized (EventArgs e)
        {
            Properties.TriggerEvent (GLInitializedEvent, this, e);
        }

        protected virtual void OnDraw (EventArgs e)
        {
            Properties.TriggerEvent (GLDrawEvent, this, e);
        }

        protected virtual void OnShuttingDown (EventArgs e)
        {
            Properties.TriggerEvent (GLShuttingDownEvent, this, e);
        }

        new IHandler Handler { get { return (IHandler)base.Handler; } }

        // interface to the platform implementations

        // ETO WIDGET -> Platform Control

        [AutoInitialize (false)]
        public new interface IHandler : Control.IHandler
        {
            void CreateWithParams (GraphicsMode mode, int major, int minor, GraphicsContextFlags flags);

            bool IsInitialized { get; }

            void MakeCurrent ();
            void SwapBuffers ();
        }

        public new interface ICallback : Control.ICallback
        {
            void OnInitialized (GLSurface w, EventArgs e);
            void OnShuttingDown (GLSurface w, EventArgs e);
            void OnDraw (GLSurface w, EventArgs e);
        }

        //PLATFORM CONTROL -> ETO WIDGET

        protected new class Callback : Control.Callback, ICallback
        {
            public void OnInitialized (GLSurface w, EventArgs e)
            {
                w.Platform.Invoke (() => w.OnGLInitalized (e));
            }

            public void OnShuttingDown (GLSurface w, EventArgs e)
            {
                w.Platform.Invoke (() => w.OnShuttingDown (e));
            }

            public void OnDraw (GLSurface w, EventArgs e)
            {
                w.Platform.Invoke (() => w.OnDraw (e));
            }
        }

        //Gets an instance of an object used to perform callbacks to the widget from handler implementations

        static readonly object callback = new Callback ();

        protected override object GetCallback ()
        {
            return callback;
        }

        public bool IsInitialized {
            get { return Handler.IsInitialized; }
        }

        public virtual void MakeCurrent ()
        {
            Handler.MakeCurrent ();
        }

        public virtual void SwapBuffers ()
        {
            Handler.SwapBuffers ();
        }
    }
}