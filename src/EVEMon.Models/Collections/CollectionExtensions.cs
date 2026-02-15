using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Collections
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="System.Collections.ObjectModel.Collection{T}" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="values">The values.</param>
        /// <exception cref="System.ArgumentNullException">collection or values</exception>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> values)
        {
            collection.ThrowIfNull(nameof(collection));

            values.ThrowIfNull(nameof(values));

            foreach (T item in values)
            {
                collection.Add(item);
            }
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the first occurrence within the entire <see cref="System.Collections.ObjectModel.Collection{T}" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">collection</exception>
        public static T Find<T>(this ICollection<T> collection, Predicate<T> predicate)
        {
            collection.ThrowIfNull(nameof(collection));

            foreach (T item in collection.Where(item => predicate(item)))
            {
                return item;
            }
            return default(T);
        }
    }
}