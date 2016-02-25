using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyExtensions
{
	//Only 2 dimensional (for now)
	//Also only 2 states
	public class CellularAutomaton
	{
		public const int MaxStates = byte.MaxValue;

		public enum BorderType
		{
			Solid,
			Empty,
			Normal
		}

		//Not very efficient memory, but I'm "naive"
		private int swapSection = 0;
		private byte[, ,] swapGrid;
		private Random random;
		private static Random randomSeeds = new Random();
		private readonly object SeedLocker = new object();

		public CellularAutomaton(int width, int height, int seed = 0)
		{
			swapGrid = new byte[width + 2, height + 2, 2];

			if (seed != 0)
			{
				random = new Random(seed);
			}
			else
			{
				lock (SeedLocker)
				{
					random = new Random(randomSeeds.Next());
				}
			}
		}

		public void Simulate(List<int> surviveStates, List<int> bornStates, BorderType border, double fillPercent, int simulations)
		{
			swapSection = 0;
			
			//Initialize
			for (int i = 0; i < swapGrid.GetLength(0); i++)
				for (int j = 0; j < swapGrid.GetLength(1); j++)
					swapGrid[i, j, swapSection] = (byte)(random.NextDouble() <= fillPercent ? 1 : 0);

			//Fix up edges
			for (int i = 0; i < swapGrid.GetLength(0); i++)
			{
				if (border == BorderType.Empty)
				{
					swapGrid[i, 0, swapSection] = 0;
					swapGrid[i, swapGrid.GetLength(1) - 1, swapSection] = 0;
				}
				else if (border == BorderType.Solid)
				{
					swapGrid[i, 0, swapSection] = 1;
					swapGrid[i, swapGrid.GetLength(1) - 1, swapSection] = 1;
				}
			}
			for (int i = 0; i < swapGrid.GetLength(1); i++)
			{
				if (border == BorderType.Empty)
				{
					swapGrid[0, i, swapSection] = 0;
					swapGrid[swapGrid.GetLength(0) - 1, i, swapSection] = 0;
				}
				else if (border == BorderType.Solid)
				{
					swapGrid[0, i, swapSection] = 1;
					swapGrid[swapGrid.GetLength(0) - 1, i, swapSection] = 1;
				}
			}

			//Simulate
			for (int i = 0; i < simulations; i++)
			{
				Parallel.For(1, swapGrid.GetLength(0) - 1, x =>
				{
					for (int y = 1; y < swapGrid.GetLength(1) - 1; y++)
					{
						int alives = 0;
						for (int xx = -1; xx <= 1; xx++)
							for (int yy = -1; yy <= 1; yy++)
								if (!(xx == 0 && yy == 0) && swapGrid[x + xx, y + yy, swapSection] > 0)
									alives++;

						//Swap states if necessary
						if (swapGrid[x, y, swapSection] == 0 && bornStates.Contains(alives) ||
							swapGrid[x, y, swapSection] == 1 && !surviveStates.Contains(alives))
							swapGrid[x, y, (swapSection + 1) % 2] = (byte)((swapGrid[x, y, swapSection] + 1) % 2);
						else
							swapGrid[x, y, (swapSection + 1) % 2] = swapGrid[x, y, swapSection];
					}
				});

				swapSection = (swapSection + 1) % 2;
			}
		}

		public byte[,] GetGrid()
		{
			byte[,] realGrid = new byte[swapGrid.GetLength(0) - 2, swapGrid.GetLength(1) - 2];

			for (int i = 1; i < swapGrid.GetLength(0) - 1; i++)
				for (int j = 1; j < swapGrid.GetLength(1) - 1; j++)
					realGrid[i - 1, j - 1] = swapGrid[i, j, swapSection];

			return realGrid;
		}

		public Bitmap GetImage(int expand = 1)
		{
			byte[,] data = GetGrid();
			Bitmap image = new Bitmap(data.GetLength(0) * expand, data.GetLength(1) * expand);

			using (Graphics g = Graphics.FromImage(image))
			{
				for (int i = 0; i < data.GetLength(0); i++)
					for (int j = 0; j < data.GetLength(1); j++)
						g.FillRectangle((data[i, j] == 1 ? Brushes.Black : Brushes.Gray), i * expand, j * expand, expand, expand);
						//image.SetPixel(i, j, (data[i, j] == 1 ? Color.Black : Color.White));
			}

			return image;
		}
	}
}
