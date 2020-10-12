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

		/// <summary>
		/// Only supports (effectively) non-flipping RotateFlipTypes. Rotations are meant to be clockwise.
		/// </summary>
		/// <param name="rectangle"></param>
		/// <param name="rotation"></param>
		/// <param name="imageWidth"></param>
		/// <param name="imageHeight"></param>
		/// <returns></returns>
		public static Rectangle RotateRectangle(Rectangle rectangle, RotateFlipType rotation, int imageWidth, int imageHeight)
		{
			switch ((int) rotation)
			{
				// no rotation no flip, 180° rotation XY flip
				case 0:
					return rectangle;
				// 90° rotation no flip, 270° rotation XY flip
				case 1:
					return new Rectangle(x: imageHeight - rectangle.Y - rectangle.Height, y: rectangle.X, width: rectangle.Height, height: rectangle.Width);
				// 180° rotation no flip, no rotation XY flip
				case 2:
					return new Rectangle(x: imageWidth - rectangle.X - rectangle.Width, y: imageHeight - rectangle.Y - rectangle.Height, width: rectangle.Width, height: rectangle.Height);
				// 270° rotation no flip, 90° rotation XY flip
				case 3:
					return new Rectangle(x: rectangle.Y, y: imageWidth - rectangle.X - rectangle.Width, width: rectangle.Height, height: rectangle.Width);
				// others will flip only one dimension each, which is not needed so far, thus not supported
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}