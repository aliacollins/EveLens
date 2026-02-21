// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

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