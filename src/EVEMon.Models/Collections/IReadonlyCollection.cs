using System.Collections.Generic;

namespace EVEMon.Common.Collections
{
    /// <summary>
    /// Represents a read-only collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadonlyCollection<out T> : IEnumerable<T>
    {
        int Count { get; }
    }
}