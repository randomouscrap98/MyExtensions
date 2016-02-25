using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Drawing;
using MyExtensions;

namespace MyExtensions
{
	public class HillGenerator
	{
		public static float[,] Generate(HillGeneratorOptions options)
		{
			int width = options[HillGeneratorOptions.IntNames.width];
			int height = options[HillGeneratorOptions.IntNames.height];
			int minRadius = options[HillGeneratorOptions.IntNames.minimumRadius];
			int maxRadius = options[HillGeneratorOptions.IntNames.maximumRadius];
			int noise = options[HillGeneratorOptions.IntNames.noise];
			bool island = options[HillGeneratorOptions.BoolNames.island];

			float[,] map = new float[width, height];
			Random random = new Random(options[HillGeneratorOptions.IntNames.randomSeed]);

			int radius = 0;
			int circleX = 0;
			int circleY = 0;

			for (int i = options[HillGeneratorOptions.IntNames.hillsToGenerate]; i > 0; i--)
			{
				radius = random.Next(minRadius, maxRadius + 1);

				//Compute circle info
				if (island)
				{
					if (radius > height / 2)
						radius = height / 2;

					double theta = random.Next(100000) * 2 * Math.PI / 100000;
					int distance = random.Next((int)Math.Sqrt(height / 2 - radius), height / 2 - radius);

					circleX = (int)(height / 2 + Math.Cos(theta) * distance);
					circleY = (int)(height / 2 + Math.Sin(theta) * distance);
				}
				else
				{
					circleX = random.Next(-radius, width + radius + 1);
					circleY = random.Next(-radius, height + radius + 1);
				}

				//Get range of circle
				int minX = circleX - radius - 1;
				int maxX = circleX + radius + 1;
				int minY = circleY - radius - 1;
				int maxY = circleY + radius + 1;

				//Fix range if out of map field
				if (minX < 0)
					minX = 0;
				if (maxX > width - 1)
					maxX = width - 1;
				if (minY < 0)
					minY = 0;
				if (maxY > height - 1)
					maxY = height - 1;

				//Raise bump
				for (int x = minX; x <= maxX; x++)
				{
					for (int y = minY; y <= maxY; y++)
					{
						float value = radius * radius - (circleX - x) * (circleX - x) - (circleY - y) * (circleY - y);

						if (noise > 0)
							value *= (1.0f + ((1 - 2 * random.Next(2)) * random.Next(noise + 1)) / 100f);

						if (value >= 0)
							map[x, y] += value;
					}
				}
			}

			map.Normalize();

			for (int x = 0; x < map.GetLength(0); x++)
				for (int y = 0; y < map.GetLength(1); y++)
					map[x, y] += (options[HillGeneratorOptions.IntNames.level] / 100.0f) * (1 - map[x, y]);

			if (options[HillGeneratorOptions.BoolNames.flatten])
				map.Apply(ArrayExtensions.Squared);

			return map;
		}

		public static Bitmap Preview(float[,] map)
		{
			Bitmap image = new Bitmap(map.GetLength(0), map.GetLength(1));

			for (int i = 0; i < image.Width; i++)
				for (int j = 0; j < image.Height; j++)
					image.SetPixel(i, j, Color.FromArgb(255, (int)(255 * map[i, j]), (int)(255 * map[i, j]), (int)(255 * map[i, j])));

			return image;
		}

		public static Bitmap Colorize(float[,] map, List<Tuple<int, Color>> colorValues)
		{
			Bitmap image = new Bitmap(map.GetLength(0), map.GetLength(1));

			int total = colorValues.Sum(x => x.Item1);

			List<Tuple<float, Color>> colorRange = colorValues.Where(x => x.Item1 > 0).Select(x => new Tuple<float, Color>((float)x.Item1 / total, x.Item2)).ToList();

			for (int i = 0; i < map.GetLength(0); i++)
			{
				for (int j = 0; j < map.GetLength(1); j++)
				{
					float testVal = map[i, j];
					for (int k = 0; k < colorRange.Count; k++)
					{
						testVal -= colorRange[k].Item1;
						if (testVal <= 0 || k == colorRange.Count - 1)
						{
							image.SetPixel(i, j, colorRange[k].Item2);
							break;
						}
					}
				}
			}

			return image;
		}
	}

	public class HillGeneratorOptions
	{
		//The available options (names)
		public enum IntNames
		{
			randomSeed,
			minimumRadius,
			maximumRadius,
			hillsToGenerate,
			noise,
			level,
			width,
			height
		}
		public enum BoolNames
		{
			island,
			flatten
		}

		//The actual data we're holding in this class
		private Dictionary<IntNames, int> intOptions;
		private Dictionary<BoolNames, bool> boolOptions;

		//Some data to "cache"
		private static readonly Type IntNamesType = typeof(IntNames);
		private static readonly Type BoolNamesType = typeof(BoolNames);
		private static readonly Array IntNamesValues = Enum.GetValues(IntNamesType);
		private static readonly Array BoolNamesValues = Enum.GetValues(BoolNamesType);

		//Note: If seed parsing fails, no values are changed
		public HillGeneratorOptions(string seed)
			: this()
		{
			DecomposeSeed(seed);
		}

		//Note: Can be empty. If options are crazy, they are fixed
		public HillGeneratorOptions(Dictionary<IntNames, int> IntOptions, Dictionary<BoolNames, bool> BoolOptions)
			: this()
		{
			foreach (IntNames name in IntOptions.Keys)
				intOptions[name] = IntOptions[name];

			foreach (BoolNames name in BoolOptions.Keys)
				BoolOptions[name] = BoolOptions[name];

			FixOptions();
		}

		//Sets generator to default values
		public HillGeneratorOptions()
		{
			intOptions = new Dictionary<IntNames, int>();
			boolOptions = new Dictionary<BoolNames, bool>();

			//This is really the only place where each option is enumerated individually
			intOptions.Add(IntNames.randomSeed, 559321);
			intOptions.Add(IntNames.minimumRadius, 4);
			intOptions.Add(IntNames.maximumRadius, 16);
			intOptions.Add(IntNames.hillsToGenerate, 400);
			intOptions.Add(IntNames.noise, 10);
			intOptions.Add(IntNames.level, 0);
			intOptions.Add(IntNames.width, 256);
			intOptions.Add(IntNames.height, 256);

			boolOptions.Add(BoolNames.island, false);
			boolOptions.Add(BoolNames.flatten, false);
		}

		//Fix values of options that are crazy
		private void FixOptions()
		{
			//Make everything positive
			foreach (IntNames name in intOptions.Keys.ToList())
				if (intOptions[name] < 0)
					intOptions[name] = 1;

			//Fix radius settings
			if (intOptions[IntNames.maximumRadius] < intOptions[IntNames.minimumRadius])
				intOptions[IntNames.maximumRadius] = intOptions[IntNames.minimumRadius];

			//Fix noise level settings (should only be 100)
			if (intOptions[IntNames.noise] > 100)
				intOptions[IntNames.noise] = 100;
		}

		//Set values based on given seed string
		private bool DecomposeSeed(string seed)
		{
			seed = seed.Replace(".", "");
			seed = seed.Replace("/", "");
			seed = seed.Replace("-", "");
			seed = seed.Replace(" ", "");

			//Oops, can't decompose the string if it's not the right length!
			if (seed.Length != Seed.Length)
				return false;

			Dictionary<IntNames, int> tempInts = new Dictionary<IntNames, int>();
			Dictionary<BoolNames, bool> tempBools = new Dictionary<BoolNames, bool>();
			System.Globalization.NumberStyles hex = System.Globalization.NumberStyles.HexNumber;
			IFormatProvider info = CultureInfo.InvariantCulture;
			int i = 0;

			//Try to parse out all the integers
			foreach (IntNames name in IntNamesValues)
			{
				int tempInt = 0;

				if (!int.TryParse(seed.Substring(i * 8, 8), hex, info, out tempInt))
					return false;

				tempInts.Add(name, tempInt);

				i++;
			}

			//Reset counter
			i = 0;

			//Try to parse out all the booleans
			foreach (BoolNames name in BoolNamesValues)
			{
				bool tempBool = false;

				string tester = seed.Substring(IntOptionsCount * 8 + i, 1);

				if (tester == "0")
					tester = "false";
				else if (tester == "1")
					tester = "true";

				if (!bool.TryParse(tester, out tempBool))
					return false;

				tempBools.Add(name, tempBool);

				i++;
			}

			//OK, everything is fine. Assign away!
			foreach (IntNames name in IntNamesValues)
				intOptions[name] = tempInts[name];
			foreach (BoolNames name in BoolNamesValues)
				boolOptions[name] = tempBools[name];

			return true;
		}

		//Build seed string based on internal values
		private string BuildSeed()
		{
			string seed = "";

			foreach (IntNames name in IntNamesValues)
				seed += intOptions[name].ToString("X8");

			foreach (BoolNames name in BoolNamesValues)
				seed += Convert.ToInt16(boolOptions[name]).ToString("X1");

			return seed;
		}

		//Accessors and junk
		public string Seed
		{
			get { return BuildSeed(); }
		}
		public int IntOptionsCount
		{
			get { return Enum.GetValues(typeof(IntNames)).Length; }
		}
		public int BoolOptionsCount
		{
			get { return Enum.GetValues(typeof(BoolNames)).Length; }
		}
		public int this[IntNames name]
		{
			get { return intOptions[name]; }
			set { intOptions[name] = value; FixOptions(); }
		}
		public bool this[BoolNames name]
		{
			get { return boolOptions[name]; }
			set { boolOptions[name] = value; FixOptions(); }
		}
	}
}
