using System;

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// The application's own identity and version, read from compiled-in assembly
    /// metadata rather than from a file on disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because <see cref="System.Diagnostics.FileVersionInfo"/> needs a
    /// path, and a single-file published app has none: <c>Assembly.Location</c> is an
    /// empty string, so <c>FileVersionInfo.GetVersionInfo(...)</c> throws before the
    /// first window can open. macOS ships single-file (Apple's bundle rules want only
    /// Mach-O binaries in <c>Contents/MacOS</c>), so anything on the startup path that
    /// asks the filesystem "what version am I?" is a crash waiting for a platform.
    /// Assembly attributes travel inside the binary and answer on every platform.
    /// </para>
    /// <para>
    /// Production: <c>AssemblyAppVersionInfo</c> over the entry assembly's attributes.
    /// Testing: substitute, or construct <c>AssemblyAppVersionInfo</c> over any assembly.
    /// </para>
    /// </remarks>
    public interface IAppVersionInfo
    {
        /// <summary>Product name, e.g. "EveLens". Never null or empty.</summary>
        string ProductName { get; }

        /// <summary>
        /// Display version including any pre-release suffix, e.g. "1.5.0-beta.13".
        /// From AssemblyInformationalVersion, with build metadata (+sha) stripped.
        /// Never null or empty.
        /// </summary>
        string ProductVersion { get; }

        /// <summary>Four-part numeric file version, e.g. "1.5.0.13". Never null or empty.</summary>
        string FileVersion { get; }

        /// <summary>Company/maintainer string. Never null or empty.</summary>
        string Company { get; }

        /// <summary>Copyright notice. May be empty if unset.</summary>
        string Copyright { get; }
    }
}
