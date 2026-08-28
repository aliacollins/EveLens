using System;

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Detects and repairs a macOS install whose app bundle cannot update itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because of Gatekeeper App Translocation: when a quarantined .app is
    /// launched without having been moved by Finder (Terminal <c>mv</c>, unzip-in-place,
    /// or running straight from Downloads), macOS executes a mirror of it from a
    /// randomized <b>read-only</b> mount under <c>/private/var/.../AppTranslocation/</c>.
    /// The in-place updater then targets the running bundle's path — the read-only
    /// mirror — and every apply fails with "Read-only file system (os error 30)".
    /// Worse, the failure happens in the spawned updater <i>after</i> the app has
    /// exited, so no in-process error handling can ever observe it: it must be
    /// prevented up front, not caught.
    /// </para>
    /// <para>
    /// Production: <c>MacInstallService</c> reading the current process path.
    /// Testing: construct <c>MacInstallService</c> with an explicit path, or substitute.
    /// On Windows/Linux all members report a healthy install.
    /// </para>
    /// </remarks>
    public interface IMacInstallService
    {
        /// <summary>
        /// Whether the app is running from a Gatekeeper App Translocation mount.
        /// While true, in-place updates can never install.
        /// </summary>
        bool IsTranslocated { get; }

        /// <summary>
        /// The real .app bundle the repair would relaunch from, or null when no
        /// candidate exists (nothing at /Applications and the running bundle path
        /// cannot be resolved).
        /// </summary>
        string? RealBundlePath { get; }

        /// <summary>
        /// Repairs the install: ensures a bundle exists at the real location, clears
        /// the quarantine attribute that causes translocation, and relaunches from
        /// there. Returns false if the repair could not be performed; on success the
        /// caller should exit — a fresh, un-translocated instance is starting.
        /// </summary>
        bool HealAndRelaunch();
    }
}
