using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CloudImagePixelizer
{
	/// <summary>
	/// Static helper class to merge a bunch of rectangles into a smaller bunch of bigger rectangles using a given
	/// distance threshold.
	/// </summary>
	public static class ImagePatchClusterizer
	{
		/// <summary>
		/// Merges the given bunch of rectangles into a smaller bunch of bigger rectangles using the given distance
		/// threshold. The algorithm is a modified version of a data clusterizer used in data analysis. The current
		/// implementation uses the manhattan distance to decide which rectangles can be merged. 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="distanceThreshold"></param>
		/// <returns></returns>
		public static IEnumerable<Rectangle> Clusterize(IEnumerable<Rectangle> data, int distanceThreshold)
		{
			var partitions = data.Select(rectangle => new List<Rectangle> {rectangle}).ToList();
			int[,] d = new int[partitions.Count, partitions.Count];
			var optimized = false;
			while (!optimized)
			{
				var minD = -1;
				var minI = -1;
				var minJ = -1;
				// 2D iteration over partitions, nested iteration is always at least one index higher than the outer one
				// stop iteration if partition size reached or minimal distance found is exactly 0 and proceed with
				// merging the partitions because there cannot be another pair of indices with a smaller distance.
				for (var i = 0; i < partitions.Count && (minD == -1 || minD > 0); i++)
				{
					for (var j = i + 1; j < partitions.Count && (minD == -1 || minD > 0); j++)
					{
						// do cluster stuff
						var dist = Distance(partitions[i], partitions[j], (i1, i2) => i1 + i2, int.MaxValue);
						d[i, j] = dist;
						d[j, i] = dist;
						if (i != j && dist <= distanceThreshold && (minD == -1 || dist < minD))
						{
							minD = dist;
							minI = i;
							minJ = j;
						}
					}
				}

				if (minD != -1)
				{
					foreach (var rectangle in partitions[minJ])
					{
						partitions[minI].Add(rectangle);
					}

					partitions[minJ] = new List<Rectangle>();
				}
				else
				{
					optimized = true;
				}
			}

			var merged = new List<Rectangle>();
			foreach (var partition in partitions)
			{
				if (partition.Count <= 0) continue;
				var y1 = partition.Min(r => r.Y);
				var x1 = partition.Min(r => r.X);
				var y2 = partition.Max(r => r.Y + r.Height);
				var x2 = partition.Max(r => r.X + r.Width);
				merged.Add(new Rectangle(x1, y1, x2 - x1, y2 - y1));
			}

			return merged;
		}
		
		/// <summary>
		/// Determines the distance between two groups of rectangles. For more dynamic usage, provide a distance
		/// calculating function like euclidean, manhattan or chebyshev and an appropriate maxValue, since manhattan
		/// and chebyshev are providing integer values when using integer coordinates, but euclidean usually provides
		/// a floating point number. 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="algorithm"></param>
		/// <param name="maxValue"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		private static T Distance<T>(IEnumerable<Rectangle> p1, IEnumerable<Rectangle> p2, Func<int, int, T> algorithm, T maxValue)
		{
			var r1 = p1.ToList();
			var r2 = p2.ToList();
			if (!r1.Any() || !r2.Any())
			{
				return maxValue;
			}
			var p1Y1 = r1.Min(r => r.Y);
			var p1X1 = r1.Min(r => r.X);
			var p1Y2 = r1.Max(r => r.Y + r.Height);
			var p1X2 = r1.Max(r => r.X + r.Width);

			var p2Y1 = r2.Min(r => r.Y);
			var p2X1 = r2.Min(r => r.X);
			var p2Y2 = r2.Max(r => r.Y + r.Height);
			var p2X2 = r2.Max(r => r.X + r.Width);

			var diffY = 0;
			var diffX = 0;


			//if ((p1X1 < p2X1 || p1X1 > p2X2) && (p1X2 < p2X1 || p1X2 > p2X2))
			if (!p1X1.IsInRange(p2X1, p2X2) && !p1X2.IsInRange(p2X1, p2X2) &&
			    !p2X1.IsInRange(p1X1, p1X2) && !p2X2.IsInRange(p1X1, p1X2))
			{
				diffX = Math.Min(Math.Abs(Math.Min(p1X1, p1X2) - Math.Max(p2X1, p2X2)),
					Math.Abs(Math.Min(p2X1, p2X2) - Math.Max(p1X1, p1X2)));
			}

			//if ((p1Y1 < p2Y1 || p1Y1 > p2Y2) && (p1Y2 < p2Y1 || p1Y2 > p2Y2))
			if (!p1Y1.IsInRange(p2Y1, p2Y2) && !p1Y2.IsInRange(p2Y1, p2Y2) &&
			    !p2Y1.IsInRange(p1Y1, p1Y2) && !p2Y2.IsInRange(p1Y1, p1Y2))
			{
				diffY = Math.Min(Math.Abs(Math.Min(p1Y1, p1Y2) - Math.Max(p2Y1, p2Y2)),
					Math.Abs(Math.Min(p2Y1, p2Y2) - Math.Max(p1Y1, p1Y2)));
			}

			return algorithm.Invoke(diffX, diffY);
		}

		/// <summary>
		/// Shorthand and easier to read variant to check if a value is between given bounds.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="lowerBound"></param>
		/// <param name="upperBound"></param>
		/// <returns></returns>
		private static bool IsInRange(this int value, int lowerBound, int upperBound)
		{
			return value >= lowerBound && value <= upperBound;
		}
	}
}