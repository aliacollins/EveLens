using System.Linq;
using System.Threading.Tasks;
using EVEMon.Common.Models;
using EVEMon.Common.Service;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
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

            return EveMonClient.Characters.OfType<CCPCharacter>()
                .FirstOrDefault(c => c.CharacterID == characterId);
        }
    }
}
