namespace EveLens.XmlGenerator.Interfaces
{
    /// <summary>
    /// Implementors have an ID attribute
    /// </summary>
    public interface IHasID
    {
        int ID { get; }
    }
}