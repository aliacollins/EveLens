// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.CompilerServices;

// Allow EveLens.Common to access internal members of types that moved here.
// This enables incremental extraction without breaking callers.
[assembly: InternalsVisibleTo("EveLens.Common")]
[assembly: InternalsVisibleTo("EveLens")]
[assembly: InternalsVisibleTo("EveLens.Tests")]
