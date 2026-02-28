// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.CloudStorageServices;
using EveLens.Common.Scheduling;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Helpers
{
    #region JSON Data Classes

    /// <summary>
    /// Combined backup format - all settings in one file for export/import.
    /// </summary>
    public class JsonBackup
    {
        public int Version { get; set; } = 1;
        public string? ForkId { get; set; }
        public string? ForkVersion { get; set; }
        public DateTime ExportedAt { get; set; }
        public JsonConfig? Config { get; set; }
        public JsonCredentials? Credentials { get; set; }
        public List<JsonCharacterData>? Characters { get; set; } = new List<JsonCharacterData>();
        public List<long>? MonitoredCharacterIds { get; set; } = new List<long>();
    }

    /// <summary>
    /// Root config.json structure - UI settings and preferences.
    /// </summary>
    public class JsonConfig
    {
        public int Version { get; set; } = 1;
        public string? ForkId { get; set; } = "aliacollins";
        public string? ForkVersion { get; set; }
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        // Metadata for round-trip preservation
        public int Revision { get; set; }
        public string? Compatibility { get; set; }
        public string? EsiScopePreset { get; set; }
        public List<string>? EsiCustomScopes { get; set; }
        public List<JsonCharacterGroupSettings>? CharacterGroups { get; set; }

        // SSO credentials (custom overrides persisted from user settings)
        public string? SSOClientID { get; set; }
        public string? SSOClientSecret { get; set; }

        // Settings objects
        public UISettings? UI { get; set; }
        public G15Settings? G15 { get; set; }
        public ProxySettings? Proxy { get; set; }
        public UpdateSettings? Updates { get; set; }
        public CalendarSettings? Calendar { get; set; }
        public ExportationSettings? Exportation { get; set; }
        public MarketPricerSettings? MarketPricer { get; set; }
        public NotificationSettings? Notifications { get; set; }
        public LoadoutsProviderSettings? LoadoutsProvider { get; set; }
        public PortableEveInstallationsSettings? PortableEveInstallations { get; set; }
        public CloudStorageServiceProviderSettings? CloudStorageServiceProvider { get; set; }
        public SchedulerSettings? Scheduler { get; set; }
    }

    /// <summary>
    /// Character group settings for config.json.
    /// </summary>
    public class JsonCharacterGroupSettings
    {
        public string? Name { get; set; }
        public List<Guid> CharacterGuids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// Root credentials.json structure - ESI authentication tokens.
    /// </summary>
    public class JsonCredentials
    {
        public int Version { get; set; } = 1;
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;
        public List<JsonEsiKey> EsiKeys { get; set; } = new List<JsonEsiKey>();
    }

    /// <summary>
    /// ESI key data for credentials.json.
    /// </summary>
    public class JsonEsiKey
    {
        public long CharacterId { get; set; }
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Legacy bitflag access mask. Kept for backward-compatible deserialization.
        /// </summary>
        [Obsolete("Use AuthorizedScopes instead.")]
        public ulong AccessMask { get; set; }

        public bool Monitored { get; set; }

        /// <summary>
        /// ESI scope strings that were granted when this key was authenticated.
        /// </summary>
        public List<string> AuthorizedScopes { get; set; } = new();
    }

    /// <summary>
    /// Character index structure - lightweight list of all characters.
    /// </summary>
    public class JsonCharacterIndex
    {
        public int Version { get; set; } = 1;
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;
        public List<JsonCharacterIndexEntry> Characters { get; set; } = new List<JsonCharacterIndexEntry>();
        public List<long> MonitoredCharacterIds { get; set; } = new List<long>();
    }

    /// <summary>
    /// Lightweight character entry for the index.
    /// </summary>
    public class JsonCharacterIndexEntry
    {
        public long CharacterId { get; set; }
        public string? Name { get; set; }
        public string? CorporationName { get; set; }
        public string? AllianceName { get; set; }
        public bool IsUriCharacter { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Full character data structure — the in-memory DTO.
    /// Component split/merge is purely an I/O concern.
    /// </summary>
    public class JsonCharacterData
    {
        public int Version { get; set; } = 1;
        public long CharacterId { get; set; }
        public Guid Guid { get; set; }
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        // Character identity
        public string? Name { get; set; }
        public DateTime Birthday { get; set; }
        public string? Race { get; set; }
        public string? Bloodline { get; set; }
        public string? Ancestry { get; set; }
        public string? Gender { get; set; }

        // Corporation/Alliance
        public long CorporationId { get; set; }
        public string? CorporationName { get; set; }
        public long AllianceId { get; set; }
        public string? AllianceName { get; set; }
        public long FactionId { get; set; }
        public string? FactionName { get; set; }

        // Attributes
        public int Intelligence { get; set; }
        public int Memory { get; set; }
        public int Charisma { get; set; }
        public int Perception { get; set; }
        public int Willpower { get; set; }

        // Financial
        public decimal Balance { get; set; }
        public long HomeStationId { get; set; }

        // UriCharacter source address (file path or URL for imported characters)
        public string? UriAddress { get; set; }

        // Character status and settings
        public string? CloneState { get; set; } = "Auto";
        public string? Label { get; set; }
        public string? ShipName { get; set; }
        public string? ShipTypeName { get; set; }
        public double SecurityStatus { get; set; }
        public string? LastKnownLocation { get; set; }

        // Remaps and jump clones
        public int FreeRespecs { get; set; }
        public DateTime CloneJumpDate { get; set; }
        public DateTime LastRespecDate { get; set; }
        public DateTime LastTimedRespec { get; set; }
        public DateTime RemoteStationDate { get; set; }
        public DateTime JumpActivationDate { get; set; }
        public DateTime JumpFatigueDate { get; set; }
        public DateTime JumpLastUpdateDate { get; set; }

        // Skills and training
        public List<JsonSkill> Skills { get; set; } = new List<JsonSkill>();
        public List<JsonSkillQueueEntry> SkillQueue { get; set; } = new List<JsonSkillQueueEntry>();
        public int FreeSkillPoints { get; set; }

        // Implants
        public List<JsonImplantSet> ImplantSets { get; set; } = new List<JsonImplantSet>();

        // Plans
        public List<JsonPlan> Plans { get; set; } = new List<JsonPlan>();

        // Employment history
        public List<JsonEmploymentRecord> EmploymentHistory { get; set; } = new List<JsonEmploymentRecord>();

        // Character UI settings (per-character preferences)
        public CharacterUISettings? UISettings { get; set; }

        // Cached API data
        public List<JsonMarketOrder> MarketOrders { get; set; } = new List<JsonMarketOrder>();
        public List<JsonContract> Contracts { get; set; } = new List<JsonContract>();
        public List<JsonIndustryJob> IndustryJobs { get; set; } = new List<JsonIndustryJob>();
        public List<JsonAsset> Assets { get; set; } = new List<JsonAsset>();
        public List<JsonWalletJournalEntry> WalletJournal { get; set; } = new List<JsonWalletJournalEntry>();
        public List<JsonWalletTransaction> WalletTransactions { get; set; } = new List<JsonWalletTransaction>();

        // Last update times for API data
        public Dictionary<string, DateTime> LastApiUpdates { get; set; } = new Dictionary<string, DateTime>();
    }

    public class JsonSkill
    {
        public int TypeId { get; set; }
        public string? Name { get; set; }
        public int Level { get; set; }
        public int ActiveLevel { get; set; }
        public long Skillpoints { get; set; }
        public bool IsKnown { get; set; }
        public bool OwnsBook { get; set; }
    }

    public class JsonSkillQueueEntry
    {
        public int TypeId { get; set; }
        public int Level { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StartSP { get; set; }
        public int EndSP { get; set; }
    }

    public class JsonImplantSet
    {
        public string? Name { get; set; }
        /// <summary>
        /// Distinguishes jump clones from user-created custom sets.
        /// "active" = active clone, "jump" = jump clone, "custom" = user-created.
        /// </summary>
        public string Type { get; set; } = "custom";
        public List<JsonImplant> Implants { get; set; } = new List<JsonImplant>();
    }

    public class JsonImplant
    {
        public int Slot { get; set; }
        public int TypeId { get; set; }
        public int Bonus { get; set; }
        public string? Name { get; set; }
    }

    public class JsonPlan
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<JsonPlanEntry> Entries { get; set; } = new List<JsonPlanEntry>();
        public List<JsonInvalidPlanEntry> InvalidEntries { get; set; } = new List<JsonInvalidPlanEntry>();
        public string SortCriteria { get; set; } = "None";
        public string SortOrder { get; set; } = "None";
        public bool GroupByPriority { get; set; }
    }

    public class JsonPlanEntry
    {
        public int SkillId { get; set; }
        public string? SkillName { get; set; }
        public int Level { get; set; }
        public string? Type { get; set; }
        public int Priority { get; set; }
        public string? Notes { get; set; }
        public List<string> PlanGroups { get; set; } = new List<string>();
        public JsonRemappingPoint? Remapping { get; set; }
    }

    public class JsonInvalidPlanEntry
    {
        public string? SkillName { get; set; }
        public long PlannedLevel { get; set; }
        public bool Acknowledged { get; set; }
    }

    public class JsonRemappingPoint
    {
        public string? Status { get; set; }
        public long Perception { get; set; }
        public long Intelligence { get; set; }
        public long Memory { get; set; }
        public long Willpower { get; set; }
        public long Charisma { get; set; }
        public string? Description { get; set; }
    }

    public class JsonEmploymentRecord
    {
        public long CorporationId { get; set; }
        public string? CorporationName { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class JsonMarketOrder { /* Will be fully implemented */ }
    public class JsonContract { /* Will be fully implemented */ }
    public class JsonIndustryJob { /* Will be fully implemented */ }
    public class JsonAsset { /* Will be fully implemented */ }
    public class JsonWalletJournalEntry { /* Will be fully implemented */ }
    public class JsonWalletTransaction { /* Will be fully implemented */ }

    #endregion

    #region Character Component File DTOs

    /// <summary>
    /// Component file: identity.json — character identity, attributes, employment, metadata.
    /// </summary>
    public class JsonCharacterIdentityFile
    {
        public int Version { get; set; } = 1;
        public long CharacterId { get; set; }
        public Guid Guid { get; set; }
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;
        public string? Name { get; set; }
        public DateTime Birthday { get; set; }
        public string? Race { get; set; }
        public string? Bloodline { get; set; }
        public string? Ancestry { get; set; }
        public string? Gender { get; set; }
        public long CorporationId { get; set; }
        public string? CorporationName { get; set; }
        public long AllianceId { get; set; }
        public string? AllianceName { get; set; }
        public long FactionId { get; set; }
        public string? FactionName { get; set; }
        public int Intelligence { get; set; }
        public int Memory { get; set; }
        public int Charisma { get; set; }
        public int Perception { get; set; }
        public int Willpower { get; set; }
        public long HomeStationId { get; set; }
        public string? UriAddress { get; set; }
        public string? CloneState { get; set; } = "Auto";
        public string? Label { get; set; }
        public string? ShipName { get; set; }
        public string? ShipTypeName { get; set; }
        public double SecurityStatus { get; set; }
        public string? LastKnownLocation { get; set; }
        public int FreeRespecs { get; set; }
        public DateTime CloneJumpDate { get; set; }
        public DateTime LastRespecDate { get; set; }
        public DateTime LastTimedRespec { get; set; }
        public DateTime RemoteStationDate { get; set; }
        public DateTime JumpActivationDate { get; set; }
        public DateTime JumpFatigueDate { get; set; }
        public DateTime JumpLastUpdateDate { get; set; }
        public int FreeSkillPoints { get; set; }
        public List<JsonEmploymentRecord> EmploymentHistory { get; set; } = new List<JsonEmploymentRecord>();
        public Dictionary<string, DateTime> LastApiUpdates { get; set; } = new Dictionary<string, DateTime>();
    }

    /// <summary>
    /// Component file: skills.json — skills list and skill queue.
    /// </summary>
    public class JsonCharacterSkillsFile
    {
        public int Version { get; set; } = 1;
        public List<JsonSkill> Skills { get; set; } = new List<JsonSkill>();
        public List<JsonSkillQueueEntry> SkillQueue { get; set; } = new List<JsonSkillQueueEntry>();
    }

    /// <summary>
    /// Component file: plans.json — all plans for this character.
    /// </summary>
    public class JsonCharacterPlansFile
    {
        public int Version { get; set; } = 1;
        public List<JsonPlan> Plans { get; set; } = new List<JsonPlan>();
    }

    /// <summary>
    /// Component file: implants.json — implant sets.
    /// </summary>
    public class JsonCharacterImplantsFile
    {
        public int Version { get; set; } = 1;
        public List<JsonImplantSet> ImplantSets { get; set; } = new List<JsonImplantSet>();
    }

    /// <summary>
    /// Component file: wallet.json — balance, orders, contracts, journal, transactions, jobs.
    /// </summary>
    public class JsonCharacterWalletFile
    {
        public int Version { get; set; } = 1;
        public decimal Balance { get; set; }
        public List<JsonMarketOrder> MarketOrders { get; set; } = new List<JsonMarketOrder>();
        public List<JsonContract> Contracts { get; set; } = new List<JsonContract>();
        public List<JsonIndustryJob> IndustryJobs { get; set; } = new List<JsonIndustryJob>();
        public List<JsonWalletJournalEntry> WalletJournal { get; set; } = new List<JsonWalletJournalEntry>();
        public List<JsonWalletTransaction> WalletTransactions { get; set; } = new List<JsonWalletTransaction>();
    }

    /// <summary>
    /// Component file: assets.json — asset list.
    /// </summary>
    public class JsonCharacterAssetsFile
    {
        public int Version { get; set; } = 1;
        public List<JsonAsset> Assets { get; set; } = new List<JsonAsset>();
    }

    /// <summary>
    /// Component file: settings.json — per-character UI settings.
    /// </summary>
    public class JsonCharacterSettingsFile
    {
        public int Version { get; set; } = 1;
        public CharacterUISettings? UISettings { get; set; }
    }

    #endregion
}
