using System.Collections.Generic;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Common.Interfaces
{
    /// <summary>
    /// Abstracts the global collection access that Settings.Import/Export needs.
    /// Minimum viable seam for making Settings testable without EveMonClient.
    /// </summary>
    public interface ISettingsDataStore
    {
        void ResetCollections();
        void ImportCharacters(IEnumerable<SerializableSettingsCharacter> characters);
        void ImportESIKeys(IEnumerable<SerializableESIKey> keys);
        void ImportPlans(ICollection<SerializablePlan> plans);
        void ImportMonitoredCharacters(ICollection<MonitoredCharacterSettings> monitored);

        IEnumerable<SerializableSettingsCharacter> ExportCharacters();
        IEnumerable<SerializableESIKey> ExportESIKeys();
        IEnumerable<SerializablePlan> ExportPlans();
        IEnumerable<MonitoredCharacterSettings> ExportMonitoredCharacters();

        string SettingsFilePath { get; }
        string DataDirectory { get; }
        string FileVersion { get; }
        bool IsClosed { get; }
    }
}
