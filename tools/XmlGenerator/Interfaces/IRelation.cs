namespace EveLens.XmlGenerator.Interfaces
{
    /// <summary>
    /// Implementors support two components, a left hand side and a right hand side that are related.
    /// </summary>
    public interface IRelation
    {
        /// <summary>
        /// Gets the left column value.
        /// </summary>
        /// <value>The left.</value>
        int Left { get; }

        /// <summary>
        /// Gets the center column value.
        /// </summary>
        /// <value>
        /// The center.
        /// </value>
        int Center { get; }

        /// <summary>
        /// Gets the right column value.
        /// </summary>
        /// <value>
        /// The right.
        /// </value>
        int Right { get; }
    }
}