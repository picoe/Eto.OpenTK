using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eto.Drawing;

namespace Mac_GUI_testing_MM
{
    public class OVPSettings
    {
        public RectangleF bounds;
        public float zoomFactor;
        public Int32 zoomStep;
        public bool allowZoomAndPan;
        public bool dynamicGrid;
        public bool panning;
        public bool selecting;
        public bool showGrid;
        public bool showAxes;
        public int gridSpacing;
        public Color minorGridColor;
        public Color majorGridColor;
        public Color axisColor;
        public Color backColor;
        public Color selectionColor;
        public Color inverSelectionColor;
        public PointF cameraPosition;
        public bool antiAlias;
        public List<ovp_Poly> polyList;
        public List<bool> drawnPoly; // tracks whether the polygon corresponds to an enabled configuration or not.

        public void updateColors(Color newColor)
        {
            for (int poly = 0; poly < polyList.Count(); poly++)
            {
                polyList[poly].color = newColor;
            }
        }

        public void reset()
        {
            polyList.Clear();
            drawnPoly.Clear();
        }

        public void addPolygon(PointF[] poly, Color polyColor)
        {
            polyList.Add(new ovp_Poly(poly, polyColor));
        }

        public void addPolygon(PointF[] poly, Color polyColor, bool drawn)
        {
            polyList.Add(new ovp_Poly(poly, polyColor));
            drawnPoly.Add(drawn);
        }

        public OVPSettings()
        {
            polyList = new List<ovp_Poly>();
            drawnPoly = new List<bool>();
            allowZoomAndPan = true;
            dynamicGrid = true;
            panning = false;
            selecting = false;
            showGrid = true;
            showAxes = true;
            gridSpacing = 10;
            minorGridColor = new Color(1.0f, 0.8f, 0.3f);
            majorGridColor = new Color(0.2f, 0.8f, 0.3f);
            axisColor = new Color(0.1f, 0.1f, 0.1f);
            backColor = new Color(1.0f, 1.0f, 1.0f);
            selectionColor = SystemColors.Highlight;
            inverSelectionColor = SystemColors.Highlight;
            antiAlias = true;
            zoomFactor = 1.0f;
            zoomStep = 1;
            cameraPosition = new PointF(0, 0);
        }
    }
}
