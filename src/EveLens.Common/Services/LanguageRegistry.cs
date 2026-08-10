// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;

namespace EveLens.Common.Services
{
    /// <summary>
    /// The single source of truth for every language EveLens supports.
    ///
    /// To add a new language, you do exactly three things:
    ///   1. Drop a <c>Resources/ui-strings-&lt;code&gt;.txt</c> file (the translator's
    ///      <c>Key = value</c> list — same format we hand out; no code, no escaping).
    ///   2. Run <c>tools/generate-translations.ps1 -Language &lt;sde-code&gt;</c> to produce the
    ///      bundled <c>eve-translations-&lt;code&gt;.xml.gzip</c> of 50k+ EVE item/skill names.
    ///   3. Add ONE line to <see cref="All"/> below.
    ///
    /// Everything else — the Settings language picker, the SDE-name loader, fallback to English —
    /// derives from this list automatically. No other file needs editing.
    /// </summary>
    public sealed class LanguageInfo
    {
        /// <summary>Locale code used everywhere (e.g. "en", "zh-CN", "ko"). Matches the
        /// <c>ui-strings-&lt;code&gt;.txt</c> and <c>eve-translations-&lt;code&gt;.xml.gzip</c> file names.</summary>
        public string Code { get; }

        /// <summary>Native display name shown in the Settings language picker.</summary>
        public string DisplayName { get; }

        /// <summary>True if a bundled SDE-name datafile (eve-translations-&lt;code&gt;.xml.gzip)
        /// exists so item/skill/group names render in this language. English needs none (it is the base).</summary>
        public bool HasSdeNames { get; }

        /// <summary>Whether game names (ships, items, skills) default to the translated SDE names
        /// or to English for this language. This reflects each community's convention — Korean
        /// players navigate by the English names they know from the client, killboards, and fitting
        /// tools (Discussion #79), while Chinese players expect the translated names. Users can
        /// override via <c>Settings.UI.UseLocalizedGameNames</c>; this is only the default.</summary>
        public bool LocalizedGameNamesDefault { get; }

        public LanguageInfo(string code, string displayName, bool hasSdeNames,
            bool localizedGameNamesDefault = true)
        {
            Code = code;
            DisplayName = displayName;
            HasSdeNames = hasSdeNames;
            LocalizedGameNamesDefault = hasSdeNames && localizedGameNamesDefault;
        }
    }

    public static class LanguageRegistry
    {
        /// <summary>
        /// Every supported language. ADD NEW LANGUAGES HERE (one line) — see class remarks.
        /// English is first and is the fallback/base (no SDE datafile needed).
        /// </summary>
        public static readonly IReadOnlyList<LanguageInfo> All = new[]
        {
            new LanguageInfo("en",    "English",                       hasSdeNames: false),
            new LanguageInfo("zh-CN", "简体中文 (Simplified Chinese)", hasSdeNames: true),
            new LanguageInfo("ko",    "한국어 (Korean)",                hasSdeNames: true,
                localizedGameNamesDefault: false),
        };

        /// <summary>Locale codes of every supported language, in display order.</summary>
        public static string[] Codes => All.Select(l => l.Code).ToArray();

        /// <summary>Codes of languages that ship a bundled SDE-name datafile (everything except English).</summary>
        public static string[] SdeLanguages => All.Where(l => l.HasSdeNames).Select(l => l.Code).ToArray();

        /// <summary>Native display name for a code, or the code itself if unknown.</summary>
        public static string DisplayName(string code)
            => All.FirstOrDefault(l => l.Code == code)?.DisplayName ?? code;

        /// <summary>Whether game names (ships/items/skills) should use the translated SDE names
        /// by default for this language. False for unknown codes and for English.</summary>
        public static bool LocalizedGameNamesDefault(string code)
            => All.FirstOrDefault(l => l.Code == code)?.LocalizedGameNamesDefault ?? false;
    }
}
