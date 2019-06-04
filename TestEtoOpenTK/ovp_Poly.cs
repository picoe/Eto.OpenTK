using Eto.Drawing;

namespace TestEtoGl
{
	public class ovp_Poly
	{
		public PointF[] poly;
		public Color color;
		public float alpha;

		public ovp_Poly(PointF[] geometry, Color geoColor)
		{
			poly = geometry;
			color = geoColor;
			alpha = 1.0f;
		}

		public ovp_Poly(PointF[] geometry, Color geoColor, float alpha_)
		{
			poly = geometry;
			color = geoColor;
			alpha = alpha_;
		}
	}
}
