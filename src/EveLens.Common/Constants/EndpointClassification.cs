// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Enumerations.CCPAPI;

namespace EveLens.Common.Constants
{
    /// <summary>
    /// Classifies ESI endpoints as core (always-on) or on-demand (user-activated).
    /// Core endpoints fetch for all characters on startup.
    /// On-demand endpoints only fetch when the user enables them per-character.
    /// </summary>
    public static class EndpointClassification
    {
        /// <summary>
        /// Core endpoints that always fetch for every monitored character.
        /// These provide overview/header data and cannot be disabled.
        /// </summary>
        public static readonly HashSet<ESIAPICharacterMethods> CoreEndpoints = new()
        {
            ESIAPICharacterMethods.CharacterSheet,
            ESIAPICharacterMethods.Skills,
            ESIAPICharacterMethods.SkillQueue,
            ESIAPICharacterMethods.AccountBalance,
            ESIAPICharacterMethods.Location,
            ESIAPICharacterMethods.Ship,
            ESIAPICharacterMethods.Clones,
            ESIAPICharacterMethods.Implants,
        };

        /// <summary>
        /// Returns true if the endpoint is core (always-on, cannot be disabled).
        /// </summary>
        public static bool IsCore(ESIAPICharacterMethods method) => CoreEndpoints.Contains(method);

        /// <summary>
        /// Endpoints whose opt-in is the OAuth scope grant itself. These have no
        /// character-monitor tab (the only writer of EnabledEndpoints), so gating
        /// them on EnabledEndpoints would leave them permanently skipped. They cost
        /// nothing for characters without the scope: the fetch closure's
        /// FindAPIKeyWithAccess check already returns null and skips the request.
        /// </summary>
        public static readonly HashSet<ESIAPICharacterMethods> ScopeActivatedEndpoints = new()
        {
            ESIAPICharacterMethods.SkinrLicenses,
            ESIAPICharacterMethods.SkinrComponents,
        };

        /// <summary>
        /// Central fetch gate for the ESI scheduler: core and scope-activated
        /// endpoints always fetch; everything else requires the user to have
        /// enabled it (by opening its character-monitor tab).
        /// </summary>
        public static bool IsFetchAllowed(ESIAPICharacterMethods method, ICollection<string> enabledEndpoints) =>
            CoreEndpoints.Contains(method) ||
            ScopeActivatedEndpoints.Contains(method) ||
            enabledEndpoints.Contains(method.ToString());

        /// <summary>
        /// Maps Avalonia tab names to their corresponding ESI endpoint methods.
        /// </summary>
        public static readonly Dictionary<string, ESIAPICharacterMethods> TabToEndpoint = new()
        {
            { "Assets", ESIAPICharacterMethods.AssetList },
            { "Orders", ESIAPICharacterMethods.MarketOrders },
            { "Contracts", ESIAPICharacterMethods.Contracts },
            { "Industry", ESIAPICharacterMethods.IndustryJobs },
            { "Journal", ESIAPICharacterMethods.WalletJournal },
            { "Transactions", ESIAPICharacterMethods.WalletTransactions },
            { "Mail", ESIAPICharacterMethods.MailMessages },
            { "Notify", ESIAPICharacterMethods.Notifications },
            { "Kills", ESIAPICharacterMethods.KillLog },
            { "PI", ESIAPICharacterMethods.PlanetaryColonies },
            { "Research", ESIAPICharacterMethods.ResearchPoints },
            { "Employment", ESIAPICharacterMethods.EmploymentHistory },
            { "Contacts", ESIAPICharacterMethods.ContactList },
            { "Standings", ESIAPICharacterMethods.Standings },
            { "FW", ESIAPICharacterMethods.FactionalWarfareStats },
            { "Medals", ESIAPICharacterMethods.Medals },
            { "LP", ESIAPICharacterMethods.LoyaltyPoints },
        };

        /// <summary>
        /// Returns a user-friendly display name for an ESI endpoint.
        /// </summary>
        public static string EndpointDisplayName(ESIAPICharacterMethods method)
        {
            return method switch
            {
                ESIAPICharacterMethods.AssetList => "Asset Monitoring",
                ESIAPICharacterMethods.MarketOrders => "Market Order Tracking",
                ESIAPICharacterMethods.Contracts => "Contract Tracking",
                ESIAPICharacterMethods.IndustryJobs => "Industry Job Tracking",
                ESIAPICharacterMethods.WalletJournal => "Wallet Journal",
                ESIAPICharacterMethods.WalletTransactions => "Wallet Transactions",
                ESIAPICharacterMethods.MailMessages => "Mail Messages",
                ESIAPICharacterMethods.Notifications => "Notifications",
                ESIAPICharacterMethods.KillLog => "Kill Log",
                ESIAPICharacterMethods.PlanetaryColonies => "Planetary Interaction",
                ESIAPICharacterMethods.ResearchPoints => "Research Points",
                ESIAPICharacterMethods.EmploymentHistory => "Employment History",
                ESIAPICharacterMethods.ContactList => "Contacts",
                ESIAPICharacterMethods.Standings => "Standings",
                ESIAPICharacterMethods.FactionalWarfareStats => "Factional Warfare",
                ESIAPICharacterMethods.Medals => "Medals",
                ESIAPICharacterMethods.LoyaltyPoints => "Loyalty Points",
                _ => method.ToString(),
            };
        }
    }
}
