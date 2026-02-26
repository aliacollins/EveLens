// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Interfaces
{
    /// <summary>
    /// Abstracts the global collection access that Settings.Import/Export needs.
    /// Minimum viable seam for making Settings testable without EveLensClient.
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
