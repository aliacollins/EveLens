using System.Collections.Generic;

namespace EVEMon.Common.Collections
{
    /// <summary>
    /// Represents a read-only collection
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public interface IReadonlyKeyedCollection<in TKey, out TItem> : IEnumerable<TItem>
    {
        int Count { get; }
        TItem this[TKey key] { get; }
    }
}