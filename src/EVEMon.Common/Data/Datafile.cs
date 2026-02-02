using System;
using System.Collections.Generic;
using System.IO;
using EVEMon.Common.Helpers;

namespace EVEMon.Common.Data
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
        /// This ensures that when EVEMon is updated, new datafiles replace old cached ones.
        /// </remarks>
        internal static string GetFullPath(string filename)
        {
            string evemonDataDir = EveMonClient.EVEMonDataDir ??
                                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EVEMon");

            // Path in %APPDATA% folder
            string cachedFilePath = $"{evemonDataDir}{Path.DirectorySeparatorChar}{filename}";

            // Path in installation directory ("Resources" subdirectory)
            string installFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Resources{Path.DirectorySeparatorChar}{filename}";

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