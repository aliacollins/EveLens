// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using EveLens.Common.Helpers;
using EveLens.Common.Services;

namespace EveLens.Common.Data
{


    #region Datafile class

    /// <summary>
    /// Represents a datafile
    /// </summary>
    public sealed class Datafile
    {
        private const string DatafileExtension = ".xml.gzip";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename"></param>
        public Datafile(string filename)
        {
            // The file may be in local directory, %APPDATA%, etc.
            Filename = filename;

            // Compute the MD5 sum
            MD5Sum = Util.CreateMD5From(GetFullPath(Filename));
        }

        /// <summary>
        /// Gets or sets the datafile name
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Gets or sets the MD5 sum
        /// </summary>
        public string MD5Sum { get; private set; }

        /// <summary>
        /// Gets the datafile extension.
        /// </summary>
        /// <value>
        /// The datafile extension.
        /// </value>
        public static string DatafilesExtension => DatafileExtension;

        /// <summary>
        /// Gets the old datafile extension.
        /// </summary>
        /// <value>
        /// The old datafile extension.
        /// </value>
        public static string OldDatafileExtension => DatafileExtension.TrimEnd("ip".ToCharArray());

        /// <summary>
        /// Gets the fully-qualified path of the provided datafile name
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <remarks>
        /// Attempts to find a datafile - checks both %APPDATA% and installation directory.
        /// If both exist, compares MD5 to ensure cached version is up to date.
        /// This ensures that when EveLens is updated, new datafiles replace old cached ones.
        /// </remarks>
        /// <summary>
        /// The bundled-resources directories, in preference order: the exe-adjacent
        /// "Resources" (Windows/Linux layout, and the dev tree), then the bundle-level
        /// "../Resources" — the macOS .app layout, where Contents/MacOS may contain
        /// only executable code so the bundle can be code-signed and notarized.
        /// </summary>
        public static IEnumerable<string> InstallResourceDirectories()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            yield return Path.Combine(baseDir, "Resources");
            string bundle = Path.GetFullPath(Path.Combine(baseDir, "..", "Resources"));
            if (!string.Equals(bundle, Path.Combine(baseDir, "Resources"),
                    StringComparison.OrdinalIgnoreCase))
                yield return bundle;
        }

        /// <summary>The first bundled-resources directory containing the file, or null.</summary>
        public static string? FindInstallResource(string fileName)
        {
            foreach (string dir in InstallResourceDirectories())
            {
                string path = Path.Combine(dir, fileName);
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        public static string GetDatafilesDirectory()
        {
            foreach (string dir in InstallResourceDirectories())
            {
                if (Directory.Exists(dir))
                    return dir;
            }

            return AppServices.ApplicationPaths.DataDirectory ??
                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveLens");
        }

        internal static string GetFullPath(string filename)
        {
            string evelensDataDir = AppServices.ApplicationPaths.DataDirectory ??
                                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveLens");

            // Path in %APPDATA% folder
            string cachedFilePath = $"{evelensDataDir}{Path.DirectorySeparatorChar}{filename}";

            // Path in installation directory ("Resources" subdirectory, either layout)
            string installFilePath = FindInstallResource(filename) ??
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", filename);

            bool cachedExists = File.Exists(cachedFilePath);
            bool installExists = File.Exists(installFilePath);

            // Neither exists - error
            if (!cachedExists && !installExists)
                throw new FileNotFoundException($"{installFilePath} not found!");

            // Only cached exists (shouldn't normally happen, but handle it)
            if (cachedExists && !installExists)
                return cachedFilePath;

            // Only installation exists - copy to cache and return
            if (!cachedExists && installExists)
            {
                FileHelper.CopyOrWarnTheUser(installFilePath, cachedFilePath);
                return installFilePath;
            }

            // Both exist - compare MD5 to ensure cache is up to date
            // This is the key fix: if installation file is different (newer), update the cache
            string cachedMD5 = Util.CreateMD5From(cachedFilePath);
            string installMD5 = Util.CreateMD5From(installFilePath);

            if (cachedMD5 != installMD5)
            {
                // Installation file is different (newer) - update the cache
                System.Diagnostics.Trace.WriteLine($"Datafile: Updating cached {filename} (MD5 mismatch)");
                FileHelper.CopyOrWarnTheUser(installFilePath, cachedFilePath);
            }

            return cachedFilePath;
        }

        /// <summary>
        /// Gets the data files from the given directory path.
        /// </summary>
        /// <param name="dirPath">The directory path.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetFilesFrom(string dirPath, string fileExtension)
            => Directory.GetFiles(dirPath, "*" + fileExtension, SearchOption.TopDirectoryOnly);
    }

    #endregion
}