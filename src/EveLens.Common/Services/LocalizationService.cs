// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Central UI-string localization. Every user-facing string lives in one place — the
    /// <c>Resources/ui-strings-&lt;code&gt;.txt</c> data files (one per language, in the simple
    /// <c>Key = value</c> format translators produce). They are embedded in the assembly and loaded
    /// on first use. Languages are declared once in <see cref="LanguageRegistry"/>.
    ///
    /// Usage: <c>Loc.Get("Action.Save")</c> in code, or <c>{loc:T Action.Save}</c> in AXAML.
    /// Missing keys fall back to English, then to the key itself (so a typo shows the raw key,
    /// never a crash).
    /// </summary>
    public static class Loc
    {
        private static string _language = "en";
        private static readonly Dictionary<string, Dictionary<string, string>> s_translations = new();
        private static readonly object s_lock = new();
        private static bool s_loaded;

        public static string Language
        {
            get => _language;
            set => _language = value ?? "en";
        }

        public static string Get(string key)
        {
            EnsureLoaded();

            if (s_translations.TryGetValue(_language, out var table) &&
                table.TryGetValue(key, out var value))
                return value;

            if (_language != "en" &&
                s_translations.TryGetValue("en", out var fallback) &&
                fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return key;
        }

        /// <summary>Locale codes of every supported language (from <see cref="LanguageRegistry"/>).</summary>
        public static string[] AvailableLanguages => LanguageRegistry.Codes;

        /// <summary>Native display name for a language code.</summary>
        public static string GetLanguageDisplayName(string code) => LanguageRegistry.DisplayName(code);

        /// <summary>
        /// Exposes the registered key/value table for a language. For tests and tooling that
        /// validate translation parity; returns null if the language is not registered.
        /// </summary>
        internal static IReadOnlyDictionary<string, string>? GetTable(string lang)
        {
            EnsureLoaded();
            return s_translations.TryGetValue(lang, out var table) ? table : null;
        }

        private static void EnsureLoaded()
        {
            if (s_loaded) return;
            lock (s_lock)
            {
                if (s_loaded) return;
                foreach (var lang in LanguageRegistry.Codes)
                {
                    var table = LoadStringsFor(lang);
                    if (table != null)
                        s_translations[lang] = table;
                }
                s_loaded = true;
            }
        }

        /// <summary>
        /// Loads <c>Resources/ui-strings-&lt;lang&gt;.txt</c> from embedded resources into a
        /// key/value dictionary. Format: <c>Key = value</c> per line; lines starting with '#'
        /// or blank are ignored; literal <c>\n</c> in a value becomes a real newline.
        /// </summary>
        private static Dictionary<string, string>? LoadStringsFor(string lang)
        {
            var asm = typeof(Loc).Assembly;
            string resourceName = $"EveLens.Common.Resources.ui-strings-{lang}.txt";

            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;

            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf(" = ", StringComparison.Ordinal);
                if (eq < 0) continue;
                string key = line.Substring(0, eq);
                string value = line.Substring(eq + 3).Replace("\\n", "\n");
                dict[key] = value;
            }
            return dict;
        }
    }
}
