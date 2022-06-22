using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NearDuplicateDetection
{
    public static class ComparisonExtensions
    {
        public static double Similar<T>(this IList<T> first, IList<T> second)
        {
            var size = first.Count > second.Count ? second.Count : first.Count;
            var counter = 0;
            for (int i = 0; i < size; i++)
            {
                if (first[i].Equals(second[i]))
                {
                    counter++;
                }
            }

            return (double)counter / (double)(first.Count < second.Count ? second.Count : first.Count);
        }
        public static string ToName<T>(this List<T> original)
        {
            return String.Join("", original.Select(x => x.ToString()));
        }

        public static List<T> Reduce<T>(this List<T> original)
        {
            var count = original.Count;
            if ((count & (count - 1)) != 0)
            {
                throw new Exception("not a power of 2");
            }
            var side = (int)Math.Floor(Math.Sqrt(count));
            if (side % 2 != 0)
            {
                throw new Exception("side size is not divisible by 2");
            }
            var newSide = side / 2;

            var result = new List<T>(newSide * newSide);

            for (int i = 0; i < newSide; i++)
            {
                for (int j = 0; j < newSide; j++)
                {
                    var d = new Dictionary<T, int>();
                    Add(original, d, i, j, side);
                    result.Add(d.OrderByDescending(x => x.Value).ThenBy(x => x.Value).First().Key);
                }
            }

            return result;
        }

        private static void Add<T>(List<T> original, Dictionary<T, int> d, int i, int j, int side)
        {
            AddValue(original, d, i * side * 2 + j * 2);
            AddValue(original, d, i * side * 2 + j * 2 + 1);
            AddValue(original, d, i * side * 2 + j * 2 + side);
            AddValue(original, d, i * side * 2 + j * 2 + side + 1);
        }

        private static void AddValue<T>(List<T> original, Dictionary<T, int> d, int index)
        {
            var value = original[index];
            if (d.ContainsKey(value))
            {
                d[value]++;
            }
            else
            {
                d[value] = 1;
            }
        }
    }
}
