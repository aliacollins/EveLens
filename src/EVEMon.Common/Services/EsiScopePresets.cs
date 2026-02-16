using System.Collections.Generic;
using System.Linq;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Defines ESI scope presets and the mapping from feature groups to ESI scope strings.
    /// </summary>
    public static class EsiScopePresets
    {
        /// <summary>
        /// Preset identifier for full monitoring (all scopes).
        /// </summary>
        public const string FullMonitoring = "FullMonitoring";

        /// <summary>
        /// Preset identifier for skill planner only.
        /// </summary>
        public const string SkillPlannerOnly = "SkillPlannerOnly";

        /// <summary>
        /// Preset identifier for standard monitoring.
        /// </summary>
        public const string StandardMonitoring = "StandardMonitoring";

        /// <summary>
        /// Preset identifier for a custom user selection.
        /// </summary>
        public const string Custom = "Custom";

        /// <summary>
        /// Display names for each preset.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> PresetDisplayNames =
            new Dictionary<string, string>
            {
                { FullMonitoring, "Full Monitoring (recommended)" },
                { SkillPlannerOnly, "Skill Planner Only" },
                { StandardMonitoring, "Standard Monitoring" },
                { Custom, "Custom" }
            };

        /// <summary>
        /// Description text shown in the UI for each preset.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> PresetDescriptions =
            new Dictionary<string, string>
            {
                { FullMonitoring, "All character features will be available." },
                { SkillPlannerOnly, "Skills, skill queue, clones, and implants only. Wallet, assets, market orders, mail, and other features will be unavailable." },
                { StandardMonitoring, "Skills, wallet, assets, market orders, industry, and contracts. Mail, notifications, calendar, planetary, kills, and corp data will be unavailable." },
                { Custom, "" } // dynamically generated
            };

        /// <summary>
        /// Ordered list of preset keys (excluding Custom) for the combo box.
        /// </summary>
        public static readonly IReadOnlyList<string> PresetKeys = new[]
        {
            FullMonitoring,
            StandardMonitoring,
            SkillPlannerOnly
        };

        /// <summary>
        /// Feature group definitions mapping group display names to their ESI scopes.
        /// </summary>
        public static readonly IReadOnlyList<FeatureGroup> FeatureGroups = new[]
        {
            new FeatureGroup("Skills & Training Queue", new[]
            {
                "esi-skills.read_skills.v1",
                "esi-skills.read_skillqueue.v1"
            }),
            new FeatureGroup("Character Sheet & Clones", new[]
            {
                "esi-clones.read_clones.v1",
                "esi-clones.read_implants.v1",
                "esi-characters.read_fatigue.v1"
            }),
            new FeatureGroup("Location & Ship", new[]
            {
                "esi-location.read_location.v1",
                "esi-location.read_ship_type.v1"
            }),
            new FeatureGroup("Wallet", new[]
            {
                "esi-wallet.read_character_wallet.v1"
            }),
            new FeatureGroup("Assets", new[]
            {
                "esi-assets.read_assets.v1"
            }),
            new FeatureGroup("Market Orders", new[]
            {
                "esi-markets.read_character_orders.v1",
                "esi-markets.structure_markets.v1"
            }),
            new FeatureGroup("Contracts", new[]
            {
                "esi-contracts.read_character_contracts.v1"
            }),
            new FeatureGroup("Industry Jobs", new[]
            {
                "esi-industry.read_character_jobs.v1"
            }),
            new FeatureGroup("Mail & Notifications", new[]
            {
                "esi-mail.read_mail.v1",
                "esi-characters.read_notifications.v1"
            }),
            new FeatureGroup("Calendar", new[]
            {
                "esi-calendar.read_calendar_events.v1"
            }),
            new FeatureGroup("Planetary Industry", new[]
            {
                "esi-planets.manage_planets.v1"
            }),
            new FeatureGroup("Combat Log", new[]
            {
                "esi-killmails.read_killmails.v1"
            }),
            new FeatureGroup("Character Details", new[]
            {
                "esi-characters.read_contacts.v1",
                "esi-characters.read_standings.v1",
                "esi-characters.read_loyalty.v1",
                "esi-characters.read_medals.v1",
                "esi-characters.read_agents_research.v1",
                "esi-characters.read_blueprints.v1",
                "esi-characters.read_corporation_roles.v1"
            }),
            new FeatureGroup("Faction Warfare", new[]
            {
                "esi-characters.read_fw_stats.v1"
            }),
            new FeatureGroup("Structures", new[]
            {
                "esi-universe.read_structures.v1"
            }),
            new FeatureGroup("Corporation Data", new[]
            {
                "esi-corporations.read_structures.v1",
                "esi-killmails.read_corporation_killmails.v1",
                "esi-wallet.read_corporation_wallets.v1",
                "esi-corporations.read_divisions.v1",
                "esi-corporations.read_contacts.v1",
                "esi-assets.read_corporation_assets.v1",
                "esi-corporations.read_blueprints.v1",
                "esi-contracts.read_corporation_contracts.v1",
                "esi-corporations.read_standings.v1",
                "esi-industry.read_corporation_jobs.v1",
                "esi-markets.read_corporation_orders.v1",
                "esi-corporations.read_medals.v1",
                "esi-alliances.read_contacts.v1",
                "esi-corporations.read_fw_stats.v1"
            })
        };

        /// <summary>
        /// All scopes across all feature groups.
        /// </summary>
        public static readonly IReadOnlyList<string> AllScopes =
            FeatureGroups.SelectMany(g => g.Scopes).Distinct().ToArray();

        /// <summary>
        /// Returns the set of scopes for a given preset name.
        /// </summary>
        public static HashSet<string> GetScopesForPreset(string presetName)
        {
            switch (presetName)
            {
                case SkillPlannerOnly:
                    return GetSkillPlannerScopes();
                case StandardMonitoring:
                    return GetStandardMonitoringScopes();
                case FullMonitoring:
                default:
                    return new HashSet<string>(AllScopes);
            }
        }

        /// <summary>
        /// Determines which preset matches the given set of scopes, or returns Custom.
        /// </summary>
        public static string DetectPreset(IEnumerable<string> scopes)
        {
            var scopeSet = new HashSet<string>(scopes);

            if (scopeSet.SetEquals(AllScopes))
                return FullMonitoring;

            if (scopeSet.SetEquals(GetStandardMonitoringScopes()))
                return StandardMonitoring;

            if (scopeSet.SetEquals(GetSkillPlannerScopes()))
                return SkillPlannerOnly;

            return Custom;
        }

        /// <summary>
        /// Generates a description for a custom scope selection.
        /// </summary>
        public static string GetCustomDescription(IEnumerable<string> selectedScopes)
        {
            var scopeSet = new HashSet<string>(selectedScopes);
            int count = scopeSet.Count;
            int total = AllScopes.Count;

            var unavailable = FeatureGroups
                .Where(g => !g.Scopes.All(s => scopeSet.Contains(s)))
                .Select(g => g.Name)
                .ToList();

            string desc = $"{count} of {total} scopes selected.";
            if (unavailable.Count > 0)
                desc += $" Unavailable: {string.Join(", ", unavailable)}.";

            return desc;
        }

        private static readonly string[] SkillPlannerGroupNames = new[]
        {
            "Skills & Training Queue",
            "Character Sheet & Clones",
            "Structures",
            "Character Details"
        };

        // Skill Planner Only includes skills, clones, implants, fatigue, agents_research,
        // structures, and the specific scopes from Character Details that are needed
        private static HashSet<string> GetSkillPlannerScopes()
        {
            var scopes = new HashSet<string>();

            // Skills & Training Queue
            foreach (var s in GetGroupByName("Skills & Training Queue").Scopes)
                scopes.Add(s);

            // Character Sheet & Clones
            foreach (var s in GetGroupByName("Character Sheet & Clones").Scopes)
                scopes.Add(s);

            // Structures (for name resolution)
            scopes.Add("esi-universe.read_structures.v1");

            // agents_research from Character Details
            scopes.Add("esi-characters.read_agents_research.v1");

            return scopes;
        }

        private static readonly string[] StandardMonitoringGroupNames = new[]
        {
            "Skills & Training Queue",
            "Character Sheet & Clones",
            "Location & Ship",
            "Wallet",
            "Assets",
            "Market Orders",
            "Contracts",
            "Industry Jobs",
            "Character Details",
            "Structures"
        };

        private static HashSet<string> GetStandardMonitoringScopes()
        {
            var scopes = new HashSet<string>();
            foreach (var name in StandardMonitoringGroupNames)
            {
                foreach (var s in GetGroupByName(name).Scopes)
                    scopes.Add(s);
            }
            return scopes;
        }

        private static FeatureGroup GetGroupByName(string name)
        {
            return FeatureGroups.First(g => g.Name == name);
        }
    }

    /// <summary>
    /// Represents a feature group with a display name and its associated ESI scopes.
    /// </summary>
    public sealed class FeatureGroup
    {
        public FeatureGroup(string name, string[] scopes)
        {
            Name = name;
            Scopes = scopes;
        }

        public string Name { get; }
        public string[] Scopes { get; }
    }
}
