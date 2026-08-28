using System;
using System.Reflection;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Reads the app's identity from compiled-in assembly attributes. No filesystem
    /// access, so it works identically in framework-dependent, self-contained and
    /// single-file publishes.
    /// </summary>
    public sealed class AssemblyAppVersionInfo : IAppVersionInfo
    {
        private readonly Assembly m_assembly;

        /// <summary>
        /// Creates version info over the entry assembly, falling back to the assembly
        /// this type lives in. The fallback matters for test hosts and for any host
        /// that loads us without being an entry point.
        /// </summary>
        public AssemblyAppVersionInfo()
            : this(Assembly.GetEntryAssembly() ?? typeof(AssemblyAppVersionInfo).Assembly)
        {
        }

        /// <summary>Creates version info over a specific assembly.</summary>
        public AssemblyAppVersionInfo(Assembly assembly)
        {
            m_assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        /// <inheritdoc />
        public string ProductName =>
            Attr<AssemblyProductAttribute>(a => a.Product, "EveLens");

        /// <inheritdoc />
        public string ProductVersion => StripBuildMetadata(
            Attr<AssemblyInformationalVersionAttribute>(a => a.InformationalVersion, FileVersion));

        /// <inheritdoc />
        public string FileVersion =>
            Attr<AssemblyFileVersionAttribute>(a => a.Version,
                m_assembly.GetName().Version?.ToString() ?? "0.0.0.0");

        /// <inheritdoc />
        public string Company =>
            Attr<AssemblyCompanyAttribute>(a => a.Company, "EveLens");

        /// <inheritdoc />
        public string Copyright =>
            Attr<AssemblyCopyrightAttribute>(a => a.Copyright, string.Empty);

        private string Attr<T>(Func<T, string?> read, string fallback) where T : Attribute
        {
            string? value = null;
            try
            {
                T? attribute = m_assembly.GetCustomAttribute<T>();
                if (attribute != null)
                    value = read(attribute);
            }
            catch (Exception)
            {
                // A missing or malformed attribute must never be fatal — this type is
                // read during startup, before any error UI exists.
            }
            return string.IsNullOrWhiteSpace(value) ? fallback : value!;
        }

        /// <summary>
        /// Drops SemVer build metadata: the SDK appends "+&lt;commit sha&gt;" to
        /// InformationalVersion, which is noise in a title bar and breaks the
        /// substring checks the update flow makes against release tags.
        /// </summary>
        private static string StripBuildMetadata(string version)
        {
            int plus = version.IndexOf('+');
            return plus < 0 ? version : version.Substring(0, plus);
        }
    }
}
