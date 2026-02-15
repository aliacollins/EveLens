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
