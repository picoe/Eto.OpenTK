using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace otkViewPort
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
