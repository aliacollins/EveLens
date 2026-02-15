using System.Collections.Generic;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Production implementation of <see cref="ISettingsDataStore"/> that delegates to EveMonClient.
    /// </summary>
    internal sealed class EveMonClientDataStore : ISettingsDataStore
    {
        internal static readonly EveMonClientDataStore Instance = new();

        public void ResetCollections() => EveMonClient.ResetCollections();

        public void ImportCharacters(IEnumerable<SerializableSettingsCharacter> characters)
            => AppServices.Characters.Import(characters);

        public void ImportESIKeys(IEnumerable<SerializableESIKey> keys)
            => AppServices.ESIKeys.Import(keys);

        public void ImportPlans(ICollection<SerializablePlan> plans)
            => AppServices.Characters.ImportPlans(plans);

        public void ImportMonitoredCharacters(ICollection<MonitoredCharacterSettings> monitored)
            => AppServices.MonitoredCharacters.Import(monitored);

        public IEnumerable<SerializableSettingsCharacter> ExportCharacters()
            => AppServices.Characters.Export();

        public IEnumerable<SerializableESIKey> ExportESIKeys()
            => AppServices.ESIKeys.Export();

        public IEnumerable<SerializablePlan> ExportPlans()
            => AppServices.Characters.ExportPlans();

        public IEnumerable<MonitoredCharacterSettings> ExportMonitoredCharacters()
            => AppServices.MonitoredCharacters.Export();

        public string SettingsFilePath => EveMonClient.SettingsFileNameFullPath;

        public string DataDirectory => EveMonClient.EVEMonDataDir;

        public string FileVersion => EveMonClient.FileVersionInfo?.FileVersion ?? "0.0.0.0";

        public bool IsClosed => AppServices.Closed;
    }
}
