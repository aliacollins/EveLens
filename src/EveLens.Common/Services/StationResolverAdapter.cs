// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using System.Threading.Tasks;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    public sealed class StationResolverAdapter : IStationResolver
    {
        public object? GetStation(long id, long characterId = 0)
        {
            CCPCharacter character = FindCharacter(characterId);
            return EveIDToStation.GetIDToStation(id, character);
        }

        public async Task<object?> GetStationAsync(long id, long characterId = 0)
        {
            CCPCharacter character = FindCharacter(characterId);
            return await EveIDToStation.GetIDToStationAsync(id, character);
        }

        private static CCPCharacter FindCharacter(long characterId)
        {
            if (characterId == 0)
                return null;

            return AppServices.Characters.OfType<CCPCharacter>()
                .FirstOrDefault(c => c.CharacterID == characterId);
        }
    }
}
