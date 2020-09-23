using System;
using System.Drawing;
using System.Linq;
using Google.Cloud.Vision.V1;

namespace CloudImagePixelizer.gcp
{
	public class GoogleVisionCoordinateTranslator
	{
		// Translates an absolute bounding poly to an absolute edge-aligned border definition
		public static Rectangle AbsolutePolyToRectangle(BoundingPoly poly)
		{
			var y1 = poly.Vertices.Select(v => v.Y).Min();
			var y2 = poly.Vertices.Select(v => v.Y).Max();
			var x1 = poly.Vertices.Select(v => v.X).Min();
			var x2 = poly.Vertices.Select(v => v.X).Max();
			return new Rectangle(x1, y1, x2 - x1, y2 - y1);
		}

		public static Rectangle RelativePolyToRectangle(BoundingPoly poly, int width, int height)
		{
			var y1 = (int) Math.Round(poly.NormalizedVertices.Select(v => v.Y).Min() * height);
			var y2 = (int) Math.Round(poly.NormalizedVertices.Select(v => v.Y).Max() * height);
			var x1 = (int) Math.Round(poly.NormalizedVertices.Select(v => v.X).Min() * width);
			var x2 = (int) Math.Round(poly.NormalizedVertices.Select(v => v.X).Max() * width);
			return new Rectangle(x1, y1, x2 - x1, y2 - y1);	
		}
	}
}