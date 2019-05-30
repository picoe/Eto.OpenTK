using Eto.Gl;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestEtoGl.WPF_WinformsHost
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      var platform = new Eto.Wpf.Platform();
      platform.Add<GLSurface.IHandler>(() => new Eto.Gl.Wpf.WpfWinGLSurfaceHandler());

      new Application(platform).Run(new MainForm());
    }
  }
}
