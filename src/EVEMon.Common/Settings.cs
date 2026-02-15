using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.Xsl;
using EVEMon.Common.Attributes;
using EVEMon.Common.CloudStorageServices;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Helpers;
using EVEMon.Common.Scheduling;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Events;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common
{
    /// <summary>
    /// Stores EVEMon's current settings and writes them to the settings file when necessary.
    /// </summary>
    [EnforceUIThreadAffinity]
    public static partial class Settings
    {
        private static volatile bool s_savePending;
        private static DateTime s_nextSaveTime;
        private static XslCompiledTransform? s_settingsTransform;
        private static volatile SerializableSettings? s_settings;
        private static volatile SmartSettingsManager? s_smartSettingsManager;
        private static IDisposable? s_thirtySecondTickSubscription;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Settings()
        {
            // ESI credentials loaded from esi-credentials.json (gitignored)
            // Create your own at https://developers.eveonline.com/
            LoadESICredentials();
            UI = new UISettings();
            G15 = new G15Settings();
            Proxy = new ProxySettings();
            Updates = new UpdateSettings();
            Calendar = new CalendarSettings();
            Exportation = new ExportationSettings();
            MarketPricer = new MarketPricerSettings();
            Notifications = new NotificationSettings();
            LoadoutsProvider = new LoadoutsProviderSettings();
            PortableEveInstallations = new PortableEveInstallationsSettings();
            CloudStorageServiceProvider = new CloudStorageServiceProviderSettings();

            // Use ThirtySecondTick instead of TimerTick for save checks
            // Saves are already delayed by 10 seconds, so checking every 30s is sufficient
            s_thirtySecondTickSubscription = AppServices.EventAggregator?.Subscribe<ThirtySecondTickEvent>(
                e => EveMonClient_TimerTick(null, EventArgs.Empty));
        }

        // Default ESI credentials for EVEMon - registered by Alia Collins
        // Users can override via esi-credentials.json in app directory
        private const string DefaultClientID = "e87550c5642e4de0bac3b124d110ca7a";
        private const string DefaultClientSecret = "eat_qpDb4LCQRKRcGWKNfoLhcrRlqQo75Aes_3fgYhF";

        /// <summary>
        /// Handles the TimerTick event of the EveMonClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static async void EveMonClient_TimerTick(object? sender, EventArgs e)
        {
            try
            {
                await UpdateOnOneSecondTickAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
        }

        /// <summary>
        /// Gets true if we're currently restoring the settings.
        /// </summary>
        public static bool IsRestoring { get; private set; }


        #region The very settings

        /// <summary>
        /// Gets or sets the SSO client ID.
        /// </summary>
        public static string SSOClientID { get; private set; } = null!;

        /// <summary>
        /// Gets or sets the SSO secret key.
        /// </summary>
        public static string SSOClientSecret { get; private set; } = null!;

        /// <summary>
        /// Gets or sets the compatibility mode.
        /// </summary>
        public static CompatibilityMode Compatibility { get; private set; }

        /// <summary>
        /// Gets the settings for updates.
        /// </summary>
        public static UpdateSettings Updates { get; private set; }

        /// <summary>
        /// Gets the settings for UI (look'n feel)
        /// </summary>
        public static UISettings UI { get; private set; }

        /// <summary>
        /// Gets the settings for the G15 keyboard.
        /// </summary>
        public static G15Settings G15 { get; private set; }

        /// <summary>
        /// Gets the settings for the notifications (alerts).
        /// </summary>
        public static NotificationSettings Notifications { get; private set; }

        /// <summary>
        /// Gets the settings for the portable EVE installations.
        /// </summary>
        public static PortableEveInstallationsSettings PortableEveInstallations { get; private set; }

        /// <summary>
        /// Gets the calendar settings.
        /// </summary>
        public static CalendarSettings Calendar { get; private set; }

        /// <summary>
        /// Gets or sets the exportation settings.
        /// </summary>
        public static ExportationSettings Exportation { get; private set; }

        /// <summary>
        /// Gets or sets the custom proxy settings.
        /// </summary>
        public static ProxySettings Proxy { get; private set; }

        /// <summary>
        /// Gets the market pricer settings.
        /// </summary>
        public static MarketPricerSettings MarketPricer { get; private set; }

        /// <summary>
        /// Gets the loadouts provider settings.
        /// </summary>
        public static LoadoutsProviderSettings LoadoutsProvider { get; private set; }

        /// <summary>
        /// Gets the cloud storage service provider settings.
        /// </summary>
        public static CloudStorageServiceProviderSettings CloudStorageServiceProvider { get; private set; }

        #endregion


        #region Import

        /// <summary>
        /// Imports the provided serialization object.
        /// </summary>
        private static void Import()
        {
            AppServices.TraceService?.Trace("begin");

            // When null, we just reset
            if (s_settings == null)
                s_settings = new SerializableSettings();

            try
            {
                // API settings
                SSOClientID = s_settings.SSOClientID ?? string.Empty;
                SSOClientSecret = s_settings.SSOClientSecret ?? string.Empty;

                // User settings
                UI = s_settings.UI;
                G15 = s_settings.G15;
                Proxy = s_settings.Proxy;
                Updates = s_settings.Updates;
                Calendar = s_settings.Calendar;
                Exportation = s_settings.Exportation;
                MarketPricer = s_settings.MarketPricer;
                Notifications = s_settings.Notifications;
                Compatibility = s_settings.Compatibility;
                LoadoutsProvider = s_settings.LoadoutsProvider;
                PortableEveInstallations = s_settings.PortableEveInstallations;
                CloudStorageServiceProvider = s_settings.CloudStorageServiceProvider;

                // Scheduler
                Scheduler.Import(s_settings.Scheduler);
            }
            finally
            {
                AppServices.TraceService?.Trace("done");

                // Notify the subscribers
                AppServices.TraceService?.Trace("SettingsChanged");
                AppServices.EventAggregator?.Publish(SettingsChangedEvent.Instance);
                AppServices.EventAggregator?.Publish(CommonEvents.SettingsChangedEvent.Instance);
            }
        }

        #endregion


        #region Save

        /// <summary>
        /// Every timer tick, checks whether we should save the settings every 10s.
        /// </summary>
        private static async Task UpdateOnOneSecondTickAsync()
        {
            // Is a save requested and is the last save older than 10s ?
            if (s_savePending && DateTime.UtcNow > s_nextSaveTime)
                await SaveImmediateAsync();
        }

        /// <summary>
        /// Saves settings to disk.
        /// </summary>
        /// <remarks>
        /// Saves will be cached for 10 seconds to avoid thrashing the disk when this method is called very rapidly
        /// such as at startup. If a save is currently pending, no action is needed. 
        /// </remarks>
        public static void Save()
        {
            if (IsRestoring)
                return;

            if (s_smartSettingsManager != null)
            {
                // SmartSettingsManager owns the full pipeline: Export→Serialize→Write
                s_smartSettingsManager.Save();
                return;
            }

            s_savePending = true;
        }

        /// <summary>
        /// Saves settings immediately.
        /// </summary>
        /// <remarks>
        /// When UsingJsonFormat is true (JSON is source of truth):
        ///   - Saves only to JSON files (fast, no XML overhead)
        /// When UsingJsonFormat is false (migration in progress):
        ///   - Saves to both XML and JSON (ensures compatibility)
        /// </remarks>
        public static async Task SaveImmediateAsync()
        {
            // Prevents the saving if we are restoring the settings at that time
            if (IsRestoring)
                return;

            // If SmartSettingsManager is active, it owns the full pipeline exclusively
            if (s_smartSettingsManager != null)
            {
                await s_smartSettingsManager.SaveImmediateAsync();
                return;
            }

            // Reset flags
            s_savePending = false;
            s_nextSaveTime = DateTime.UtcNow.AddSeconds(10);

            AppServices.TraceService?.Trace($"begin - UsingJsonFormat={UsingJsonFormat}");

            try
            {
                // Export settings on UI thread (required for collection access)
                SerializableSettings settings = Export();
                AppServices.TraceService?.Trace("Export done");

                if (UsingJsonFormat)
                {
                    // JSON is source of truth - save only to JSON (faster)
                    await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);
                    AppServices.TraceService?.Trace("JSON save complete (JSON-only mode)");
                }
                else
                {
                    // Migration in progress - save to both XML and JSON
                    // Serialize to MemoryStream on background thread to avoid UI freeze
                    byte[] serializedData = await Task.Run(() =>
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            XmlSerializer xs = new XmlSerializer(typeof(SerializableSettings));
                            xs.Serialize(ms, settings);
                            return ms.ToArray();
                        }
                    });

                    AppServices.TraceService?.Trace($"Serialized {serializedData.Length} bytes to XML");

                    // Write to XML file (atomic via temp file)
                    await FileHelper.OverwriteOrWarnTheUserAsync(AppServices.DataStore.SettingsFilePath,
                        async fs =>
                        {
                            await fs.WriteAsync(serializedData, 0, serializedData.Length);
                            await fs.FlushAsync();
                            return true;
                        });
                    AppServices.TraceService?.Trace("XML file written");

                    // Also save to JSON format (keeps JSON files in sync with XML)
                    await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);
                    AppServices.TraceService?.Trace("JSON save complete (dual-write mode)");
                }
            }
            catch (Exception exception)
            {
                AppServices.TraceService?.Trace($"Error: {exception.Message}");
                ExceptionHandler.LogException(exception, true);
            }
        }

        /// <summary>
        /// Shuts down settings services, disposing the SmartSettingsManager if active.
        /// Called during application shutdown.
        /// </summary>
        public static void Shutdown()
        {
            s_thirtySecondTickSubscription?.Dispose();
            s_thirtySecondTickSubscription = null;
            s_smartSettingsManager?.Dispose();
            s_smartSettingsManager = null;
        }

        #endregion
    }
}
