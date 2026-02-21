// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Service;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    public sealed class FlagResolverAdapter : IFlagResolver
    {
        public string GetFlagText(int flagId)
        {
            return EveFlag.GetFlagText(flagId);
        }

        public int GetFlagID(string flagName)
        {
            return EveFlag.GetFlagID(flagName);
        }
    }
}
