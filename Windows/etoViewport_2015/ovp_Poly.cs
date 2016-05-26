using Eto.Drawing;

namespace etoViewport_2015
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
