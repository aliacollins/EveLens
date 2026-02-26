// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;

namespace EveLens.Common.Interfaces
{
    public interface IStaticSkill
    {
        int ID { get; }
        int ArrayIndex { get; }
        string Name { get; }

        long Rank { get; }
        long Cost { get; }
        StaticSkillGroup Group { get; }

        Collection<StaticSkillLevel> Prerequisites { get; }

        EveAttribute PrimaryAttribute { get; }
        EveAttribute SecondaryAttribute { get; }

        Skill ToCharacter(Character character);
    }
}