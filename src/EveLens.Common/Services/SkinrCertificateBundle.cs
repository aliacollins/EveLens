// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Produces the PEM certificate bundle the render engine needs before it can fetch a single
    /// byte of game content from CCP's CDN.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this class has to exist at all.</b> Trinity's Blue layer builds every libcurl
    /// connection through one factory, and that factory does this, unconditionally and without
    /// checking the result:</para>
    ///
    /// <code>
    /// std::wstring cert_str = BePaths->ResolvePathW( L"bin://cacert.pem" );
    /// curl_easy_setopt( connection, CURLOPT_CAINFO, cert_path );
    /// </code>
    ///
    /// <para>Two consequences follow, and both are load-bearing. First, <c>CURLOPT_CAINFO</c>
    /// <em>overrides</em> the platform certificate store, so on a machine whose Windows trust
    /// store is perfectly healthy the engine will still refuse every HTTPS request unless that
    /// exact file exists. Second, the failure is reported as
    /// <c>CURLE_SSL_CACERT_BADFILE</c> deep inside Blue and surfaces to Python as the sentence
    /// <c>RuntimeError: Couldn't download file</c> — which sends you looking at the network, the
    /// CDN, the URL, and the resource index, in that order, for as long as your patience lasts.
    /// The retail EVE client ships <c>cacert.pem</c> beside its executable, so CCP never hits
    /// this. A from-source engine build has no such file, which is why every render we produced
    /// for weeks worked only because earlier experiments had already filled the resource cache.
    /// </para>
    ///
    /// <para><b>Why we generate the bundle instead of shipping one.</b> Embedding Mozilla's
    /// <c>cacert.pem</c> would work today and rot quietly: a bundled trust list is a snapshot,
    /// roots get added and revoked, and the failure mode of a stale one is an unfetchable CDN
    /// months after release with no clue pointing at the certificate file. Exporting the machine's
    /// own root store instead means the trust list is whatever Windows Update has decided it is —
    /// current by construction, and, importantly, <em>including</em> any private root a corporate
    /// TLS-inspecting proxy has installed. Users behind such a proxy are exactly the users a
    /// pinned Mozilla bundle would break, and they would have no way to diagnose it.</para>
    ///
    /// <para><b>Why the file must be named <c>cacert.pem</c>.</b> The name is Blue's, not ours,
    /// and it is compiled in. We control only which directory <c>bin://</c> resolves to — and
    /// conveniently, that scheme has precisely one consumer in the whole engine (the line quoted
    /// above), so pointing it at a directory of our own costs nothing. That is what lets the
    /// bundle live in EveLens's writable data tree rather than inside the engine install, which
    /// in turn is what makes this work when the engine sits in Program Files.</para>
    /// </remarks>
    public static class SkinrCertificateBundle
    {
        /// <summary>
        /// The file name Blue compiles into its curl setup. Not configurable — see the class
        /// remarks.
        /// </summary>
        public const string FileName = "cacert.pem";

        /// <summary>
        /// How long a generated bundle is trusted before it is rebuilt from the live store.
        /// </summary>
        /// <remarks>
        /// Root store changes arrive with Windows Update, so this is not urgent work; it is
        /// insurance against a long-lived install drifting from the OS. Rebuilding costs a few
        /// milliseconds and a couple of hundred kilobytes of write, so the interval is chosen for
        /// "obviously often enough" rather than tuned.
        /// </remarks>
        public static readonly TimeSpan MaxAge = TimeSpan.FromDays(14);

        /// <summary>
        /// A plausible floor for a real bundle, used to reject a truncated or empty write.
        /// </summary>
        /// <remarks>
        /// A Windows root store holds dozens of certificates and a PEM encoding of one is on the
        /// order of a kilobyte, so a healthy bundle is tens of kilobytes. Anything under this is
        /// a partially written file or a store we failed to read, and handing either to curl
        /// reproduces the original bug with extra steps.
        /// </remarks>
        private const int MinimumPlausibleBytes = 8 * 1024;

        /// <summary>
        /// Returns the directory to hand to the sidecar as its <c>bin</c> search root, having
        /// made sure a usable <c>cacert.pem</c> is in it.
        /// </summary>
        /// <param name="dataDirectory">EveLens's writable data directory.</param>
        /// <returns>
        /// The containing directory, or null when no bundle could be produced. Null is a normal
        /// answer — it means "the renderer cannot fetch resources", which
        /// <see cref="SkinrSidecarOptions.Validate"/> turns into a sentence rather than a crash.
        /// </returns>
        public static string? Ensure(string dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(dataDirectory))
                return null;

            string directory = Path.Combine(dataDirectory, "cache", "skinr", "certs");
            string path = Path.Combine(directory, FileName);

            try
            {
                if (IsFresh(path))
                    return directory;

                Directory.CreateDirectory(directory);

                string pem = BuildPem(out int certificates);
                if (certificates == 0 || pem.Length < MinimumPlausibleBytes)
                {
                    AppServices.TraceService?.Trace(
                        "Skinr: the system root certificate store yielded " +
                        $"{certificates} usable certificates, which is too few to trust. " +
                        "The renderer will not be able to fetch game resources.");

                    // An existing bundle, even a stale one, beats no bundle: roots do not usually
                    // vanish, and a fetch that might work is better than one that cannot.
                    return File.Exists(path) ? directory : null;
                }

                // Written via a temporary and moved into place. Blue reads this file on its first
                // connection, and a half-written PEM is indistinguishable from a missing one at
                // exactly the moment we are trying to stop being indistinguishable.
                string temp = path + ".tmp";
                File.WriteAllText(temp, pem, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                File.Move(temp, path, overwrite: true);

                AppServices.TraceService?.Trace(string.Format(CultureInfo.InvariantCulture,
                    "Skinr: wrote CA bundle with {0} certificates ({1:n0} bytes) to {2}",
                    certificates, pem.Length, path));

                return directory;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                          or System.Security.SecurityException)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: could not prepare the CA bundle: {ex.Message}");
                return File.Exists(path) ? directory : null;
            }
        }

        /// <summary>
        /// True when the bundle on disk is present, large enough to be real, and recent enough
        /// to still reflect the system store.
        /// </summary>
        private static bool IsFresh(string path)
        {
            var info = new FileInfo(path);
            return info.Exists
                   && info.Length >= MinimumPlausibleBytes
                   && DateTime.UtcNow - info.LastWriteTimeUtc < MaxAge;
        }

        /// <summary>
        /// Concatenates the machine and current-user root stores into one PEM document.
        /// </summary>
        /// <remarks>
        /// <para>Both stores, because they answer different questions: <c>LocalMachine</c> is the
        /// OS trust list that Windows Update maintains, and <c>CurrentUser</c> is where a
        /// per-user install of a corporate inspection root tends to land. Reading only the first
        /// is the common mistake and it breaks precisely the users who cannot diagnose it.</para>
        ///
        /// <para>Each certificate is annotated with its subject before its PEM block. curl
        /// ignores everything outside the <c>BEGIN</c>/<c>END</c> markers, and it means a support
        /// question about this file can be answered by reading it.</para>
        /// </remarks>
        private static string BuildPem(out int certificates)
        {
            var builder = new StringBuilder(256 * 1024);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            builder.Append("# EveLens generated CA bundle for the SKINR render engine.\n")
                   .Append("# Exported from this machine's Windows root certificate stores on ")
                   .Append(DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture))
                   .Append(".\n# Regenerated automatically; edits will be overwritten.\n\n");

            certificates = 0;
            foreach (StoreLocation location in
                     new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
            {
                foreach (X509Certificate2 certificate in Read(location))
                {
                    using (certificate)
                    {
                        // Thumbprint, not subject: the two stores overlap heavily, and the same
                        // root under two names would be listed twice.
                        if (!seen.Add(certificate.Thumbprint))
                            continue;

                        builder.Append("# ").Append(Sanitize(certificate.Subject)).Append('\n');
                        builder.Append(certificate.ExportCertificatePem()).Append('\n');
                        certificates++;
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Reads one root store, treating an unreadable store as empty.
        /// </summary>
        /// <remarks>
        /// Opening <c>LocalMachine</c> read-only is unprivileged on Windows, but this runs on
        /// machines we do not administer: a locked-down policy, a redirected store, or a
        /// non-Windows host all throw here, and none of them is a reason to fail the render
        /// pipeline outright when the other store may still have what we need.
        /// </remarks>
        private static IEnumerable<X509Certificate2> Read(StoreLocation location)
        {
            var found = new List<X509Certificate2>();
            try
            {
                using var store = new X509Store(StoreName.Root, location);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (X509Certificate2 certificate in store.Certificates)
                    found.Add(certificate);
            }
            catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException
                                           or PlatformNotSupportedException
                                           or UnauthorizedAccessException)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: could not read the {location} root store: {ex.Message}");
            }
            return found;
        }

        /// <summary>
        /// Flattens a subject name to one comment-safe line.
        /// </summary>
        private static string Sanitize(string value)
        {
            string flat = (value ?? string.Empty)
                .Replace('\r', ' ').Replace('\n', ' ').Trim();
            return flat.Length <= 160 ? flat : flat[..160];
        }
    }
}
