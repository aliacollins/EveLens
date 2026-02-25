// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

// Settings initialization and deserialization (extracted from Settings.cs)
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.Xsl;
using EVEMon.Common.Helpers;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;
using EVEMon.Core.Enumerations;

namespace EVEMon.Common
{
    public static partial class Settings
    {
        /// <summary>
        /// Gets the current assembly's revision, which is also used for files versioning.
        /// </summary>
        internal static int Revision => Version.Parse(AppServices.DataStore.FileVersion).Revision;

        /// <summary>
        /// Gets whether the settings are currently using JSON format (source of truth).
        /// When true, saves go only to JSON. When false, saves go to XML (migration not complete).
        /// </summary>
        public static bool UsingJsonFormat { get; private set; }

        /// <summary>
        /// Initialization for the EVEMon client settings.
        /// </summary>
        /// <remarks>
        /// Settings loading priority:
        /// 1. JSON format (config.json) - primary if exists
        /// 2. XML format (settings.xml) - fallback, will migrate to JSON
        /// 3. Cloud storage - if configured
        /// 4. Fresh install - create new settings
        /// </remarks>
        public static void Initialize()
        {
            AppServices.TraceService?.Trace("begin");

            // Priority 1: Check if JSON settings exist (new format - source of truth)
            if (SettingsFileManager.JsonSettingsExist())
            {
                AppServices.TraceService?.Trace("JSON settings found, loading from JSON format");
                s_settings = Task.Run(() => SettingsFileManager.LoadToSerializableSettingsAsync()).GetAwaiter().GetResult();

                if (s_settings != null)
                {
                    UsingJsonFormat = true;
                    AppServices.TraceService?.Trace($"Loaded from JSON: {s_settings.Characters.Count} characters");
                }
                else
                {
                    AppServices.TraceService?.Trace("JSON load failed, falling back to XML");
                }
            }

            // Priority 2: Fall back to XML if JSON didn't work
            if (s_settings == null)
            {
                AppServices.TraceService?.Trace("Loading from XML format");
                s_settings = TryDeserializeFromFile();
                AppServices.TraceService?.Trace("TryDeserializeFromFile done");

                // Try to download the settings file from the cloud
                CloudStorageServices.CloudStorageServiceAPIFile? settingsFile = s_settings?.CloudStorageServiceProvider?.Provider == null
                    ? null
                    : Task.Run(() => s_settings.CloudStorageServiceProvider.Provider
                        .DownloadSettingsFileResultAsync()).GetAwaiter().GetResult();
                if (settingsFile != null)
                {
                    AppServices.TraceService?.Trace("Cloud settings downloaded, deserializing");
                    s_settings = TryDeserializeFromFileContent(settingsFile.FileContent);
                }
            }

            // Loading settings
            // If there are none, we create them from scratch
            IsRestoring = true;

            // One-time migration: infer MinimizeToTray from old tray/close enums.
            // Must run before Import() so the migrated value is picked up,
            // and only here (not in Import()) so saves don't re-migrate.
            if (s_settings != null)
                s_settings.UI.MigrateToMinimizeToTray();

            Import();
            IsRestoring = false;

            // If we loaded from XML, migrate to JSON format
            // After migration completes, JSON becomes source of truth
            if (!UsingJsonFormat && s_settings != null)
            {
                TryMigrateToJsonAsync(s_settings).ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && SettingsFileManager.JsonSettingsExist())
                    {
                        UsingJsonFormat = true;
                        AppServices.TraceService?.Trace("Migration complete - JSON is now source of truth");
                    }
                });
            }

            // Initialize SmartSettingsManager for save coalescing
            s_smartSettingsManager = new SmartSettingsManager(
                AppServices.DataStore.DataDirectory,
                AppServices.EventAggregator,
                AppServices.Dispatcher,
                () => Export());
            AppServices.TraceService?.Trace("SmartSettingsManager initialized");

            AppServices.TraceService?.Trace($"done - UsingJsonFormat={UsingJsonFormat}");
        }

        /// <summary>
        /// Attempts to migrate settings from XML to the new JSON file structure.
        /// This runs in the background and doesn't block initialization.
        /// </summary>
        /// <param name="settings">The deserialized XML settings.</param>
        private static async Task TryMigrateToJsonAsync(SerializableSettings settings)
        {
            try
            {
                // Check if migration is needed
                if (!SettingsFileManager.NeedsMigration())
                {
                    if (SettingsFileManager.JsonSettingsExist())
                    {
                        AppServices.TraceService?.Trace("JSON settings already exist, no migration needed");
                    }
                    else if (!SettingsFileManager.LegacySettingsExist())
                    {
                        AppServices.TraceService?.Trace("No legacy settings to migrate");
                    }
                    return;
                }

                if (settings == null)
                {
                    AppServices.TraceService?.Trace("No settings to migrate");
                    return;
                }

                AppServices.TraceService?.Trace("Starting migration from XML to JSON format");
                await SettingsFileManager.MigrateFromXmlAsync(settings);
                AppServices.TraceService?.Trace("Migration to JSON complete");
            }
            catch (Exception ex)
            {
                // Migration failure is not critical - we still have the XML file
                AppServices.TraceService?.Trace($"Migration to JSON failed (non-critical): {ex.Message}");
            }
        }

        /// <summary>
        /// Try to deserialize the settings from a storage server file, prompting the user for errors.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        /// <returns>
        ///   <c>Null</c> if we have been unable to deserialize anything, the generated settings otherwise
        /// </returns>
        private static SerializableSettings? TryDeserializeFromFileContent(string fileContent)
        {
            if (string.IsNullOrWhiteSpace(fileContent))
                return null;

            AppServices.TraceService?.Trace("begin");

            // Gets the revision number of the assembly which generated this file
            int revision = Util.GetRevisionNumber(fileContent);

            // Try to load from a file (when no revision found then it's a pre 1.3.0 version file)
            // Note: revision < 0 means no revision attribute; revision >= 0 is valid (including 0)
            SerializableSettings? settings = revision < 0
                ? (SerializableSettings?)UIHelper.ShowNoSupportMessage()
                : Util.DeserializeXmlFromString<SerializableSettings>(fileContent,
                    SettingsTransform);

            if (settings != null)
            {
                AppServices.TraceService?.Trace("done");
                return settings;
            }

            const string Caption = "Corrupt Settings";

            DialogChoice dr = AppServices.DialogService.ShowMessage(
                $"Loading settings from {CloudStorageServiceProvider.ProviderName} failed." +
                $"{Environment.NewLine}Do you want to use the local settings file?",
                Caption, DialogButtons.YesNo, DialogIcon.Warning);

            if (dr != DialogChoice.No)
                return TryDeserializeFromFile();

            AppServices.DialogService.ShowMessage(
                $"A new settings file will be created.{Environment.NewLine}"
                + @"You may wish then to restore a saved copy of the file.", Caption,
                DialogButtons.OK, DialogIcon.Warning);

            return null;
        }

        /// <summary>
        /// Try to deserialize the settings from a file, prompting the user for errors.
        /// </summary>
        /// <returns><c>Null</c> if we have been unable to load anything from files, the generated settings otherwise</returns>
        private static SerializableSettings? TryDeserializeFromFile()
        {
            string settingsFile = AppServices.DataStore.SettingsFilePath;
            string backupFile = settingsFile + ".bak";

            // If settings file doesn't exists
            // try to recover from the backup
            if (!File.Exists(settingsFile))
                return TryDeserializeFromBackupFile(backupFile);

            AppServices.TraceService?.Trace("begin");

            // Check settings file length
            FileInfo settingsInfo = new FileInfo(settingsFile);
            if (settingsInfo.Length == 0)
                return TryDeserializeFromBackupFile(backupFile);

            // Read file content once - we'll use it for all checks
            string fileContent = File.ReadAllText(settingsFile);

            // Step 1: Detect migration scenario (silent - no UI yet)
            // This checks forkId and revision to determine if user is migrating from another fork
            var migration = DetectForkMigration(fileContent);

            // Step 2: Check revision compatibility
            // revision < 0 means no revision attribute found (ancient pre-1.3.0 file)
            bool revisionCompatible = migration.DetectedRevision >= 0;

            // Step 3: Handle migration scenario (peterhaneve or other fork)
            if (migration.MigrationDetected)
            {
                if (!revisionCompatible)
                {
                    // Settings format too old (ancient pre-1.3.0) - can't migrate
                    ShowMigrationMessage(migration, settingsCanBePreserved: false);
                    return null;
                }

                // Revision is compatible - try to deserialize to confirm settings can be preserved
                SerializableSettings? testSettings = null;
                try
                {
                    testSettings = Util.DeserializeXmlFromString<SerializableSettings>(
                        fileContent, SettingsTransform);
                }
                catch
                {
                    // Deserialization failed
                }

                bool settingsCanBePreserved = testSettings != null;

                // Show migration message with accurate info about preservation
                ShowMigrationMessage(migration, settingsCanBePreserved);

                if (!settingsCanBePreserved)
                {
                    // Couldn't load settings - start fresh
                    return null;
                }

                // Settings loaded successfully - update file (clear ESI keys, add forkId)
                UpdateSettingsFileForMigration(fileContent, settingsFile);

                // IMPORTANT: Also clear ESI keys from the in-memory settings object
                // Otherwise they'd get written back to disk on next save
                // testSettings is guaranteed non-null here because settingsCanBePreserved was true
                int esiKeyCount = testSettings!.ESIKeys.Count;
                testSettings.ESIKeys.Clear();
                AppServices.TraceService?.Trace($"Migration: Cleared {esiKeyCount} ESI keys from memory, preserved {testSettings.Plans.Count} plans");

                // Return the settings we already loaded
                CheckSettingsVersion(testSettings);
                FileHelper.CopyOrWarnTheUser(settingsFile, backupFile);
                AppServices.TraceService?.Trace("done (migration)");
                return testSettings;
            }

            // No migration detected - normal flow for our users

            // Check for ancient settings (pre-1.3.0)
            if (!revisionCompatible)
            {
                // Settings too old - show existing "no support" message
                UIHelper.ShowNoSupportMessage();
                return TryDeserializeFromBackupFile(backupFile);
            }

            // Add forkId if needed (for our existing users who don't have it yet)
            if (migration.NeedsForkIdAdded)
            {
                fileContent = AddForkIdToSettingsFile(fileContent, settingsFile);
            }

            // Deserialize the settings
            SerializableSettings? settings = Util.DeserializeXmlFromString<SerializableSettings>(
                fileContent, SettingsTransform);

            // If the settings loaded OK, make a backup as 'last good settings' and return
            if (settings == null)
                return TryDeserializeFromBackupFile(backupFile);

            CheckSettingsVersion(settings);
            FileHelper.CopyOrWarnTheUser(settingsFile, backupFile);

            AppServices.TraceService?.Trace("done");
            return settings;
        }

        /// <summary>
        /// Try to deserialize from the backup file.
        /// </summary>
        /// <param name="backupFile">The backup file.</param>
        /// <param name="recover">if set to <c>true</c> do a settings recover attempt.</param>
        /// <returns>
        /// 	<c>Null</c> if we have been unable to load anything from files, the generated settings otherwise
        /// </returns>
        private static SerializableSettings? TryDeserializeFromBackupFile(string backupFile, bool recover = true)
        {
            // Backup file doesn't exist
            if (!File.Exists(backupFile))
                return null;

            AppServices.TraceService?.Trace("begin");

            // Check backup settings file length
            FileInfo backupInfo = new FileInfo(backupFile);
            if (backupInfo.Length == 0)
                return null;

            string settingsFile = AppServices.DataStore.SettingsFilePath;

            const string Caption = "Corrupt Settings";
            if (recover)
            {
                // Prompts the user to use the backup
                string fileDate =
                    $"{backupInfo.LastWriteTime.ToLocalTime().ToShortDateString()} " +
                    $"at {backupInfo.LastWriteTime.ToLocalTime().ToShortTimeString()}";
                DialogChoice dialogResult = AppServices.DialogService.ShowMessage(
                    $"The settings file is missing or corrupt. There is a backup available from {fileDate}.{Environment.NewLine}" +
                    @"Do you want to use the backup file?", Caption, DialogButtons.YesNo, DialogIcon.Warning);

                if (dialogResult == DialogChoice.No)
                {
                    AppServices.DialogService.ShowMessage(
                        $"A new settings file will be created.{Environment.NewLine}"
                        + @"You may wish then to restore a saved copy of the file.", Caption,
                        DialogButtons.OK, DialogIcon.Warning);

                    // Save a copy of the corrupt file just in case
                    FileHelper.CopyOrWarnTheUser(backupFile, settingsFile + ".corrupt");

                    return null;
                }
            }

            // Get the revision number of the assembly which generated this file
            // Try to load from a file (when no revision found then it's a pre 1.3.0 version file)
            // Note: revision < 0 means no revision attribute; revision >= 0 is valid (including 0)
            SerializableSettings? settings = Util.GetRevisionNumber(backupFile) < 0
                ? (SerializableSettings?)UIHelper.ShowNoSupportMessage()
                : Util.DeserializeXmlFromFile<SerializableSettings>(backupFile,
                    SettingsTransform);

            // If the settings loaded OK, copy to the main settings file, then copy back to stamp date
            if (settings != null)
            {
                CheckSettingsVersion(settings);
                FileHelper.CopyOrWarnTheUser(backupFile, settingsFile);
                FileHelper.CopyOrWarnTheUser(settingsFile, backupFile);

                AppServices.TraceService?.Trace("done");
                return settings;
            }

            if (recover)
            {
                // Backup failed too, notify the user we have a problem
                AppServices.DialogService.ShowMessage(
                    $"Loading from backup failed.\nA new settings file will be created.{Environment.NewLine}"
                    + @"You may wish then to restore a saved copy of the file.",
                    Caption, DialogButtons.OK, DialogIcon.Warning);

                // Save a copy of the corrupt file just in case
                FileHelper.CopyOrWarnTheUser(backupFile, settingsFile + ".corrupt");
            }
            else
            {
                // Restoring from file failed
                AppServices.DialogService.ShowMessage(
                    $"Restoring settings from {backupFile} failed, the file is corrupted.",
                    Caption, DialogButtons.OK, DialogIcon.Warning);
            }

            return null;
        }

        /// <summary>
        /// Compare the settings version with this version and, when different, update and prompt the user for a backup.
        /// </summary>
        /// <param name="settings"></param>
        private static void CheckSettingsVersion(SerializableSettings settings)
        {
            if (EveMonClient.IsDebugBuild)
                return;

            if (Revision == settings.Revision)
                return;

            int oldRevision = settings.Revision;
            AppServices.TraceService?.Trace($"CheckSettingsVersion: Revision mismatch - settings={oldRevision}, current={Revision}");

            DialogChoice backupSettings = AppServices.DialogService.ShowMessage(
                $"The current EVEMon settings file is from a previous version.{Environment.NewLine}" +
                @"Backup the current file before proceeding (recommended)?",
                @"EVEMon version changed", DialogButtons.YesNo, DialogIcon.Question);

            if (backupSettings == DialogChoice.Yes)
            {
                string? backupPath = AppServices.DialogService.ShowSaveDialog(
                    @"Settings file backup",
                    @"Settings Backup Files (*.bak)|*.bak",
                    $"EVEMon_Settings_{oldRevision}.xml.bak",
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal));

                if (backupPath != null)
                {
                    FileHelper.CopyOrWarnTheUser(AppServices.DataStore.SettingsFilePath, backupPath);
                }
            }

            // IMPORTANT: Update the revision in settings to current version
            // This prevents the backup prompt from appearing on every startup
            settings.Revision = Revision;
            AppServices.TraceService?.Trace($"CheckSettingsVersion: Updated revision from {oldRevision} to {Revision}");
        }

        /// <summary>
        /// Loads ESI credentials - uses embedded defaults, can be overridden via esi-credentials.json.
        /// </summary>
        private static void LoadESICredentials()
        {
            // Start with embedded defaults
            SSOClientID = DefaultClientID;
            SSOClientSecret = DefaultClientSecret;

            // Look for override file in application directory
            string credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "esi-credentials.json");

            if (!File.Exists(credentialsPath))
            {
                // Also check parent directories for development scenarios
                string devPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "esi-credentials.json");
                if (File.Exists(devPath))
                    credentialsPath = devPath;
            }

            // Override with file credentials if present
            if (File.Exists(credentialsPath))
            {
                try
                {
                    string json = File.ReadAllText(credentialsPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("ClientID", out var clientId) && clientId.GetString() is string id && !string.IsNullOrEmpty(id))
                        SSOClientID = id;
                    if (root.TryGetProperty("ClientSecret", out var clientSecret) && clientSecret.GetString() is string secret && !string.IsNullOrEmpty(secret))
                        SSOClientSecret = secret;
                }
                catch
                {
                    // Failed to load override, continue with defaults
                }
            }
        }

        /// <summary>
        /// Gets the XSLT used for transforming rowsets into something deserializable by <see cref="XmlSerializer"/>
        /// </summary>
        private static XslCompiledTransform SettingsTransform
            => s_settingsTransform ?? (s_settingsTransform = Util.LoadXslt(Properties.Resources.SettingsXSLT));
    }
}
