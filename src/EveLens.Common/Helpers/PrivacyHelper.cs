// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Globalization;
using EveLens.Common.Enumerations;
using EveLens.Common.Services;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Static helpers for masking sensitive values when privacy mode is active.
    /// Any code-behind or ViewModel can call these without event wiring.
    /// </summary>
    public static class PrivacyHelper
    {
        public const string Mask = "\u2022\u2022\u2022\u2022\u2022";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static bool IsActive => AppServices.PrivacyModeEnabled;

        public static bool IsNameHidden => AppServices.IsPrivate(PrivacyCategories.Name);
        public static bool IsCorpAllianceHidden => AppServices.IsPrivate(PrivacyCategories.CorpAlliance);
        public static bool IsBalanceHidden => AppServices.IsPrivate(PrivacyCategories.Balance);
        public static bool IsSkillPointsHidden => AppServices.IsPrivate(PrivacyCategories.SkillPoints);
        public static bool IsTrainingHidden => AppServices.IsPrivate(PrivacyCategories.Training);
        public static bool IsRemapsHidden => AppServices.IsPrivate(PrivacyCategories.Remaps);

        public static string MaskText(string value)
            => IsActive ? Mask : value;

        public static string MaskIsk(decimal balance)
            => IsActive ? $"{Mask} ISK" : balance.ToString("N2", Inv) + " ISK";

        public static string MaskNumber(long value, string suffix = "")
            => IsActive
                ? $"{Mask}{(suffix.Length > 0 ? " " + suffix : "")}"
                : value.ToString("N0", Inv) + (suffix.Length > 0 ? " " + suffix : "");

        public static string MaskFormatted(string formatted)
            => IsActive ? Mask : formatted;
    }
}
