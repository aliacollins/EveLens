// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Interfaces;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Production implementation of <see cref="ISettingsDataStore"/> that delegates to EveLensClient.
    /// </summary>
    internal sealed class EveLensClientDataStore : ISettingsDataStore
    {
        internal static readonly EveLensClientDataStore Instance = new();

        public void ResetCollections() => EveLensClient.ResetCollections();

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

        public string SettingsFilePath => EveLensClient.SettingsFileNameFullPath;

        public string DataDirectory => EveLensClient.EveLensDataDir;

        public string FileVersion => EveLensClient.FileVersionInfo?.FileVersion ?? "0.0.0.0";

        public bool IsClosed => AppServices.Closed;
    }
}
