// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections;
using System.Collections.Generic;
using EveLens.Common.Extensions;

namespace EveLens.Common.Collections
{
    /// <summary>
    /// Implements an as fast and light list as possible. No checks are performed, no synchronization possibilities, etc, etc, etc.
    /// </summary>
    /// <remarks>
    /// Beware ! This list is a struct. Resizing it (or adding items and such) may mean to change the array and, therefore, the structure.
    /// Also, this structure *must* be initialized with the custom capacity-based constructor to avoid <see cref="NullReferenceException"/>.
    /// Use when you're sure about what you're doing.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public struct FastList<T> : IList<T>
    {
        #region Enumerator

        /// <summary>
        /// An enumerator for the FastList.
        /// </summary>
        private struct Enumerator : IEnumerator<T>
        {
            private T[] m_items;
            private int m_index;
            private readonly int m_count;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="items"></param>
            /// <param name="count"></param>
            public Enumerator(T[] items, int count)
            {
                m_items = items;
                m_count = count;
                m_index = -1;
            }

            /// <summary>
            /// Gets the current.
            /// </summary>
            /// <value>The current.</value>
            public T Current => m_items[m_index];

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                m_items = null;
            }

            /// <summary>
            /// Gets the current.
            /// </summary>
            /// <value>The current.</value>
            object IEnumerator.Current => m_items[m_index];

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public bool MoveNext()
            {
                m_index++;
                return m_index < m_count;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public void Reset()
            {
                m_index = -1;
            }
        }

        #endregion


        private T[] m_items;

        /// <summary>
        /// Constructor with a starting capacity. Always use this constructor, not the default one !
        /// </summary>
        /// <param name="capacity">Initial capacity of the list</param>
        public FastList(int capacity)
            : this()
        {
            m_items = new T[capacity];
            Count = 0;
        }

        /// <summary>
        /// Gets the number of items within this list.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets or sets the current capacity of the list.
        /// </summary>
        public int Capacity
        {
            get { return m_items.Length; }
            set
            {
                if (value < Count)
                    throw new ArgumentException("The given count is lesser than the items in the list.");

                Array.Resize(ref m_items, value);
            }
        }

        /// <summary>
        /// Gets / sets the item at the given index.
        /// </summary>
        /// <param name="index">The index where the item is located</param>
        /// <returns>The item found at the given index</returns>
        public T this[int index]
        {
            get { return m_items[index]; }
            set { m_items[index] = value; }
        }

        /// <summary>
        /// Gets the index of an item within the list.
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>The index where the item was found, -1 otherwise</returns>
        public int IndexOf(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(m_items[i], item))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Adds an item at the end of the list.
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(T item)
        {
            // We ensure the capacity is high enough
            if (Count == m_items.Length)
            {
                int newSize = Math.Max(1, Count + 1);
                Capacity = newSize;
            }

            // We add the item
            m_items[Count] = item;

            // Finally, we increase the count
            Count++;
        }

        /// <summary>
        /// Add items at the end of the list.
        /// </summary>
        /// <param name="enumerable">The enumeration containing the items to add</param>
        /// <exception cref="System.ArgumentNullException">enumerable</exception>
        public void AddRange(IEnumerable<T> enumerable)
        {
            enumerable.ThrowIfNull(nameof(enumerable));

            // Scroll through the items to add
            foreach (T item in enumerable)
            {
                // Ensure size is enough
                if (Count == m_items.Length)
                {
                    int newSize = Math.Max(1, Count + 1);
                    Array.Resize(ref m_items, newSize);
                }

                // We add the item
                m_items[Count] = item;

                // Finally, we increase the count
                Count++;
            }
        }

        /// <summary>
        /// Add items at the end of the list.
        /// </summary>
        /// <param name="list">The list containing the items to add</param>
        public void AddRange(FastList<T> list)
        {
            AddRange(list.m_items, list.Count);
        }

        /// <summary>
        /// Add items at the end of the list.
        /// </summary>
        /// <param name="newItems">The array containing the items to add</param>
        /// <param name="newCount">The number of items to add</param>
        private void AddRange(T[] newItems, int newCount)
        {
            // We ensure the capacity is high enough
            if (Count + newCount > m_items.Length)
            {
                int newSize = Math.Max(Count + newCount, Count + 1);
                Array.Resize(ref m_items, newSize);
            }

            // Append items
            Array.Copy(newItems, 0, m_items, Count, newCount);

            // Finally, we increase the count
            Count += newCount;
        }

        /// <summary>
        /// Insert an item at the given index.
        /// </summary>
        /// <param name="index">The item to insert</param>
        /// <param name="item">The index to insert the item in</param>
        public void Insert(int index, T item)
        {
            // We ensure the capacity is high enough
            if (Count == m_items.Length)
            {
                int newSize = Math.Max(1, Count + 1);
                Array.Resize(ref m_items, newSize);
            }

            // Do we need to shift items before ? (insertion rather than addition)
            if (index < Count)
                Array.Copy(m_items, index, m_items, index + 1, Count - index);
            m_items[index] = item;

            // Finally, we increase the count
            Count++;
        }

        /// <summary>
        /// Removes the item located at the specified index.
        /// </summary>
        /// <param name="index">The index to remove the item from</param>
        public void RemoveAt(int index)
        {
            // When item is not the last item, shift the items after it to the left
            if (index < Count - 1)
                Array.Copy(m_items, index + 1, m_items, index, Count - (index + 1));

            // Make sure we don't hold a reference anymore over the left item
            m_items[Count - 1] = default(T);

            // Updates the count
            Count--;
        }

        /// <summary>
        /// Removes the specified item from the list.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if the item was found, false otherwise</returns>
        public bool Remove(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < Count; i++)
            {
                if (!comparer.Equals(m_items[i], item))
                    continue;

                RemoveAt(i);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            Array.Clear(m_items, 0, Count);
            Count = 0;
        }

        /// <summary>
        /// Checks whether a given item is present in the list.
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>True if the item was found in this list, false otherwise</returns>
        public bool Contains(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(m_items[i], item))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Copy those list's items in the specified array.
        /// </summary>
        /// <param name="array">The destination array</param>
        /// <param name="arrayIndex">The starynig index in the destination array</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(m_items, 0, array, arrayIndex, Count);
        }

        /// <summary>
        /// Reverses the list.
        /// </summary>
        public void Reverse()
        {
            Array.Reverse(m_items);
        }

        /// <summary>
        /// Gets an enumerator over this list.
        /// </summary>
        /// <returns>The enumerator over this list</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (m_items == null || Count == 0)
                return new Enumerator();
            return new Enumerator(m_items, Count);
        }

        /// <summary>
        /// Gets a non-generic enumerator over this list.
        /// </summary>
        /// <returns>The enumerator over this list</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (m_items == null || Count == 0)
                return new Enumerator();
            return new Enumerator(m_items, Count);
        }

        /// <summary>
        /// Gets false, since the list is not readonly.
        /// </summary>
        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Trims the list.
        /// </summary>
        public void Trim()
        {
            Array.Resize(ref m_items, Count);
        }
    }
}