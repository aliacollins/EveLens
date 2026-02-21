// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;

namespace EVEMon.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ParentAttribute : Attribute
    {
        public ParentAttribute(params object[] parents)
        {
            Parents = parents?.Where(x => x as Enum != null).Select(x => (Enum)x).ToArray();
        }

        public Enum[] Parents { get; }
    }
}
