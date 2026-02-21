// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EVEMon.Common.Attributes;
using EVEMon.Common.Collections.Global;
using CommonEvents = EVEMon.Common.Events;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Models.Extended;
using EVEMon.Common.Net;
using EVEMon.Common.Services;
using EVEMon.Common.Threading;
using Microsoft.Extensions.Logging;

namespace EVEMon.Common
{
    /// <summary>
    /// Provides a controller layer for the application. This class manages API querying, objects lifecycle, etc. 
    /// See it as the entry point of the library and its collections as databases with stored procedures (the public ones).
    /// </summary>
    [EnforceUIThreadAffinity]
    public static partial class EveMonClient
    {
        #region Fields

        private static readonly DateTime s_startTime = DateTime.UtcNow;

        internal static TimeSpan Uptime => DateTime.UtcNow - s_startTime;

        private static IEnumerable<string>? s_defaultEvePortraitCacheFolders;
        private static bool s_initialized;
        private static string s_traceFile = null!;
        private static UpdateBatcher? s_updateBatcher;
        private static ApiRequestQueue? s_apiRequestQueue;
        private static IDisposable? s_esiKeyRefreshTickSubscription;

        #endregion


        #region Initialization and threading

        /// <summary>
        /// Initializes paths, static objects, check and load datafiles, etc.
        /// </summary>
        /// <remarks>May be called more than once without causing redundant operations to occur.</remarks>
        public static void Initialize()
        {
            if (s_initialized)
                return;

            s_initialized = true;

            Trace("begin");

            // Network monitoring (connection availability changes)
            NetworkMonitor.Initialize();

            // ESIMethods collection initialization (always before members instatiation)
            ESIMethods.Initialize();

            // Members instantiations
            APIProviders = new GlobalAPIProviderCollection();
            MonitoredCharacters = new GlobalMonitoredCharacterCollection();
            CharacterIdentities = new GlobalCharacterIdentityCollection();
            Notifications = new GlobalNotificationCollection();
            Characters = new GlobalCharacterCollection();
            Datafiles = new GlobalDatafileCollection();
            ESIKeys = new GlobalAPIKeyCollection();
            EVEServer = new EveServer();

            // Initialize the update batcher for coalescing character updates
            s_updateBatcher = new UpdateBatcher(coalesceMs: 100);
            s_updateBatcher.CharactersBatchUpdated += OnBatchedCharacterUpdatesReady;
            s_updateBatcher.SkillQueuesBatchUpdated += OnBatchedSkillQueueUpdatesReady;

            // Subscribe to CharacterUpdatedEvent so that models publishing directly
            // to the EventAggregator (bypassing OnCharacterUpdated) still feed the batcher.
            AppServices.EventAggregator?.Subscribe<CommonEvents.CharacterUpdatedEvent>(e =>
            {
                s_updateBatcher?.QueueCharacterUpdate(e.Character);
            });

            // Initialize the account status subscriber (reacts to ESIKey and SkillQueue events)
            AccountStatusSubscriber.Initialize();

            // ESI key token refresh runs on FiveSecondTickEvent (independent of data fetch scheduler)
            s_esiKeyRefreshTickSubscription = AppServices.EventAggregator?.Subscribe<Core.Events.FiveSecondTickEvent>(
                e => OnEsiKeyRefreshTick());

            // Touch EsiScheduler to start the dispatch loop (lazy initialization)
            _ = AppServices.EsiScheduler;
            Trace("EsiScheduler initialized");

            // Initialize the API request queue for rate limiting
            // ESI recommends no more than 20 concurrent connections, with 50ms spacing
            s_apiRequestQueue = new ApiRequestQueue(maxConcurrent: 20, minDelayMs: 50);

            Trace("done");
        }

        /// <summary>
        /// Starts the event processing on a multi-threaded model, with the UI actor being the main actor.
        /// </summary>
        /// <param name="thread">The thread.</param>
        public static void Run(Thread thread)
        {
            Dispatcher.Run(thread);
            Trace();
        }

        /// <summary>
        /// Shutdowns the timer.
        /// </summary>
        public static void Shutdown()
        {
            Closed = true;

            // Dispose the update batcher (flushes any pending updates)
            s_updateBatcher?.Dispose();
            s_updateBatcher = null;

            // Dispose ESI key refresh subscription
            s_esiKeyRefreshTickSubscription?.Dispose();
            s_esiKeyRefreshTickSubscription = null;

            // Shutdown settings services
            Settings.Shutdown();

            // Dispose the API request queue
            s_apiRequestQueue?.Dispose();
            s_apiRequestQueue = null;

            Dispatcher.Shutdown();
            Trace();
        }

        /// <summary>
        /// Gets the update batcher for coalescing character updates.
        /// </summary>
        public static UpdateBatcher? UpdateBatcher => s_updateBatcher;

        /// <summary>
        /// Gets the API request queue for rate limiting ESI requests.
        /// </summary>
        public static ApiRequestQueue? ApiRequestQueue => s_apiRequestQueue;

        /// <summary>
        /// Drives ESI key token refresh. SmartQueryScheduler doesn't
        /// know about ESI keys, so we need a separate handler.
        /// </summary>
        private static void OnEsiKeyRefreshTick()
        {
            foreach (var key in ESIKeys)
                key.ProcessTick();
        }

        /// <summary>
        /// Resets collection that need to be cleared.
        /// </summary>
        internal static void ResetCollections()
        {
            ESIKeys = new GlobalAPIKeyCollection();
            Characters = new GlobalCharacterCollection();
            Notifications = new GlobalNotificationCollection();
            CharacterIdentities = new GlobalCharacterIdentityCollection();
            MonitoredCharacters = new GlobalMonitoredCharacterCollection();
        }

        /// <summary>
        /// Gets true whether the client has been shut down.
        /// </summary>
        public static bool Closed { get; set; }

        /// <summary>
        /// Gets true when static datafiles, ID caches, and character data have been loaded.
        /// Set by Program.cs during splash screen phase to indicate MainWindow can skip InitializeData.
        /// </summary>
        public static bool IsDataLoaded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is debug build.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is debug build; otherwise, <c>false</c>.
        /// </value>
        public static bool IsDebugBuild { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is snapshot build.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is snapshot build; otherwise, <c>false</c>.
        /// </value>
        public static bool IsSnapshotBuild { get; private set; }

        #endregion


        #region Version

        /// <summary>
        /// Gets the file version information.
        /// </summary>
        /// <value>
        /// The file version.
        /// </value>
        public static FileVersionInfo FileVersionInfo
            => FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);

        /// <summary>
        /// Gets the full version string from AssemblyInformationalVersion (e.g., "5.2.0-alpha.1").
        /// </summary>
        public static string VersionString
            => FileVersionInfo.ProductVersion ?? FileVersionInfo.FileVersion ?? "0.0.0";

        /// <summary>
        /// Gets whether this is an alpha version.
        /// </summary>
        public static bool IsAlphaVersion
            => VersionString?.Contains("alpha", StringComparison.OrdinalIgnoreCase) ?? false;

        /// <summary>
        /// Gets whether this is a beta version.
        /// </summary>
        public static bool IsBetaVersion
            => VersionString?.Contains("beta", StringComparison.OrdinalIgnoreCase) ?? false;

        /// <summary>
        /// Gets whether this is a pre-release version (alpha or beta).
        /// </summary>
        public static bool IsPreReleaseVersion
            => IsAlphaVersion || IsBetaVersion;

        /// <summary>
        /// Gets the product name with version type prefix for window titles.
        /// For alpha: "EVEMon ALPHA (5.2.0-alpha.1)"
        /// For beta: "EVEMon BETA (5.2.0-beta.1)"
        /// For stable: "EVEMon"
        /// </summary>
        public static string ProductNameWithVersion
        {
            get
            {
                string productName = FileVersionInfo.ProductName ?? "EVEMon";
                if (IsAlphaVersion)
                    return $"{productName} ALPHA ({VersionString})";
                if (IsBetaVersion)
                    return $"{productName} BETA ({VersionString})";
                return productName;
            }
        }

        #endregion


        #region File paths

        /// <summary>
        /// Gets the EVE Online installations default portrait cache folder.
        /// </summary>
        public static IEnumerable<string> DefaultEvePortraitCacheFolders
        {
            get
            {
                if (s_defaultEvePortraitCacheFolders != null && s_defaultEvePortraitCacheFolders.Any())
                    return s_defaultEvePortraitCacheFolders;

                s_defaultEvePortraitCacheFolders = Settings.PortableEveInstallations.EVEClients
                    .Select(eveClientInstallation => $"{eveClientInstallation.Path}\\cache\\Pictures\\Characters")
                    .Where(Directory.Exists).ToList();

                if (s_defaultEvePortraitCacheFolders.Any())
                    EvePortraitCacheFolders = s_defaultEvePortraitCacheFolders;

                return s_defaultEvePortraitCacheFolders;
            }
        }

        /// <summary>
        /// Gets or sets the portrait cache folder defined by the user.
        /// </summary>
        public static IEnumerable<string> EvePortraitCacheFolders { get; internal set; } = null!;

        /// <summary>
        /// Gets or sets the EVE Online application data folder.
        /// </summary>
        public static string EVEApplicationDataDir { get; private set; } = null!;

        /// <summary>
        /// Returns the state of the EVE database.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if EVE database is out of service; otherwise, <c>false</c>.
        /// </value>
        public static bool EVEDatabaseDisabled { get; internal set; }

        /// <summary>
        /// Returns the current data storage directory.
        /// </summary>
        public static string EVEMonDataDir { get; private set; } = null!;

        /// <summary>
        /// Returns the current cache directory.
        /// </summary>
        public static string EVEMonCacheDir { get; private set; } = null!;

        /// <summary>
        /// Returns the current xml cache directory.
        /// </summary>
        public static string EVEMonXmlCacheDir { get; private set; } = null!;

        /// <summary>
        /// Returns the current image cache directory (not portraits).
        /// </summary>
        public static string EVEMonImageCacheDir { get; private set; } = null!;

        /// <summary>
        /// Returns the current portraits cache directory.
        /// </summary>
        /// <remarks>
        /// We're talking about the cache in %APPDATA%\cache\portraits
        /// This is different from the ImageService's hit cache (%APPDATA%\cache\image)
        /// or the game's portrait cache (in EVE Online folder)
        ///</remarks>
        public static string EVEMonPortraitCacheDir { get; private set; } = null!;

        /// <summary>
        /// Gets the name of the current settings file.
        /// </summary>
        public static string SettingsFileName { get; private set; } = null!;

        /// <summary>
        /// Gets a value indicating whether cache folder in EVE default location exist.
        /// </summary>
        public static bool EveAppDataFoldersExistInDefaultLocation { get; private set; }

        /// <summary>
        /// Gets the fully qualified path to the current settings file.
        /// </summary>
        public static string SettingsFileNameFullPath => Path.Combine(EVEMonDataDir, SettingsFileName);

        /// <summary>
        /// Gets the fully qualified path to the trace file.
        /// </summary>
        public static string TraceFileNameFullPath => Path.Combine(EVEMonDataDir, s_traceFile);

        /// <summary>
        /// Creates the file system paths (settings file name, appdata directory, etc).
        /// </summary>
        public static void InitializeFileSystemPaths()
        {
            // Ensure it is made once only
            if (!string.IsNullOrEmpty(SettingsFileName))
                return;

            string debugAddition = IsDebugBuild ? "-debug" : string.Empty;
            SettingsFileName = $"settings{debugAddition}.xml";
            s_traceFile = $"trace{debugAddition}.txt";

            while (true)
            {
                try
                {
                    InitializeEVEMonPaths();
                    InitializeDefaultEvePortraitCachePath();
                    return;
                }
                catch (UnauthorizedAccessException exc)
                {
                    string msg = "An error occurred while EVEMon was looking for its data directory. " +
                                 "You may have insufficient rights or a synchronization may be taking place.\n\n" +
                                 $"The message was :{Environment.NewLine}{exc.Message}";

                    var result = AppServices.DialogService.ShowMessage(msg, @"EVEMon Error",
                        Core.Enumerations.DialogButtons.RetryCancel,
                        Core.Enumerations.DialogIcon.Error);

                    if (result != Core.Enumerations.DialogChoice.Cancel)
                        continue;

                    AppServices.ApplicationLifecycle.Exit();
                    return;
                }
            }
        }

        /// <summary>
        /// Initializes all needed EVEMon paths.
        /// </summary>
        private static void InitializeEVEMonPaths()
        {
            // Assign or create the EVEMon data directory
            if (!Directory.Exists(EVEMonDataDir))
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EVEMon");

                // If settings.xml exists in the app's directory, we use this one
                EVEMonDataDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Else, we use %APPDATA%\EVEMon
                if (!File.Exists(SettingsFileNameFullPath))
                    EVEMonDataDir = appDataPath;

                // Create the directory if it does not exist already
                if (!Directory.Exists(EVEMonDataDir))
                    Directory.CreateDirectory(EVEMonDataDir);
            }

            // Create the cache subfolder
            EVEMonCacheDir = Path.Combine(EVEMonDataDir, "cache");
            if (!Directory.Exists(EVEMonCacheDir))
                Directory.CreateDirectory(EVEMonCacheDir);

            // Create the xml cache subfolder
            EVEMonXmlCacheDir = Path.Combine(EVEMonCacheDir, "xml");
            if (!Directory.Exists(EVEMonXmlCacheDir))
                Directory.CreateDirectory(EVEMonXmlCacheDir);

            // Create the images cache subfolder (not portraits)
            EVEMonImageCacheDir = Path.Combine(EVEMonCacheDir, "images");
            if (!Directory.Exists(EVEMonImageCacheDir))
                Directory.CreateDirectory(EVEMonImageCacheDir);

            // Create the portraits cache subfolder
            EVEMonPortraitCacheDir = Path.Combine(EVEMonCacheDir, "portraits");
            if (!Directory.Exists(EVEMonPortraitCacheDir))
                Directory.CreateDirectory(EVEMonPortraitCacheDir);
        }

        /// <summary>
        /// Retrieves the portrait cache folder, from the game installation.
        /// </summary>
        private static void InitializeDefaultEvePortraitCachePath()
        {
            string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            EVEApplicationDataDir = $"{localApplicationData}\\CCP\\EVE";

            // Check folder exists
            if (!Directory.Exists(EVEApplicationDataDir))
                return;

            // Create a pattern that matches anything "*_tranquility"
            // Enumerate files in the EVE cache directory
            DirectoryInfo di = new DirectoryInfo(EVEApplicationDataDir);
            DirectoryInfo[] tranquilityFolders = di.GetDirectories("*_tranquility");

            EveAppDataFoldersExistInDefaultLocation = tranquilityFolders.Any();

            if (!tranquilityFolders.Any())
                return;

            s_defaultEvePortraitCacheFolders = tranquilityFolders
                .Select(traquilityFolder => $"{EVEApplicationDataDir}\\{traquilityFolder.Name}\\cache\\Pictures\\Characters")
                .Where(Directory.Exists);

            EvePortraitCacheFolders = s_defaultEvePortraitCacheFolders;
        }

        /// <summary>
        /// Ensures the cache directories are initialized.
        /// </summary>
        internal static void EnsureCacheDirInit()
        {
            InitializeEVEMonPaths();
        }

        #endregion


        #region Services

        /// <summary>
        /// Gets the current synchronization context.
        /// </summary>
        /// <value>
        /// The current synchronization context.
        /// </value>
        public static TaskScheduler CurrentSynchronizationContext => TaskScheduler.FromCurrentSynchronizationContext();

        /// <summary>
        /// Gets an enumeration over the datafiles checksums.
        /// </summary>
        public static GlobalDatafileCollection Datafiles { get; private set; } = null!;

        /// <summary>
        /// Gets the API providers collection.
        /// </summary>
        public static GlobalAPIProviderCollection APIProviders { get; private set; } = null!;

        /// <summary>
        /// Gets the EVE server's informations.
        /// </summary>
        public static EveServer EVEServer { get; private set; } = null!;

        /// <summary>
        /// Apply some settings changes.
        /// </summary>
        private static void UpdateSettings()
        {
            HttpWebClientServiceState.Proxy = Settings.Proxy;

            // Wire up proxy host resolver for HttpWebClientServiceException
            // so it can resolve proxy hosts without depending on HttpWebClientServiceState directly
            HttpWebClientServiceException.ProxyHostResolver = url =>
            {
                if (HttpWebClientServiceState.Proxy.Enabled)
                    return HttpWebClientServiceState.Proxy.Host;

                var proxyUri = System.Net.WebRequest.DefaultWebProxy?.GetProxy(url);
                return proxyUri?.Host ?? url.Host;
            };
        }

        #endregion


        #region Cache Clearing

        public static void ClearCache()
        {
            try
            {
                List<FileInfo> cachedFiles = new List<FileInfo>();
                cachedFiles.AddRange(new DirectoryInfo(EVEMonImageCacheDir).GetFiles());
                cachedFiles.AddRange(new DirectoryInfo(EVEMonXmlCacheDir).GetFiles());
                cachedFiles.AddRange(new DirectoryInfo(EVEMonPortraitCacheDir).GetFiles());

                cachedFiles.ForEach(x => x.Delete());
            }
            finally
            {
                InitializeEVEMonPaths();
            }
        }

        #endregion


        #region API Keys management

        /// <summary>
        /// Gets the collection of all known API keys.
        /// </summary>
        public static GlobalAPIKeyCollection ESIKeys { get; private set; } = null!;

        /// <summary>
        /// Gets the collection of all characters.
        /// </summary>
        public static GlobalCharacterCollection Characters { get; private set; } = null!;

        /// <summary>
        /// Gets the collection of all known character identities. For monitored character, see <see cref="MonitoredCharacters"/>.
        /// </summary>
        public static GlobalCharacterIdentityCollection CharacterIdentities { get; private set; } = null!;

        /// <summary>
        /// Gets the collection of all monitored characters.
        /// </summary>
        public static GlobalMonitoredCharacterCollection MonitoredCharacters { get; private set; } = null!;

        /// <summary>
        /// Gets the collection of notifications.
        /// </summary>
        public static GlobalNotificationCollection Notifications { get; private set; } = null!;

        #endregion


        #region Diagnostics

        /// <summary>
        /// Sends a formatted trace message. Delegates to <see cref="AppServices.TraceService"/>.
        /// </summary>
        public static void Trace(string format, params object[] args)
        {
            AppServices.TraceService?.Trace(format, args);
        }

        /// <summary>
        /// Sends a trace message. Delegates to <see cref="AppServices.TraceService"/>.
        /// </summary>
        public static void Trace(string message = null, bool printMethod = true)
        {
            AppServices.TraceService?.Trace(message ?? string.Empty, printMethod);
        }

        /// <summary>
        /// Sends a message to the trace with the calling method, time
        /// and the types of any arguments passed to the method.
        /// </summary>
        public static void TraceMethod()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame frame = stackTrace.GetFrame(1);
            MethodBase method = frame.GetMethod();
            string parameters = FormatParameters(method.GetParameters());
            string declaringType = method.DeclaringType?.ToString().Replace("EVEMon.", string.Empty);

            Trace($"{declaringType}.{method.Name}({parameters})");
        }

        /// <summary>
        /// Formats the parameters of a method into a string.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A comma seperated string of paramater types and names.</returns>
        private static string FormatParameters(IEnumerable<ParameterInfo> parameters)
        {
            StringBuilder paramDetail = new StringBuilder();

            foreach (ParameterInfo param in parameters)
            {
                if (paramDetail.Length != 0)
                    paramDetail.Append(", ");

                paramDetail.Append($"{param.GetType().Name} {param.Name}");
            }

            return paramDetail.ToString();
        }

        /// <summary>
        /// Starts the logging of trace messages to a file.
        /// Delegates to <see cref="AppServices.TraceService"/>.
        /// Kept for backward compatibility with existing callers.
        /// </summary>
        public static void StartTraceLogging()
        {
            AppServices.TraceService?.StartLogging(TraceFileNameFullPath);
        }

        /// <summary>
        /// Stops the logging of trace messages to a file.
        /// Delegates to <see cref="AppServices.TraceService"/>.
        /// Kept for backward compatibility with existing callers.
        /// </summary>
        public static void StopTraceLogging()
        {
            AppServices.TraceService?.StopLogging();
        }

        /// <summary>
        /// Will only execute if DEBUG is set, thus lets us avoid #IFDEF.
        /// </summary>
        [Conditional("DEBUG")]
        public static void CheckIsDebug()
        {
            IsDebugBuild = true;
        }

        /// <summary>
        /// Will only execute if SHAPSHOT is set, thus lets us avoid #IFDEF.
        /// </summary>
        [Conditional("SNAPSHOT")]
        public static void CheckIsSnapshot()
        {
            IsSnapshotBuild = true;
        }

        #endregion
    }
}
