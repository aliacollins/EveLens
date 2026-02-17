// Settings import/export, restore, and data operations (extracted from Settings.cs)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EVEMon.Core.Enumerations;
using EVEMon.Common.Collections;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Models.Extended;
using EVEMon.Common.Notifications;
using EVEMon.Common.Scheduling;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Events;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common
{
    public static partial class Settings
    {
        /// <summary>
        /// Creates new empty Settings file, overwriting the existing file.
        /// </summary>
        public static async Task ResetAsync()
        {
            // Clear JSON files (they'll be recreated empty on next save)
            SettingsFileManager.ClearAllJsonFiles();

            s_settings = new SerializableSettings();

            IsRestoring = true;
            Import();
            await ImportDataAsync();
            IsRestoring = false;
        }

        /// <summary>
        /// Asynchronously imports the settings.
        /// </summary>
        /// <param name="serial">The serial.</param>
        /// <param name="saveImmediate">if set to <c>true</c> [save immediate].</param>
        /// <returns></returns>
        public static async Task ImportAsync(SerializableSettings serial, bool saveImmediate = false)
        {
            s_settings = serial;

            Import();
            IsRestoring = true;
            if (saveImmediate)
                await SaveImmediateAsync();
            IsRestoring = false;
        }

        /// <summary>
        /// Asynchronously imports the data.
        /// </summary>
        /// <returns></returns>
        public static async Task ImportDataAsync()
        {
            // Quit if the client has been shut down
            if (AppServices.DataStore.IsClosed)
                return;

            IsRestoring = true;
            await TaskHelper.RunCPUBoundTaskAsync(() => ImportData());
            await SaveImmediateAsync();
            IsRestoring = false;
        }

        /// <summary>
        /// Imports the data.
        /// </summary>
        private static void ImportData()
        {
            AppServices.TraceService?.Trace("begin");

            if (s_settings == null)
                s_settings = new SerializableSettings();

            AppServices.DataStore.ResetCollections();
            AppServices.DataStore.ImportCharacters(s_settings.Characters);
            AppServices.DataStore.ImportESIKeys(s_settings.ESIKeys);
            AppServices.DataStore.ImportPlans(s_settings.Plans);
            AppServices.DataStore.ImportMonitoredCharacters(s_settings.MonitoredCharacters);

            OnImportCompleted();

            AppServices.TraceService?.Trace("done");

            // Notify the subscribers
            AppServices.TraceService?.Trace("SettingsChanged");
            AppServices.EventAggregator?.Publish(SettingsChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.SettingsChangedEvent.Instance);
        }

        /// <summary>
        /// Corrects the imported data and add missing stuff.
        /// </summary>
        private static void OnImportCompleted()
        {
            // Add missing notification behaviours
            foreach (NotificationCategory category in EnumExtensions.GetValues<NotificationCategory>()
                .Where(category => !Notifications.Categories.ContainsKey(category) && category.HasHeader()))
            {
                Notifications.Categories[category] = new NotificationCategorySettings();
            }

            // Add missing ESI methods update periods
            foreach (Enum method in ESIMethods.Methods.Where(method => method.GetUpdatePeriod() != null)
                .Where(method => !Updates.Periods.ContainsKey(method.ToString())))
                Updates.Periods.Add(method.ToString(), method.GetUpdatePeriod().DefaultPeriod);

            // Initialize or add missing columns
            InitializeOrAddMissingColumns();

            // Removes redundant notification behaviours
            List<KeyValuePair<NotificationCategory, NotificationCategorySettings>> notifications =
                Notifications.Categories.ToList();
            foreach (KeyValuePair<NotificationCategory, NotificationCategorySettings> notification in notifications
                .Where(notification => !notification.Key.HasHeader()))
            {
                Notifications.Categories.Remove(notification.Key);
            }

            // Removes redundant windows locations
            List<KeyValuePair<string, WindowLocationSettings>> locations = UI.WindowLocations.ToList();
            foreach (KeyValuePair<string, WindowLocationSettings> windowLocation in locations
                .Where(windowLocation => windowLocation.Key == "FeaturesWindow"))
            {
                UI.WindowLocations.Remove(windowLocation.Key);
            }

            // Removes redundant splitters
            List<KeyValuePair<string, int>> splitters = UI.Splitters.ToList();
            foreach (KeyValuePair<string, int> splitter in splitters
                .Where(splitter => splitter.Key == "EFTLoadoutImportationForm"))
            {
                UI.Splitters.Remove(splitter.Key);
            }
        }

        /// <summary>
        /// Initializes or adds missing columns.
        /// </summary>
        private static void InitializeOrAddMissingColumns()
        {
            // Initializes the plan columns or adds missing ones
            UI.PlanWindow.Columns.AddRange(UI.PlanWindow.DefaultColumns);

            // Initializes the asset columns or adds missing ones
            UI.MainWindow.Assets.Columns.AddRange(UI.MainWindow.Assets.DefaultColumns);

            // Initializes the market order columns or adds missing ones
            UI.MainWindow.MarketOrders.Columns.AddRange(UI.MainWindow.MarketOrders.DefaultColumns);

            // Initializes the contracts columns or adds missing ones
            UI.MainWindow.Contracts.Columns.AddRange(UI.MainWindow.Contracts.DefaultColumns);

            // Initializes the wallet journal columns or adds missing ones
            UI.MainWindow.WalletJournal.Columns.AddRange(UI.MainWindow.WalletJournal.DefaultColumns);

            // Initializes the wallet transactions columns or adds missing ones
            UI.MainWindow.WalletTransactions.Columns.AddRange(UI.MainWindow.WalletTransactions.DefaultColumns);

            // Initializes the industry jobs columns or adds missing ones
            UI.MainWindow.IndustryJobs.Columns.AddRange(UI.MainWindow.IndustryJobs.DefaultColumns);

            // Initializes the planetary colonies columns or adds missing ones
            UI.MainWindow.Planetary.Columns.AddRange(UI.MainWindow.Planetary.DefaultColumns);

            // Initializes the research points columns or adds missing ones
            UI.MainWindow.Research.Columns.AddRange(UI.MainWindow.Research.DefaultColumns);

            // Initializes the EVE mail messages columns or adds missing ones
            UI.MainWindow.EVEMailMessages.Columns.AddRange(UI.MainWindow.EVEMailMessages.DefaultColumns);

            // Initializes the EVE notifications columns or adds missing ones
            UI.MainWindow.EVENotifications.Columns.AddRange(UI.MainWindow.EVENotifications.DefaultColumns);
        }

        /// <summary>
        /// Creates a serializable version of the settings.
        /// </summary>
        /// <returns></returns>
        public static SerializableSettings Export()
        {
            AppServices.TraceService?.Trace("begin");

            SerializableSettings serial = new SerializableSettings
            {
                SSOClientID = SSOClientID,
                SSOClientSecret = SSOClientSecret,
                Revision = Revision,
                Compatibility = Compatibility,
                ForkId = OurForkId,
                ForkVersion = AppServices.DataStore.FileVersion,
                Scheduler = Scheduler.Export(),
                Calendar = Calendar,
                CloudStorageServiceProvider = CloudStorageServiceProvider,
                PortableEveInstallations = PortableEveInstallations,
                LoadoutsProvider = LoadoutsProvider,
                MarketPricer = MarketPricer,
                Notifications = Notifications,
                Exportation = Exportation,
                Updates = Updates,
                Proxy = Proxy,
                G15 = G15,
                UI = UI,
                EsiScopePreset = EsiScopePreset
            };

            foreach (var scope in EsiCustomScopes)
                serial.EsiCustomScopes.Add(scope);

            serial.Characters.AddRange(AppServices.DataStore.ExportCharacters());
            AppServices.TraceService?.Trace($"{serial.Characters.Count} characters exported");
            serial.ESIKeys.AddRange(AppServices.DataStore.ExportESIKeys());
            serial.Plans.AddRange(AppServices.DataStore.ExportPlans());
            AppServices.TraceService?.Trace($"{serial.Plans.Count} plans exported");
            serial.MonitoredCharacters.AddRange(AppServices.DataStore.ExportMonitoredCharacters());

            AppServices.TraceService?.Trace("done");
            return serial;
        }

        /// <summary>
        /// Asynchronously restores the settings from the specified file.
        /// Supports both JSON (.json) and XML (.xml) backup formats.
        /// </summary>
        /// <param name="filename">The fully qualified filename of the settings file to load</param>
        /// <returns>The Settings object loaded</returns>
        public static async Task RestoreAsync(string filename)
        {
            const string Caption = "Restore Settings";
            string extension = Path.GetExtension(filename).ToLowerInvariant();

            if (extension == ".json" && SettingsFileManager.IsJsonBackupFile(filename))
            {
                // Restore from JSON backup format
                bool success = await SettingsFileManager.ImportBackupAsync(filename);
                if (!success)
                {
                    AppServices.TraceService?.Trace("Failed to import JSON backup");
                    return;
                }

                // Load from the imported JSON files
                AppServices.TraceService?.Trace("JSON backup imported - loading settings from JSON files");
                s_settings = await SettingsFileManager.LoadToSerializableSettingsAsync();

                if (s_settings == null)
                {
                    AppServices.TraceService?.Trace("Failed to load from imported JSON backup");
                    AppServices.DialogService.ShowMessage(
                        "Failed to load the imported backup. The backup file may be corrupted.",
                        Caption, DialogButtons.OK, DialogIcon.Error);
                    return;
                }

                // JSON is now our source of truth
                UsingJsonFormat = true;
                AppServices.TraceService?.Trace($"Restored from JSON backup: {s_settings.Characters.Count} characters");
            }
            else
            {
                // Restore from XML backup format
                // First, read file content to check for fork migration
                string? fileContent = null;
                try
                {
                    fileContent = File.ReadAllText(filename);
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"RestoreAsync: Failed to read backup file: {ex.Message}");
                    AppServices.DialogService.ShowMessage(
                        $"Failed to read the backup file: {ex.Message}",
                        Caption, DialogButtons.OK, DialogIcon.Error);
                    return;
                }

                // Check for fork migration (user restoring from peterhaneve or other fork)
                var migration = DetectForkMigration(fileContent);

                if (migration.MigrationDetected)
                {
                    AppServices.TraceService?.Trace($"RestoreAsync: Fork migration detected in backup - fork={migration.DetectedForkId}, revision={migration.DetectedRevision}");

                    // Warn user and clear ESI keys from the content before deserializing
                    string message = @"This backup appears to be from a different version of EVEMon.

The ESI authentication tokens in this backup won't work with this version of EVEMon (they're tied to the original application).

Your skill plans and settings will be restored, but you'll need to re-add your characters:
  1. Go to File → Add Character
  2. Log in with your EVE account

Do you want to continue?";

                    DialogChoice result = AppServices.DialogService.ShowMessage(message, "Restore from Different Fork",
                        DialogButtons.YesNo, DialogIcon.Warning);

                    if (result != DialogChoice.Yes)
                    {
                        AppServices.TraceService?.Trace("RestoreAsync: User cancelled fork migration restore");
                        return;
                    }

                    // Clear ESI keys from the content
                    fileContent = Regex.Replace(fileContent,
                        @"<esiKeys>.*?</esiKeys>",
                        "<esiKeys></esiKeys>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase);

                    MigrationFromOtherForkDetected = true;
                }

                // Deserialize the (possibly modified) content
                s_settings = Util.DeserializeXmlFromString<SerializableSettings>(fileContent, SettingsTransform);

                // Loading from file failed, we abort and keep our current settings
                if (s_settings == null)
                {
                    AppServices.TraceService?.Trace("RestoreAsync: Failed to deserialize backup");
                    AppServices.DialogService.ShowMessage(
                        "Failed to load the backup file. The file may be corrupted or in an incompatible format.",
                        Caption, DialogButtons.OK, DialogIcon.Error);
                    return;
                }

                AppServices.TraceService?.Trace($"RestoreAsync: Loaded XML backup with {s_settings.Characters.Count} characters");

                // Clear JSON files - they'll be recreated from restored XML
                SettingsFileManager.ClearAllJsonFiles();

                // Save restored settings directly to JSON (bypasses NeedsMigration check)
                await SettingsFileManager.SaveFromSerializableSettingsAsync(s_settings);
                UsingJsonFormat = true;
                AppServices.TraceService?.Trace("RestoreAsync: Saved restored settings to JSON");
            }

            IsRestoring = true;
            Import();
            await ImportDataAsync();
            IsRestoring = false;

            // Mark all ESI keys as needing re-auth (tokens are stale after restore)
            foreach (ESIKey key in AppServices.ESIKeys)
            {
                key.HasError = true;
                // Clear refresh token to prevent auto-refresh attempts with stale tokens
                key.ClearRefreshToken();
            }

            AppServices.TraceService?.Trace($"RestoreAsync: Complete - UsingJsonFormat={UsingJsonFormat}, marked {AppServices.ESIKeys.Count} keys for re-auth");
        }

        /// <summary>
        /// Exports settings to the specified location.
        /// Supports both JSON (.json) and XML (.xml) formats based on file extension.
        /// </summary>
        /// <param name="copyFileName">The fully qualified filename of the destination file</param>
        public static async Task CopySettingsAsync(string copyFileName)
        {
            // Export current in-memory settings (always fresh)
            SerializableSettings settings = Export();

            // Check file extension to determine format
            string extension = Path.GetExtension(copyFileName).ToLowerInvariant();
            if (extension == ".json")
            {
                // Export to JSON backup format
                await SettingsFileManager.ExportBackupAsync(copyFileName, settings);
                AppServices.TraceService?.Trace($"CopySettingsAsync: Exported to JSON backup: {copyFileName}");
            }
            else
            {
                // Export to XML format - serialize current settings, don't copy potentially stale file
                byte[] serializedData = await Task.Run(() =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(SerializableSettings));
                        xs.Serialize(ms, settings);
                        return ms.ToArray();
                    }
                });

                await FileHelper.OverwriteOrWarnTheUserAsync(copyFileName,
                    async fs =>
                    {
                        await fs.WriteAsync(serializedData, 0, serializedData.Length);
                        await fs.FlushAsync();
                        return true;
                    });

                AppServices.TraceService?.Trace($"CopySettingsAsync: Exported {serializedData.Length} bytes to XML backup: {copyFileName}");
            }
        }
    }
}
