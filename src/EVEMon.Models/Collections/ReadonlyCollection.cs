using System.Collections;
using System.Collections.Generic;

namespace EVEMon.Common.Collections
{
    /// <summary>
    /// Represents a base readonly collection relying on an inner list.
    /// </summary>
    public abstract class ReadonlyCollection<T> : IReadonlyCollection<T>
    {
        protected FastList<T> Items;

        /// <summary>
        /// Protected default constructor with an initial capacity.
        /// </summary>
        protected ReadonlyCollection(int capacity)
        {
            Items = new FastList<T>(capacity);
        }

        /// <summary>
        /// Protected default constructor
        /// </summary>
        protected ReadonlyCollection()
        {
            Items = new FastList<T>(0);
        }

        /// <summary>
        /// Gets the number of items in this collection
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Gets true if the collection contains the given item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item) => Items.Contains(item);


        #region Enumerators

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        #endregion
    }
}