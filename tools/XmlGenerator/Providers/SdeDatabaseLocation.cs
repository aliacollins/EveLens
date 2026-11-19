using System;
using System.Configuration;

namespace EveLens.XmlGenerator.Providers
{
    /// <summary>
    /// Resolves the SQLite connection string for the static data database.
    /// </summary>
    public static class SdeDatabaseLocation
    {
        /// <summary>The environment variable holding the path of the SQLite database file.</summary>
        public const string PathVariable = "EVELENS_SDE_SQLITE";

        private const string ConnectionName = "EveStaticData";

        /// <summary>
        /// The connection string: the file named by <see cref="PathVariable"/> when it is set,
        /// otherwise the "EveStaticData" entry of App.config; null when neither exists.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                string path = Environment.GetEnvironmentVariable(PathVariable);
                if (!string.IsNullOrWhiteSpace(path))
                    return "Data Source=" + path;

                return ConfigurationManager.ConnectionStrings[ConnectionName]?.ConnectionString;
            }
        }
    }
}
