using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EVEMon.Common.Collections
{
    /// <summary>
    /// Represents a base readonly collection relying on a method override providing an <see cref="IEnumerable{T}"/>.
    /// </summary>
    public abstract class ReadonlyVirtualCollection<T> : IReadonlyCollection<T>
    {
        /// <summary>
        /// The core method to implement : all other methods rely on this one.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<T> Enumerate();

        /// <summary>
        /// Gets the number of items in this collection
        /// </summary>
        public int Count => Enumerate().Count();


        #region Enumerators

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Enumerate().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Enumerate().GetEnumerator();

        #endregion
    }
}