using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    public sealed class ApplicationPathsAdapter : IApplicationPaths
    {
        public string DataDirectory => EveMonClient.EVEMonDataDir;

        public string XmlCacheDirectory => EveMonClient.EVEMonXmlCacheDir;

        public string ImageCacheDirectory => EveMonClient.EVEMonImageCacheDir;

        public string SettingsFilePath => EveMonClient.SettingsFileNameFullPath;

        public string TraceFilePath => EveMonClient.TraceFileNameFullPath;
    }
}
