using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyExtensions
{
	public static class ArrayExtensions
	{
		public static void Normalize(this float[,] map)
		{
			float min = float.MaxValue;
			float max = float.MinValue;
			int width = map.GetLength(0);
			int height = map.GetLength(1);

			//Calculate min and max
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					if (map[i, j] < min)
						min = map[i, j];
					if (map[i, j] > max)
						max = map[i, j];
				}
			}

			//Preshift max down to the real maximum value
			max -= min;

			//We can't normalize if the maximum value is 0 (after shifting)
			if (max == 0)
				return;

			//Perform normalization
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
					map[i, j] = (map[i, j] - min) / max;
		}

		public static void Apply(this float[,] map, Func<float, float> function)
		{
			int width = map.GetLength(0);
			int height = map.GetLength(1);

			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
					map[i, j] = function(map[i, j]);
		}

		public static void Fill<T>(this T[] singleDimensionArray, T value)
		{
			int size = singleDimensionArray.GetLength(0);
			for (int i = 0; i < size; i++)
			{
				singleDimensionArray[i] = value;
			}
		}

		//private static Tuple<int, T> FindMinOrMax<T>(this T[] findArray, bool max)
		//{
		//	int minIndex = 0;
		//	int maxIndex = 0;
		//	T minValue, maxValue;

		//	if (minValue is int)
		//	{
		//		minValue = int.MaxValue;
		//		maxValue = int.MinValue;
		//	}

		//}

		public static Func<float, float> Squared = x => x * x;
		public static Func<float, float> SquareRoot = x => (float)Math.Sqrt((double)x);
	}
}
