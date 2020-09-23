using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Amazon.Rekognition.Model;
using Point = Amazon.Rekognition.Model.Point;

namespace CloudImagePixelizer.aws
{
	public class AmazonRekognitionCoordinateTranslator
	{
		// Translates a relative bounding box to an absolute border definition using the absolute image resolution
		public static Rectangle RelativeBoxToAbsolute(BoundingBox box, int width, int height)
		{
			var y = (int) Math.Round(box.Top * height);
			var h = (int) Math.Round(box.Height * height);
			var x = (int) Math.Round(box.Left * width);
			var w = (int) Math.Round(box.Width * width);
			return new Rectangle(x, y, w, h);
		}
		
		// Translates a relative polygon to an absolute edge-aligned border definition using the absolute image
		// resolution
		public static Rectangle RelativePolygonToAbsolute(List<Point> poly, int width, int height)
		{
			var y1 = (int) Math.Round(poly.Select(p => p.Y).Min() * height);
			var y2 = (int) Math.Round(poly.Select(p => p.Y).Max() * height);
			var x1 = (int) Math.Round(poly.Select(p => p.X).Min() * width);
			var x2 = (int) Math.Round(poly.Select(p => p.X).Max() * width);
			return new Rectangle(x1, y1, x2 - x1, y2 - y1);
		}
	}
}