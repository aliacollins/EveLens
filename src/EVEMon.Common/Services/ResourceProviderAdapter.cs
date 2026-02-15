using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Adapter that bridges IResourceProvider to Properties.Resources in EVEMon.Common.
    /// </summary>
    public sealed class ResourceProviderAdapter : IResourceProvider
    {
        public string DatafilesXSLT => Properties.Resources.DatafilesXSLT;

        public string ChrFactions => Properties.Resources.chrFactions;
    }
}
