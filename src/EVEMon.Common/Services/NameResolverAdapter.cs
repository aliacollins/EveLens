using System.Collections.Generic;
using EVEMon.Common.Service;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    public sealed class NameResolverAdapter : INameResolver
    {
        public string GetName(long id, bool bypassCache = false)
        {
            return EveIDToName.GetIDToName(id, bypassCache);
        }

        public IEnumerable<string> GetNames(IEnumerable<long> ids)
        {
            return EveIDToName.GetIDsToNames(ids);
        }

        public string GetRefTypeName(int refTypeId)
        {
            return EveRefType.GetRefTypeIDToName(refTypeId);
        }
    }
}
