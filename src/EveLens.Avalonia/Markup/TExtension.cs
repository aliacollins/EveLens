// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia.Markup.Xaml;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Markup
{
    /// <summary>
    /// XAML markup extension that resolves a localized UI string from the central
    /// <see cref="Loc"/> dictionary. Lets AXAML stay declarative while keeping every
    /// user-facing string in one place:
    /// <code>
    ///   xmlns:loc="clr-namespace:EveLens.Avalonia.Markup"
    ///   &lt;TextBlock Text="{loc:T Plan.Skill}" /&gt;
    ///   &lt;Button Content="{loc:T Action.Save}" /&gt;
    /// </code>
    /// The language is selected once at startup (<c>Loc.Language</c> is set before any view is
    /// created, and changing it triggers an app restart), so resolving at load time is sufficient —
    /// no runtime binding/INotify is required.
    ///
    /// If the key is missing, <see cref="Loc.Get"/> falls back to English and then to the key
    /// itself, so a typo surfaces as the raw key on screen rather than a crash.
    /// </summary>
    public sealed class TExtension : MarkupExtension
    {
        public TExtension() { }

        public TExtension(string key)
        {
            Key = key;
        }

        /// <summary>The translation key (e.g. "Action.Save"). Required.</summary>
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return string.IsNullOrEmpty(Key) ? string.Empty : Loc.Get(Key);
        }
    }
}
