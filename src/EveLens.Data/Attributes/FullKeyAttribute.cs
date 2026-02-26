// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Attributes
{
    /// <summary>
    /// An attribute used to mark the API methods which require a full API key.
    /// </summary>
    public sealed class FullKeyAttribute : Attribute
    {
    }
}