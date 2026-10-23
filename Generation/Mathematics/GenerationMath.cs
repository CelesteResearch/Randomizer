using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Celeste.Mods.Randomizer.Generation.Mathematics
{
    internal static class GenerationMath
    {
        public static float NextFloat(this Random random) => (float)random.NextDouble();
        public static float NextFloat(this Random random, float maximum) => random.NextFloat() * maximum;

        public static float NextFloat(this Random random, float minimum, float maximum) =>
            minimum + random.NextFloat(maximum - minimum);

        public static T Choose<T>(this Random random, params T[] choices) => choices[random.Next(choices.Length)];
        public static T Choose<T>(this Random random, IList<T> choices) => choices[random.Next(choices.Count)];
        public static T Choose<T>(this Random random, List<T> choices) => choices[random.Next(choices.Count)];

        public static void Shuffle<T>(this IList<T> values, Random random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swap = random.Next(index + 1);
                T current = values[index];
                values[index] = values[swap];
                values[swap] = current;
            }
        }

        public static string IntGridToCSV(int[,] grid)
        {
            var result = new StringBuilder();
            var row = new List<int>();
            int pendingRows = 0;
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                int pendingEmpty = 0;
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    if (grid[x, y] == -1)
                    {
                        pendingEmpty++;
                        continue;
                    }

                    for (int index = 0; index < pendingRows; index++)
                        result.Append('\n');
                    for (int index = 0; index < pendingEmpty; index++)
                        row.Add(-1);
                    pendingEmpty = pendingRows = 0;
                    row.Add(grid[x, y]);
                }

                if (row.Count > 0)
                {
                    result.Append(string.Join(",", row));
                    row.Clear();
                }

                pendingRows++;
            }

            return result.ToString();
        }
    }
}
