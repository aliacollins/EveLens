// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Serialization.Settings
{
    /// <summary>
    /// This class is a temporary representation of the <see cref="EveLens.Common.Settings" /> class for serialization purposes through automatic serialization
    /// </summary>
    [XmlRoot("Settings")]
    public sealed class SerializableSettings
    {
        private readonly Collection<SerializablePlan> m_plans;
        private readonly Collection<SerializableESIKey> m_esiKeys;
        private readonly Collection<SerializableSettingsCharacter> m_characters;
        private readonly Collection<MonitoredCharacterSettings> m_monitoredCharacters;

        private readonly Collection<string> m_esiCustomScopes;
        private readonly Collection<CharacterGroupSettings> m_characterGroups;

        public SerializableSettings()
        {
            m_plans = new Collection<SerializablePlan>();
            m_esiKeys = new Collection<SerializableESIKey>();
            m_characters = new Collection<SerializableSettingsCharacter>();
            m_monitoredCharacters = new Collection<MonitoredCharacterSettings>();
            m_esiCustomScopes = new Collection<string>();
            m_characterGroups = new Collection<CharacterGroupSettings>();
            SSOClientID = string.Empty;
            SSOClientSecret = string.Empty;
            CloudStorageServiceProvider = new CloudStorageServiceProviderSettings();
            PortableEveInstallations = new PortableEveInstallationsSettings();
            Notifications = new NotificationSettings();
            LoadoutsProvider = new LoadoutsProviderSettings();
            MarketPricer = new MarketPricerSettings();
            Exportation = new ExportationSettings();
            Scheduler = new SchedulerSettings();
            Calendar = new CalendarSettings();
            Updates = new UpdateSettings();
            Proxy = new ProxySettings();
            G15 = new G15Settings();
            UI = new UISettings();
        }

        [XmlAttribute("clientID")]
        public string SSOClientID { get; set; }
        [XmlAttribute("clientSecret")]
        public string SSOClientSecret { get; set; }

        [XmlAttribute("revision")]
        public int Revision { get; set; }

        /// <summary>
        /// Identifies which EveLens fork created this settings file.
        /// Used to detect migration from other forks.
        /// </summary>
        [XmlAttribute("forkId")]
        public string? ForkId { get; set; }

        /// <summary>
        /// Version of the fork that last saved this settings file.
        /// </summary>
        [XmlAttribute("forkVersion")]
        public string? ForkVersion { get; set; }

        [XmlElement("compatibility")]
        public CompatibilityMode Compatibility { get; set; }

        [XmlArray("esiKeys")]
        [XmlArrayItem("esikey")]
        public Collection<SerializableESIKey> ESIKeys => m_esiKeys;

        [XmlArray("characters")]
        [XmlArrayItem("ccp", typeof(SerializableCCPCharacter))]
        [XmlArrayItem("uri", typeof(SerializableUriCharacter))]
        public Collection<SerializableSettingsCharacter> Characters => m_characters;

        [XmlArray("plans")]
        [XmlArrayItem("plan")]
        public Collection<SerializablePlan> Plans => m_plans;

        [XmlArray("monitoredCharacters")]
        [XmlArrayItem("character")]
        public Collection<MonitoredCharacterSettings> MonitoredCharacters => m_monitoredCharacters;
        
        [XmlElement("updates")]
        public UpdateSettings Updates { get; set; }

        [XmlElement("notifications")]
        public NotificationSettings Notifications { get; set; }

        [XmlElement("scheduler")]
        public SchedulerSettings Scheduler { get; set; }

        [XmlElement("calendar")]
        public CalendarSettings Calendar { get; set; }

        [XmlElement("exportation")]
        public ExportationSettings Exportation { get; set; }

        [XmlElement("marketPricer")]
        public MarketPricerSettings MarketPricer { get; set; }

        [XmlElement("loadoutsProvider")]
        public LoadoutsProviderSettings LoadoutsProvider { get; set; }

        [XmlElement("cloudStorageServiceProvider")]
        public CloudStorageServiceProviderSettings CloudStorageServiceProvider { get; set; }

        [XmlElement("portableEveInstallations")]
        public PortableEveInstallationsSettings PortableEveInstallations { get; set; }

        [XmlElement("G15")]
        public G15Settings G15 { get; set; }

        [XmlElement("UI")]
        public UISettings UI { get; set; }

        [XmlElement("proxy")]
        public ProxySettings Proxy { get; set; }

        [XmlElement("esiScopePreset")]
        public string EsiScopePreset { get; set; } = "FullMonitoring";

        [XmlArray("esiCustomScopes")]
        [XmlArrayItem("scope")]
        public Collection<string> EsiCustomScopes => m_esiCustomScopes;

        [XmlArray("characterGroups")]
        [XmlArrayItem("group")]
        public Collection<CharacterGroupSettings> CharacterGroups => m_characterGroups;
    }
}
