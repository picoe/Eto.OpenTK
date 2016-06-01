using Eto.Drawing;

namespace Mac_GUI_testing_MM
{
    public class ovp_Poly
    {
        public PointF[] poly;
        public Color color;
        public ovp_Poly(PointF[] geometry, Color geoColor)
        {
            poly = geometry;
            color = geoColor;
        }
    }
}
