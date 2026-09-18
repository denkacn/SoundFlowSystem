using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundFlowSystem.Tools
{
    public static class EnumerableExtension
    {
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var items = source as IList<T> ?? source.ToArray();
            if (items.Count == 0) throw new InvalidOperationException("Cannot choose from an empty collection.");
            return items[UnityEngine.Random.Range(0, items.Count)];
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var items = source.ToArray();
            for (int i = items.Length - 1; i > 0; i--)
            {
                int other = UnityEngine.Random.Range(0, i + 1);
                var value = items[i];
                items[i] = items[other];
                items[other] = value;
            }
            return items;
        }
    }
}
