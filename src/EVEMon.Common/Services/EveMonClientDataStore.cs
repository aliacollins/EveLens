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
            => EveMonClient.Characters.Import(characters);

        public void ImportESIKeys(IEnumerable<SerializableESIKey> keys)
            => EveMonClient.ESIKeys.Import(keys);

        public void ImportPlans(ICollection<SerializablePlan> plans)
            => EveMonClient.Characters.ImportPlans(plans);

        public void ImportMonitoredCharacters(ICollection<MonitoredCharacterSettings> monitored)
            => EveMonClient.MonitoredCharacters.Import(monitored);

        public IEnumerable<SerializableSettingsCharacter> ExportCharacters()
            => EveMonClient.Characters.Export();

        public IEnumerable<SerializableESIKey> ExportESIKeys()
            => EveMonClient.ESIKeys.Export();

        public IEnumerable<SerializablePlan> ExportPlans()
            => EveMonClient.Characters.ExportPlans();

        public IEnumerable<MonitoredCharacterSettings> ExportMonitoredCharacters()
            => EveMonClient.MonitoredCharacters.Export();

        public string SettingsFilePath => EveMonClient.SettingsFileNameFullPath;

        public string DataDirectory => EveMonClient.EVEMonDataDir;

        public string FileVersion => EveMonClient.FileVersionInfo?.FileVersion ?? "0.0.0.0";

        public bool IsClosed => EveMonClient.Closed;
    }
}
