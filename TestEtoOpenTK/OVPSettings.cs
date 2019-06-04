using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using LibTessDotNet.Double;

namespace TestEtoGl
{
	public static class errorReporter
	{
		public static void showMessage_OK(string stringToDisplay, string caption)
		{
			Application.Instance.Invoke(() =>
			{
				MessageBox.Show(stringToDisplay, caption, MessageBoxButtons.OK);
			});
		}
	}

	public class OVPSettings
	{
		public float minX, maxX, minY, maxY;
		public bool enableFilledPolys;
		public bool immediateMode; // if true, don't use VBOs.
		public bool drawPoints;
		public RectangleF bounds;
		public float base_zoom;
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
		public PointF default_cameraPosition;
		public bool antiAlias;
		public List<ovp_Poly> polyList;
		public List<ovp_Poly> lineList; // purely for lines.
		public List<bool> drawnPoly; // tracks whether the polygon corresponds to an enabled configuration or not.
		public List<bool> bgPoly; // background polygon

		public float zoom()
		{
			return base_zoom * zoomFactor;
		}

		public void updateColors(Color newColor)
		{
			for (int poly = 0; poly < polyList.Count(); poly++)
			{
				polyList[poly].color = newColor;
			}
		}

		public void reset()
		{
			minX = 0;
			maxX = 0;
			minY = 0;
			maxY = 0;
			clear();
			drawnPoly.Clear();
			bgPoly.Clear();
		}

		public void clear()
		{
			polyList.Clear();
			lineList.Clear();
		}

		public void addLine(PointF[] line, Color lineColor, float alpha)
		{
			pAddLine(line, lineColor, alpha);
		}

		void pAddLine(PointF[] line, Color lineColor, float alpha)
		{
			lineList.Add(new ovp_Poly(line, lineColor, alpha));
		}

		public void addPolygon(PointF[] poly, Color polyColor, float alpha, bool drawn)
		{
            if (drawn)
            {
                // Drawn polygons are to be treated as lines : they don't get filled.
                addLine(poly, polyColor, alpha);
            }
            else
            {
                pAddPolygon(poly, polyColor, alpha, drawn);
            }
		}

		void pAddPolygon(PointF[] poly, Color polyColor, float alpha, bool drawn)
		{
			List<PointF[]> polys = checkPoly(poly);
			for (int p = 0; p < polys.Count; p++)
			{
				pAddPolygon_2(polys[p], polyColor, alpha, drawn);
			}
		}

		void pAddPolygon_2(PointF[] poly, Color polyColor, float alpha, bool drawn)
		{
			polyList.Add(new ovp_Poly(poly, polyColor, alpha));
			drawnPoly.Add(drawn);
			bgPoly.Add(false);
		}

		public void addBGPolygon(PointF[] poly, Color polyColor, float alpha)
		{
			pAddBGPolygon(poly, polyColor, alpha);
		}

		void pAddBGPolygon(PointF[] poly, Color polyColor, float alpha)
		{
			List<PointF[]> polys = checkPoly(poly);

			for (int p = 0; p < polys.Count; p++)
			{
				pAddBGPolygon_2(polys[p], polyColor, alpha);
			}
		}

		void pAddBGPolygon_2(PointF[] poly, Color polyColor, float alpha)
		{
			polyList.Add(new ovp_Poly(poly, polyColor, alpha));
			bgPoly.Add(true);
			drawnPoly.Add(false);
		}

		public OVPSettings(float defX = 0.0f, float defY = 0.0f)
		{
			base_zoom = 1.0f;
			minorGridColor = new Color(1.0f, 0.8f, 0.3f);
			majorGridColor = new Color(0.2f, 0.8f, 0.3f);
			axisColor = new Color(0.1f, 0.1f, 0.1f);
			backColor = new Color(1.0f, 1.0f, 1.0f);
			selectionColor = SystemColors.Highlight;
			inverSelectionColor = SystemColors.Highlight;
			allowZoomAndPan = true;
			enableFilledPolys = true;
			drawPoints = false;
			dynamicGrid = true;
			panning = false;
			selecting = false;
			showGrid = true;
			showAxes = true;
			gridSpacing = 10;
			antiAlias = true;
			zoomStep = 1;
			immediateMode = false;
			fullReset(defX, defY);
		}

		public void fullReset(float defX = 0.0f, float defY = 0.0f)
		{
			default_cameraPosition = new PointF(defX, defY);
			polyList = new List<ovp_Poly>();
			lineList = new List<ovp_Poly>();
			drawnPoly = new List<bool>();
			bgPoly = new List<bool>();
			zoomFactor = 1.0f;
			cameraPosition = new PointF(default_cameraPosition.X, default_cameraPosition.Y);
		}

		PointF[] clockwiseOrder(PointF[] iPoints)
		{
			// Based on stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
			// Shoelace formula.

			double delta = 0;

			for (Int32 pt = 0; pt < iPoints.Length; pt++)
			{
				double deltaX = 0;
				double deltaY = 0;
				if (pt == iPoints.Length - 1)
				{
					deltaX = (iPoints[0].X - iPoints[pt].X);
					deltaY = (iPoints[0].Y + iPoints[pt].Y);
				}
				else
				{
					deltaX = (iPoints[pt + 1].X - iPoints[pt].X);
					deltaY = (iPoints[pt + 1].Y + iPoints[pt].Y);
				}

				delta += deltaX * deltaY;
			}

			if (delta > 0)
			{
				// clockwise
			}
			else
			{
				// counter-clockwise.
				Array.Reverse(iPoints);
			}

			return iPoints;
		}

		List<PointF[]> checkPoly(PointF[] poly)
		{
			List<PointF[]> output = new List<PointF[]>();

			PointF[] source = poly.ToArray();

			if ((poly[0].X != poly[poly.Length - 1].X) && (poly[0].Y != poly[poly.Length - 1].Y))
			{
				PointF[] tempPoly = new PointF[poly.Length + 1];
				for (int pt = 0; pt < poly.Length; pt++)
				{
					tempPoly[pt] = new PointF(poly[pt].X, poly[pt].Y);
				}
				tempPoly[tempPoly.Length - 1] = new PointF(tempPoly[0].X, tempPoly[0].Y);
				source = tempPoly.ToArray();
			}

			// Now we need to check for polyfill, and triangulate the polygon if needed.
			if (enableFilledPolys)
			{
				var tess = new Tess();

				ContourVertex[] contour = new ContourVertex[source.Length];
				for (int pt = 0; pt < contour.Length; pt++)
				{
					contour[pt].Position = new Vec3 { X = source[pt].X, Y = source[pt].Y, Z = 0 };
				}
				tess.AddContour(contour, ContourOrientation.Clockwise); // keep our orientation to allow holes to be handled.

				// Triangulate.
				tess.Tessellate(WindingRule.Positive, ElementType.Polygons, 3); // We don't have any hole polygons here.

				// Iterate triangles and create output geometry
				for (int i = 0; i < tess.ElementCount; i++)
				{
					PointF[] tempPoly = new PointF[3]; // 3 points.
					tempPoly[0] = new PointF((float)tess.Vertices[tess.Elements[i * 3]].Position.X, (float)tess.Vertices[tess.Elements[i * 3]].Position.Y);
					tempPoly[1] = new PointF((float)tess.Vertices[tess.Elements[(i * 3) + 1]].Position.X, (float)tess.Vertices[tess.Elements[(i * 3) + 1]].Position.Y);
					tempPoly[2] = new PointF((float)tess.Vertices[tess.Elements[(i * 3) + 2]].Position.X, (float)tess.Vertices[tess.Elements[(i * 3) + 2]].Position.Y);

					output.Add(clockwiseOrder(tempPoly).ToArray());
				}
			}
			else
			{
				output.Add(source.ToArray());
			}

			return output;
		}
	}
}
