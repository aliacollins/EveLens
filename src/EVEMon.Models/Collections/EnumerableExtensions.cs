using System;
using System.Collections.Generic;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Collections
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Uses an insertion sort algorithm to perform a stable sort (keep the initial order of the keys with equal values).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="comparer">The comparer.</param>
        /// <remarks>Memory overhead is null, average complexity is O(n.ln(n)), worst-case is O(n²).</remarks>
        public static void StableSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            list.StableSort(comparer.Compare);
        }

        /// <summary>
        /// Uses an insertion sort algorithm to perform a stable sort (keep the initial order of the keys with equal values).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="comparison">The comparison.</param>
        /// <remarks>Memory overhead is null, average complexity is O(n.ln(n)), worst-case is O(n²).</remarks>
        /// <exception cref="System.ArgumentNullException">list or comparison</exception>
        public static void StableSort<T>(this IList<T> list, Comparison<T> comparison)
        {
            list.ThrowIfNull(nameof(list));

            comparison.ThrowIfNull(nameof(comparison));

            // For every key
            for (int i = 1; i < list.Count; i++)
            {
                T value = list[i];
                int j = i - 1;

                // Move the key backward while the previous items are lesser than it, shifting those items to the right
                while (j >= 0 && comparison(list[j], value) > 0)
                {
                    list[j + 1] = list[j];
                    j--;
                }

                // Insert at the left of the scrolled sequence, immediately on the right of the first lesser or equal value it found
                list[j + 1] = value;
            }
        }

        /// <summary>
        /// Gets the index of the given element in this enumeration, or -1 when the item is absent from the enumeration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">src</exception>
        public static int IndexOf<T>(this IEnumerable<T> src, T item)
        {
            src.ThrowIfNull(nameof(src));

            int index = 0;
            foreach (T srcItem in src)
            {
                if (Equals(item, srcItem))
                    return index;
                index++;
            }
            return -1;
        }
    }
}