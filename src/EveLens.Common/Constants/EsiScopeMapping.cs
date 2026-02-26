// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Enumerations.CCPAPI;

namespace EveLens.Common.Constants
{
    /// <summary>
    /// Maps ESI API methods to their required OAuth scope strings.
    /// Null means the endpoint is public and requires no scope.
    /// Built from CCP's official ESI scope documentation.
    /// </summary>
    public static class EsiScopeMapping
    {
        /// <summary>
        /// Character method → required ESI scope. Null = public endpoint.
        /// </summary>
        private static readonly Dictionary<ESIAPICharacterMethods, string?> s_characterScopes = new()
        {
            { ESIAPICharacterMethods.None, null },

            // Public endpoints (no scope required)
            { ESIAPICharacterMethods.CharacterSheet, null },
            { ESIAPICharacterMethods.Attributes, null },
            { ESIAPICharacterMethods.EmploymentHistory, null },

            // Skills & Training
            { ESIAPICharacterMethods.Skills, "esi-skills.read_skills.v1" },
            { ESIAPICharacterMethods.SkillQueue, "esi-skills.read_skillqueue.v1" },

            // Clones & Implants
            { ESIAPICharacterMethods.Clones, "esi-clones.read_clones.v1" },
            { ESIAPICharacterMethods.Implants, "esi-clones.read_implants.v1" },

            // Location
            { ESIAPICharacterMethods.Location, "esi-location.read_location.v1" },
            { ESIAPICharacterMethods.Ship, "esi-location.read_ship_type.v1" },

            // Wallet
            { ESIAPICharacterMethods.AccountBalance, "esi-wallet.read_character_wallet.v1" },
            { ESIAPICharacterMethods.WalletJournal, "esi-wallet.read_character_wallet.v1" },
            { ESIAPICharacterMethods.WalletTransactions, "esi-wallet.read_character_wallet.v1" },

            // Assets
            { ESIAPICharacterMethods.AssetList, "esi-assets.read_assets.v1" },

            // Market Orders
            { ESIAPICharacterMethods.MarketOrders, "esi-markets.read_character_orders.v1" },
            { ESIAPICharacterMethods.MarketOrdersHistory, "esi-markets.read_character_orders.v1" },

            // Contracts
            { ESIAPICharacterMethods.Contracts, "esi-contracts.read_character_contracts.v1" },
            { ESIAPICharacterMethods.ContractItems, "esi-contracts.read_character_contracts.v1" },
            { ESIAPICharacterMethods.ContractBids, "esi-contracts.read_character_contracts.v1" },

            // Industry
            { ESIAPICharacterMethods.IndustryJobs, "esi-industry.read_character_jobs.v1" },

            // Mail
            { ESIAPICharacterMethods.MailMessages, "esi-mail.read_mail.v1" },
            { ESIAPICharacterMethods.MailBodies, "esi-mail.read_mail.v1" },
            { ESIAPICharacterMethods.MailingLists, "esi-mail.read_mail.v1" },

            // Notifications
            { ESIAPICharacterMethods.Notifications, "esi-characters.read_notifications.v1" },
            { ESIAPICharacterMethods.ContactNotifications, "esi-characters.read_notifications.v1" },

            // Combat Log
            { ESIAPICharacterMethods.KillLog, "esi-killmails.read_killmails.v1" },

            // Planetary Interaction
            { ESIAPICharacterMethods.PlanetaryColonies, "esi-planets.manage_planets.v1" },
            { ESIAPICharacterMethods.PlanetaryLayout, "esi-planets.manage_planets.v1" },

            // Research
            { ESIAPICharacterMethods.ResearchPoints, "esi-characters.read_agents_research.v1" },

            // Contacts & Standings
            { ESIAPICharacterMethods.ContactList, "esi-characters.read_contacts.v1" },
            { ESIAPICharacterMethods.Standings, "esi-characters.read_standings.v1" },

            // Character Details
            { ESIAPICharacterMethods.Medals, "esi-characters.read_medals.v1" },
            { ESIAPICharacterMethods.FactionalWarfareStats, "esi-characters.read_fw_stats.v1" },
            { ESIAPICharacterMethods.LoyaltyPoints, "esi-characters.read_loyalty.v1" },
            { ESIAPICharacterMethods.JumpFatigue, "esi-characters.read_fatigue.v1" },

            // Calendar
            { ESIAPICharacterMethods.UpcomingCalendarEvents, "esi-calendar.read_calendar_events.v1" },
            { ESIAPICharacterMethods.UpcomingCalendarEventDetails, "esi-calendar.read_calendar_events.v1" },
            { ESIAPICharacterMethods.CalendarEventAttendees, "esi-calendar.read_calendar_events.v1" },

            // Structures
            { ESIAPICharacterMethods.CitadelInfo, "esi-universe.read_structures.v1" },
        };

        /// <summary>
        /// Corporation method → required ESI scope. Null = public endpoint.
        /// </summary>
        private static readonly Dictionary<ESIAPICorporationMethods, string?> s_corporationScopes = new()
        {
            { ESIAPICorporationMethods.None, null },
            { ESIAPICorporationMethods.CorporationSheet, null },
            { ESIAPICorporationMethods.CorporationMarketOrders, "esi-markets.read_corporation_orders.v1" },
            { ESIAPICorporationMethods.CorporationMarketOrdersHistory, "esi-markets.read_corporation_orders.v1" },
            { ESIAPICorporationMethods.CorporationContracts, "esi-contracts.read_corporation_contracts.v1" },
            { ESIAPICorporationMethods.CorporationContractItems, "esi-contracts.read_corporation_contracts.v1" },
            { ESIAPICorporationMethods.CorporationContractBids, "esi-contracts.read_corporation_contracts.v1" },
            { ESIAPICorporationMethods.CorporationIndustryJobs, "esi-industry.read_corporation_jobs.v1" },
            { ESIAPICorporationMethods.CorporationAccountBalance, "esi-wallet.read_corporation_wallets.v1" },
            { ESIAPICorporationMethods.CorporationWalletJournal, "esi-wallet.read_corporation_wallets.v1" },
            { ESIAPICorporationMethods.CorporationWalletTransactions, "esi-wallet.read_corporation_wallets.v1" },
            { ESIAPICorporationMethods.CorporationAssetList, "esi-assets.read_corporation_assets.v1" },
            { ESIAPICorporationMethods.CorporationContactList, "esi-corporations.read_contacts.v1" },
            { ESIAPICorporationMethods.CorporationContainerLog, "esi-corporations.read_container_logs.v1" },
            { ESIAPICorporationMethods.CorporationFactionalWarfareStats, "esi-corporations.read_fw_stats.v1" },
            { ESIAPICorporationMethods.CorporationKillLog, "esi-killmails.read_corporation_killmails.v1" },
            { ESIAPICorporationMethods.CorporationMedals, "esi-corporations.read_medals.v1" },
            { ESIAPICorporationMethods.CorporationMemberMedals, "esi-corporations.read_medals.v1" },
            { ESIAPICorporationMethods.CorporationMemberSecurity, "esi-corporations.read_titles.v1" },
            { ESIAPICorporationMethods.CorporationMemberSecurityLog, "esi-corporations.read_titles.v1" },
            { ESIAPICorporationMethods.CorporationMemberTracking, "esi-corporations.track_members.v1" },
            { ESIAPICorporationMethods.CorporationOutpostList, "esi-corporations.read_structures.v1" },
            { ESIAPICorporationMethods.CorporationOutpostServiceDetail, "esi-corporations.read_structures.v1" },
            { ESIAPICorporationMethods.CorporationShareholders, "esi-wallet.read_corporation_wallets.v1" },
            { ESIAPICorporationMethods.CorporationStandings, "esi-corporations.read_standings.v1" },
            { ESIAPICorporationMethods.CorporationStarbaseDetails, "esi-corporations.read_starbases.v1" },
            { ESIAPICorporationMethods.CorporationStarbaseList, "esi-corporations.read_starbases.v1" },
            { ESIAPICorporationMethods.CorporationTitles, "esi-corporations.read_titles.v1" },
            { ESIAPICorporationMethods.CorporationBookmarks, null }, // No longer in ESI
        };

        /// <summary>
        /// Maps an ESI scope string to the cache endpoint keys that store data for that scope.
        /// Used to delete stale cache files when a scope is revoked during re-authentication.
        /// </summary>
        private static readonly Dictionary<string, string[]> s_scopeCacheKeys = new()
        {
            { "esi-wallet.read_character_wallet.v1", new[] { "wallet_journal", "wallet_transactions" } },
            { "esi-assets.read_assets.v1", new[] { "assets" } },
            { "esi-markets.read_character_orders.v1", new[] { "market_orders" } },
            { "esi-contracts.read_character_contracts.v1", new[] { "contracts" } },
            { "esi-industry.read_character_jobs.v1", new[] { "industry_jobs" } },
            { "esi-mail.read_mail.v1", new[] { "mail_headers", "mailing_lists" } },
            { "esi-characters.read_notifications.v1", new[] { "notifications" } },
            { "esi-characters.read_contacts.v1", new[] { "contacts" } },
            { "esi-characters.read_standings.v1", new[] { "standings" } },
            { "esi-characters.read_medals.v1", new[] { "medals" } },
            { "esi-characters.read_fw_stats.v1", new[] { "factional_warfare" } },
            { "esi-characters.read_agents_research.v1", new[] { "research" } },
            { "esi-killmails.read_killmails.v1", new[] { "kill_log" } },
            { "esi-calendar.read_calendar_events.v1", new[] { "calendar" } },
            { "esi-planets.manage_planets.v1", new[] { "planetary" } },
            { "esi-characters.read_loyalty.v1", new[] { "loyalty" } },
        };

        /// <summary>
        /// Returns the cache endpoint keys associated with the given ESI scope,
        /// or an empty list if the scope has no mapped cache keys.
        /// </summary>
        public static IReadOnlyList<string> GetCacheKeysForScope(string scope)
        {
            return s_scopeCacheKeys.TryGetValue(scope, out string[]? keys)
                ? keys
                : Array.Empty<string>();
        }

        /// <summary>
        /// Returns the ESI scope required for the given character API method,
        /// or null if the endpoint is public.
        /// </summary>
        public static string? GetRequiredScope(ESIAPICharacterMethods method)
        {
            return s_characterScopes.TryGetValue(method, out string? scope) ? scope : null;
        }

        /// <summary>
        /// Returns the ESI scope required for the given corporation API method,
        /// or null if the endpoint is public.
        /// </summary>
        public static string? GetRequiredCorpScope(ESIAPICorporationMethods method)
        {
            return s_corporationScopes.TryGetValue(method, out string? scope) ? scope : null;
        }

        /// <summary>
        /// Returns true if the given scope set includes the scope required for the method.
        /// Returns true for public endpoints (null scope) and for unmapped methods.
        /// </summary>
        public static bool HasScope(IEnumerable<string> authorizedScopes, ESIAPICharacterMethods method)
        {
            string? required = GetRequiredScope(method);
            if (required == null)
                return true;

            foreach (string scope in authorizedScopes)
            {
                if (scope == required)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given scope set includes the scope required for the corp method.
        /// Returns true for public endpoints (null scope) and for unmapped methods.
        /// </summary>
        public static bool HasCorpScope(IEnumerable<string> authorizedScopes, ESIAPICorporationMethods method)
        {
            string? required = GetRequiredCorpScope(method);
            if (required == null)
                return true;

            foreach (string scope in authorizedScopes)
            {
                if (scope == required)
                    return true;
            }

            return false;
        }
    }
}
