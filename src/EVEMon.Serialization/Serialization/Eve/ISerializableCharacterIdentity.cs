namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a set of informations required to create an identity.
    /// </summary>
    public interface ISerializableCharacterIdentity
    {
        long ID { get; }
        string? Name { get; }
        long CorporationID { get; }
        string? CorporationName { get; }
        long AllianceID { get; }
        string? AllianceName { get; }
        int FactionID { get; }
        string? FactionName { get; }
    }
}