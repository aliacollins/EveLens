// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Net;
using EveLens.Common.Serialization.Skinr;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Downloads, verifies and installs the EveLens Render Runtime — the separate,
    /// proprietary add-on that does the 3D rendering. EveLens (GPL) never bundles it;
    /// this class fetches it from evelens.dev on the user's explicit consent, and
    /// nothing runs out of it until every check below has passed.
    /// </summary>
    /// <remarks>
    /// <para><b>The verification chain, in order.</b> (1) The zip's SHA-256 must match
    /// the published release. (2) The manifest inside must carry a valid ECDSA P-256
    /// signature under the pinned key below — the private half lives only on the
    /// build machine, so no server compromise can mint an acceptable manifest.
    /// (3) The manifest's protocol generation must be one this build speaks.
    /// (4) Every extracted file must hash to what the signed manifest says. Only then
    /// does the tree move into place, atomically, under a version directory.</para>
    ///
    /// <para><b>Why a pinned key rather than Authenticode alone.</b> The runtime's
    /// binaries are Authenticode-signed too, but that protects Windows' opinion of
    /// them; this signature is EveLens's own accept/reject decision, it works
    /// offline, and it has no certificate-chain edge cases. Two locks, different
    /// doors.</para>
    /// </remarks>
    public static class SkinrRuntimeInstaller
    {
        /// <summary>Where releases are announced. The JSON is a
        /// <see cref="SkinrRuntimeRelease"/>.</summary>
        public const string LatestUrl = "https://hub.evelens.dev/runtime/latest.json";

        /// <summary>The sidecar protocol generation this build of EveLens speaks.</summary>
        public const int SupportedProtocolVersion = 1;

        /// <summary>
        /// The runtime-signing public key. The private half NEVER leaves the build
        /// machine — it is not in any repository, ours or this one.
        /// </summary>
        public const string PublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEGD9mQpGmBFlJ6apfGkD4Hfyg/BcA
mIqhIr1UnPgA18UMXK/zeOJXE02ogEoQP+5Nt9OJ1gsEGcZwqhZCsgeV5A==
-----END PUBLIC KEY-----";

        /// <summary>Root under EveLens's data directory holding installed runtimes,
        /// one subdirectory per version plus a <c>current.txt</c> pointer.</summary>
        public static string InstallRoot =>
            Path.Combine(AppServices.ApplicationPaths.DataDirectory, "skinr-runtime");

        private static string CurrentPointer => Path.Combine(InstallRoot, "current.txt");

        /// <summary>
        /// The installed runtime's root directory, or null when none is installed.
        /// This is what discovery consumes; it never guesses, only reads the pointer.
        /// </summary>
        public static string? InstalledRoot()
        {
            try
            {
                if (!File.Exists(CurrentPointer))
                    return null;
                string version = File.ReadAllText(CurrentPointer).Trim();
                if (version.Length == 0)
                    return null;
                string root = Path.Combine(InstallRoot, version);
                return File.Exists(Path.Combine(root, "manifest.json")) ? root : null;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrRuntime: reading install pointer failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>The installed version, for the settings/about surfaces.</summary>
        public static string? InstalledVersion()
        {
            string? root = InstalledRoot();
            return root == null ? null : Path.GetFileName(root);
        }

        /// <summary>Fetches the current release announcement, or null when the
        /// service is unreachable — a first-class "try again later" answer.</summary>
        public static async Task<SkinrRuntimeRelease?> GetLatestAsync(
            CancellationToken ct = default)
        {
            try
            {
                var result = await HttpWebClientService
                    .DownloadStreamAsync<SkinrRuntimeRelease>(new Uri(LatestUrl),
                        (stream, _) => JsonSerializer.Deserialize<SkinrRuntimeRelease>(stream),
                        null)
                    .ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                SkinrRuntimeRelease? release = result?.Result;
                if (release == null || string.IsNullOrEmpty(release.Version) ||
                    string.IsNullOrEmpty(release.Url) ||
                    string.IsNullOrEmpty(release.ZipSha256))
                    return null;
                return release;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrRuntime: release check failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Downloads, verifies and installs <paramref name="release"/>. Returns the
        /// installed root. Throws with a human sentence when any verification fails —
        /// a failed check deletes everything it downloaded.
        /// </summary>
        /// <param name="release">What to install, from <see cref="GetLatestAsync"/>.</param>
        /// <param name="progress">Download progress, 0.0–1.0.</param>
        /// <param name="ct">Cancels the download and cleans up.</param>
        public static async Task<string> InstallAsync(SkinrRuntimeRelease release,
            IProgress<double>? progress = null, CancellationToken ct = default)
        {
            if (release.ProtocolVersion > SupportedProtocolVersion)
            {
                throw new InvalidOperationException(
                    "This render runtime release requires a newer EveLens — " +
                    "update EveLens first, then install the runtime.");
            }

            Directory.CreateDirectory(InstallRoot);
            string zipPath = Path.Combine(InstallRoot,
                $"download-{Guid.NewGuid():N}.zip.part");
            string staging = Path.Combine(InstallRoot, $"staging-{Guid.NewGuid():N}");
            try
            {
                await DownloadZipAsync(release, zipPath, progress, ct).ConfigureAwait(false);

                string zipHash = await HashFileAsync(zipPath, ct).ConfigureAwait(false);
                if (!string.Equals(zipHash, release.ZipSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The downloaded runtime failed its integrity check " +
                        "(hash mismatch) and was discarded. Try again later.");
                }

                ZipFile.ExtractToDirectory(zipPath, staging);
                VerifyStagedTree(staging);

                string finalRoot = Path.Combine(InstallRoot, release.Version!);
                if (Directory.Exists(finalRoot))
                    Directory.Delete(finalRoot, recursive: true);
                Directory.Move(staging, finalRoot);
                File.WriteAllText(CurrentPointer, release.Version);
                AppServices.TraceService?.Trace(
                    $"SkinrRuntime: installed {release.Version} at {finalRoot}");
                return finalRoot;
            }
            finally
            {
                TryDelete(zipPath);
                if (Directory.Exists(staging))
                {
                    try { Directory.Delete(staging, recursive: true); }
                    catch (Exception) { /* best effort; staging is inert */ }
                }
            }
        }

        /// <summary>
        /// The signed-manifest half of the chain: signature under the pinned key,
        /// protocol generation, then every file's hash. Internal so a test can aim
        /// it at a synthetic tree without a 232 MB download.
        /// </summary>
        internal static void VerifyStagedTree(string root) =>
            VerifyStagedTree(root, PublicKeyPem);

        /// <summary>Key-injectable form, so tests can sign synthetic trees with a
        /// throwaway key instead of needing the real (offline) private half.</summary>
        internal static void VerifyStagedTree(string root, string publicKeyPem)
        {
            string manifestPath = Path.Combine(root, "manifest.json");
            string sigPath = manifestPath + ".sig";
            if (!File.Exists(manifestPath) || !File.Exists(sigPath))
                throw new InvalidOperationException(
                    "The runtime package is missing its signed manifest.");

            byte[] manifestBytes = File.ReadAllBytes(manifestPath);
            byte[] signature = Convert.FromBase64String(
                File.ReadAllText(sigPath).Trim());
            if (!VerifySignature(manifestBytes, signature, publicKeyPem))
                throw new InvalidOperationException(
                    "The runtime package's signature is invalid — it was not " +
                    "produced by EveLens's build and will not be run.");

            using JsonDocument doc = JsonDocument.Parse(manifestBytes);
            JsonElement rootEl = doc.RootElement;
            int protocol = rootEl.TryGetProperty("protocolVersion", out JsonElement p)
                ? p.GetInt32() : 0;
            if (protocol > SupportedProtocolVersion)
                throw new InvalidOperationException(
                    "This runtime speaks a newer protocol than this EveLens — " +
                    "update EveLens first.");

            if (!rootEl.TryGetProperty("files", out JsonElement files))
                throw new InvalidOperationException(
                    "The runtime manifest lists no files.");

            foreach (JsonProperty entry in files.EnumerateObject())
            {
                string rel = entry.Name;
                // The manifest signs itself by exclusion: its own entry (and the
                // signature's) cannot exist inside the signed content.
                if (rel is "manifest.json" or "manifest.json.sig")
                    continue;
                string full = Path.GetFullPath(Path.Combine(root, rel));
                // A hostile manifest must not be able to make us hash — or later
                // trust — anything outside the staged tree.
                if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"The runtime manifest names a path outside its tree ({rel}).");
                if (!File.Exists(full))
                    throw new InvalidOperationException(
                        $"The runtime package is incomplete ({rel} is missing).");
                using FileStream fs = File.OpenRead(full);
                byte[] hash = SHA256.HashData(fs);
                if (!string.Equals(Convert.ToHexString(hash), entry.Value.GetString(),
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"The runtime package failed verification ({rel} was altered).");
            }
        }

        /// <summary>ECDSA P-256 / SHA-256, IEEE P1363 signature format — the exact
        /// shape <c>sign.js</c> produces and .NET verifies by default.</summary>
        internal static bool VerifySignature(byte[] data, byte[] signature, string publicKeyPem)
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(publicKeyPem);
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrRuntime: signature check errored: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dedicated client, deliberately NOT <see cref="HttpWebClientService"/>: that
        /// stack applies the app-wide HttpTimeout (20s default, 5min ceiling) to the
        /// WHOLE request, which is correct for API calls and fatal for a 232 MB
        /// artifact on any connection slower than the ceiling allows. This one
        /// streams with headers-first completion and a generous cap of its own.
        /// </summary>
        private static async Task DownloadZipAsync(SkinrRuntimeRelease release,
            string zipPath, IProgress<double>? progress, CancellationToken ct)
        {
            long total = Math.Max(1, release.SizeBytes);
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromMinutes(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    HttpWebClientServiceState.UserAgent);
                using var response = await client.GetAsync(new Uri(release.Url!),
                    System.Net.Http.HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using Stream stream = await response.Content
                    .ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using FileStream file = File.Create(zipPath);
                byte[] buffer = new byte[1 << 16];
                long written = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    written += read;
                    progress?.Report(Math.Min(1.0, written / (double)total));
                }
                if (written == 0)
                    throw new InvalidOperationException("no data received");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "The runtime download failed: " + ex.Message, ex);
            }
        }

        private static async Task<string> HashFileAsync(string path, CancellationToken ct)
        {
            using FileStream fs = File.OpenRead(path);
            byte[] hash = await SHA256.HashDataAsync(fs, ct).ConfigureAwait(false);
            return Convert.ToHexString(hash);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception) { /* a stray .part is reaped next run */ }
        }
    }
}
