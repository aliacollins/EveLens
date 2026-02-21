// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Enumerations.CCPAPI;

namespace EVEMon.Common.Constants
{
    /// <summary>
    /// Central reference for ESI endpoint cache times and rate limit groups.
    /// Source of truth: https://esi.evetech.net/ OpenAPI specs.
    /// Last verified: 2026-02-18.
    ///
    /// The EsiScheduler uses CachedUntil from response headers as the primary scheduling signal.
    /// These values serve as: (1) documentation, (2) fallback when no response header available,
    /// (3) reference for the legacy QueryMonitor [Update] attributes.
    ///
    /// RATE LIMITING (Floating Window):
    ///   - Per (applicationID, characterID) pair for authenticated routes
    ///   - Per sourceIP for unauthenticated routes
    ///   - Token costs: 200=2 tokens, 304=1 token, 4xx=5 tokens, 5xx=0 tokens
    ///   - 304 (conditional GET with ETag) costs half — always use ETags
    ///   - Error rate limit: max 100 non-2xx/3xx per minute (returns 420 globally)
    /// </summary>
    public static class EsiEndpointReference
    {
        /// <summary>
        /// CCP's actual cache duration for each ESI endpoint.
        /// Update these when CCP changes cache times.
        /// </summary>
        public static readonly Dictionary<ESIAPICharacterMethods, TimeSpan> CcpCacheTimes = new()
        {
            // ═══════════════════════════════════════════════════════
            // Core character endpoints
            // ═══════════════════════════════════════════════════════
            { ESIAPICharacterMethods.CharacterSheet,    TimeSpan.FromHours(1) },      // /characters/{id}/ — cached 1 hour
            { ESIAPICharacterMethods.Skills,            TimeSpan.FromMinutes(1) },     // /characters/{id}/skills/ — cached 1 minute
            { ESIAPICharacterMethods.SkillQueue,        TimeSpan.FromMinutes(1) },     // /characters/{id}/skillqueue/ — cached 1 minute
            { ESIAPICharacterMethods.AccountBalance,    TimeSpan.FromMinutes(2) },     // /characters/{id}/wallet/ — cached ~2 minutes
            { ESIAPICharacterMethods.Location,          TimeSpan.FromSeconds(30) },    // /characters/{id}/location/ — cached 30 seconds
            { ESIAPICharacterMethods.Ship,              TimeSpan.FromSeconds(30) },    // /characters/{id}/ship/ — cached 30 seconds
            { ESIAPICharacterMethods.Clones,            TimeSpan.FromMinutes(2) },     // /characters/{id}/clones/ — cached 2 minutes
            { ESIAPICharacterMethods.Implants,          TimeSpan.FromMinutes(2) },     // /characters/{id}/implants/ — cached 2 minutes
            { ESIAPICharacterMethods.Attributes,        TimeSpan.FromMinutes(2) },     // /characters/{id}/attributes/ — cached 2 minutes

            // ═══════════════════════════════════════════════════════
            // Market & Industry
            // ═══════════════════════════════════════════════════════
            { ESIAPICharacterMethods.MarketOrders,      TimeSpan.FromMinutes(20) },    // /characters/{id}/orders/ — cached 20 minutes
            { ESIAPICharacterMethods.MarketOrdersHistory,TimeSpan.FromHours(1) },      // /characters/{id}/orders/history/ — cached 1 hour
            { ESIAPICharacterMethods.Contracts,         TimeSpan.FromMinutes(5) },     // /characters/{id}/contracts/ — cached 5 minutes
            { ESIAPICharacterMethods.IndustryJobs,      TimeSpan.FromMinutes(5) },     // /characters/{id}/industry/jobs/ — cached 5 minutes

            // ═══════════════════════════════════════════════════════
            // Wallet
            // ═══════════════════════════════════════════════════════
            { ESIAPICharacterMethods.WalletJournal,     TimeSpan.FromMinutes(5) },     // /characters/{id}/wallet/journal/ — cached 5 minutes
            { ESIAPICharacterMethods.WalletTransactions, TimeSpan.FromMinutes(5) },    // /characters/{id}/wallet/transactions/ — cached 5 minutes

            // ═══════════════════════════════════════════════════════
            // Communications
            // ═══════════════════════════════════════════════════════
            { ESIAPICharacterMethods.MailMessages,      TimeSpan.FromSeconds(30) },    // /characters/{id}/mail/ — cached 30 seconds
            { ESIAPICharacterMethods.MailBodies,        TimeSpan.FromSeconds(30) },    // /characters/{id}/mail/{id}/ — cached 30 seconds
            { ESIAPICharacterMethods.MailingLists,      TimeSpan.FromMinutes(2) },     // /characters/{id}/mail/lists/ — cached 2 minutes
            { ESIAPICharacterMethods.Notifications,     TimeSpan.FromMinutes(10) },    // /characters/{id}/notifications/ — cached 10 minutes
            { ESIAPICharacterMethods.ContactNotifications, TimeSpan.FromMinutes(10) }, // /characters/{id}/notifications/contacts/ — cached 10 minutes

            // ═══════════════════════════════════════════════════════
            // Social
            // ═══════════════════════════════════════════════════════
            { ESIAPICharacterMethods.ContactList,       TimeSpan.FromMinutes(5) },     // /characters/{id}/contacts/ — cached 5 minutes
            { ESIAPICharacterMethods.Standings,         TimeSpan.FromHours(1) },       // /characters/{id}/standings/ — cached 1 hour
            { ESIAPICharacterMethods.Medals,            TimeSpan.FromMinutes(30) },    // /characters/{id}/medals/ — cached 30 minutes

            // ═══════════════════════════════════════════════════════
            // Other
            // ═══════════════════════════════════════════════════════
            { ESIAPICharacterMethods.AssetList,         TimeSpan.FromHours(1) },       // /characters/{id}/assets/ — cached 1 hour, rate group: char-asset (1800/15m)
            { ESIAPICharacterMethods.KillLog,           TimeSpan.FromMinutes(5) },     // /characters/{id}/killmails/recent/ — cached 5 minutes
            { ESIAPICharacterMethods.EmploymentHistory, TimeSpan.FromHours(1) },       // /characters/{id}/corporationhistory/ — cached 1 hour
            { ESIAPICharacterMethods.FactionalWarfareStats, TimeSpan.FromHours(1) },   // /characters/{id}/fw/stats/ — cached 1 hour
            { ESIAPICharacterMethods.PlanetaryColonies, TimeSpan.FromMinutes(10) },    // /characters/{id}/planets/ — cached 10 minutes
            { ESIAPICharacterMethods.ResearchPoints,    TimeSpan.FromHours(1) },       // /characters/{id}/agents_research/ — cached 1 hour
            { ESIAPICharacterMethods.LoyaltyPoints,     TimeSpan.FromHours(1) },       // /characters/{id}/loyalty/points/ — cached 1 hour
            { ESIAPICharacterMethods.UpcomingCalendarEvents, TimeSpan.FromMinutes(5) }, // /characters/{id}/calendar/ — cached 5 minutes
        };

        /// <summary>
        /// ESI rate limit groups and their budgets.
        /// Format: group name → (max tokens per window, window size).
        /// </summary>
        public static readonly Dictionary<string, (int MaxTokens, TimeSpan Window)> RateLimitGroups = new()
        {
            { "char-detail",       (600,  TimeSpan.FromMinutes(15)) },  // Skills, SkillQueue, Implants, Industry, JumpFatigue
            { "char-location",     (1200, TimeSpan.FromMinutes(15)) },  // Location, Ship, Clones
            { "char-wallet",       (150,  TimeSpan.FromMinutes(15)) },  // Wallet, WalletJournal, WalletTransactions, LP
            { "char-asset",        (1800, TimeSpan.FromMinutes(15)) },  // Assets, AssetLocations, AssetNames
            { "char-contract",     (600,  TimeSpan.FromMinutes(15)) },  // Contracts, ContractItems
            { "char-social",       (600,  TimeSpan.FromMinutes(15)) },  // Contacts, Mail, MailingLists
            { "char-notification", (15,   TimeSpan.FromMinutes(15)) },  // Notifications (very tight!)
            { "char-killmail",     (30,   TimeSpan.FromMinutes(15)) },  // Killmails (tight)
            { "killmail",          (3600, TimeSpan.FromMinutes(15)) },  // Single killmail lookup
            { "status",            (600,  TimeSpan.FromMinutes(15)) },  // Server status
        };

        /// <summary>
        /// Maps ESI endpoints to their rate limit groups.
        /// </summary>
        public static readonly Dictionary<ESIAPICharacterMethods, string> EndpointRateGroup = new()
        {
            { ESIAPICharacterMethods.Skills,            "char-detail" },
            { ESIAPICharacterMethods.SkillQueue,        "char-detail" },
            { ESIAPICharacterMethods.Implants,          "char-detail" },
            { ESIAPICharacterMethods.Attributes,        "char-detail" },
            { ESIAPICharacterMethods.IndustryJobs,      "char-detail" },
            { ESIAPICharacterMethods.Location,          "char-location" },
            { ESIAPICharacterMethods.Ship,              "char-location" },
            { ESIAPICharacterMethods.Clones,            "char-location" },
            { ESIAPICharacterMethods.AccountBalance,    "char-wallet" },
            { ESIAPICharacterMethods.WalletJournal,     "char-wallet" },
            { ESIAPICharacterMethods.WalletTransactions,"char-wallet" },
            { ESIAPICharacterMethods.LoyaltyPoints,     "char-wallet" },
            { ESIAPICharacterMethods.AssetList,         "char-asset" },
            { ESIAPICharacterMethods.Contracts,         "char-contract" },
            { ESIAPICharacterMethods.ContactList,       "char-social" },
            { ESIAPICharacterMethods.MailMessages,      "char-social" },
            { ESIAPICharacterMethods.MailingLists,      "char-social" },
            { ESIAPICharacterMethods.Notifications,     "char-notification" },
            { ESIAPICharacterMethods.KillLog,           "char-killmail" },
        };
    }
}
