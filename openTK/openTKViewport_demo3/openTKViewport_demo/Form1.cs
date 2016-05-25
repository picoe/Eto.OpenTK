using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using otkViewPort;

namespace openTKViewport_demo
{
    using GL = OpenTK.Graphics.OpenGL.GL;
    public partial class Form1 : Form
    {
        private OpenTK.GLControl glControl1;
        private OpenTK.GLControl glControl2;
        openTKViewPort oVP2, oVP;

        public OVPSettings ovpSettings, ovp2Settings;

        bool loaded = false;
        bool loaded2 = false;

        public Form1()
        {
            ovpSettings = new OVPSettings();
            ovp2Settings = new OVPSettings();
            List<PointF[]> polyList = new List<PointF[]>();
            PointF[] testPoly = new PointF[5];
            testPoly[0] = new PointF(100, 100);
            testPoly[1] = new PointF(200, 100);
            testPoly[2] = new PointF(200, 50);
            testPoly[3] = new PointF(100, 50);
            testPoly[4] = testPoly[0];

            polyList.Add(testPoly);
            ovpSettings.addPolygon(testPoly, Color.Black);
            ovp2Settings.addPolygon(testPoly, Color.Black);

            testPoly = new PointF[5];
            testPoly[0] = new PointF(-80, -100);
            testPoly[1] = new PointF(-180, -100);
            testPoly[2] = new PointF(-200, -50);
            testPoly[3] = new PointF(-100, -50);
            testPoly[4] = testPoly[0];
            polyList.Add(testPoly);
            ovpSettings.addPolygon(testPoly, Color.Red);
            ovp2Settings.addPolygon(testPoly, Color.Red);
            ovp2Settings.zoomFactor = 3;

            InitializeComponent();
            setupViewports();

            glControl1.Load += glControl1_Load;
            glControl1.Paint += glControl1_Paint;

            glControl2.Load += glControl2_Load;
            glControl2.Paint += glControl2_Paint;

            ovp2Settings.zoomFactor = 0.5f;

            button1.Click += changePoly;
        }

        private void changePoly(object sender, EventArgs e)
        {
            ovpSettings.polyList.Clear();
            PointF[] testPoly = new PointF[5];
            testPoly[0] = new PointF(-80, -100);
            testPoly[1] = new PointF(-180, -100);
            testPoly[2] = new PointF(-200, -50);
            testPoly[3] = new PointF(-100, -50);
            testPoly[4] = testPoly[0];
            ovpSettings.addPolygon(testPoly, Color.Green);
            glControl1.Invalidate();
        }

        private void setupViewports()
        {
            this.glControl1 = new OpenTK.GLControl();
            this.glControl2 = new OpenTK.GLControl();
            this.SuspendLayout();
            // 
            // glControl1
            // 
            this.glControl1.BackColor = System.Drawing.Color.Black;
            this.glControl1.Location = new System.Drawing.Point(328, 13);
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(259, 236);
            this.glControl1.TabIndex = 0;
            this.glControl1.VSync = false;
            // 
            // glControl2
            // 
            this.glControl2.BackColor = System.Drawing.Color.Black;
            this.glControl2.Location = new System.Drawing.Point(30, 29);
            this.glControl2.Name = "glControl2";
            this.glControl2.Size = new System.Drawing.Size(150, 150);
            this.glControl2.TabIndex = 1;
            this.glControl2.VSync = false;

            this.tabPage1.Controls.Add(this.glControl2);
            this.tabPage2.Controls.Add(this.glControl1);
            this.ResumeLayout(false);
        }


        private void glControl2_Load(object sender, EventArgs e)
        {
            oVP2 = new openTKViewPort(ref glControl2, ovp2Settings);
            loaded2 = true;
            GL.ClearColor(Color.Moccasin); // Yey! .NET Colors can be used directly!
        }

        private void glControl2_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded2) // Play nice
                return;

            oVP2.updateViewport();
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            oVP = new openTKViewPort(ref glControl1, ovpSettings);
            loaded = true;
            GL.ClearColor(Color.SkyBlue); // Yey! .NET Colors can be used directly!
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) // Play nice
                return;

            oVP.updateViewport();
        }
    }
}
