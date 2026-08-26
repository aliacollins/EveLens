// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EveLens.Common.Services
{
    /// <summary>A newer release found on GitHub for this build's channel.</summary>
    public sealed class GitHubReleaseInfo
    {
        public string Version { get; init; } = "";
        public string Url { get; init; } = "";
        public string NotesMarkdown { get; init; } = "";
    }

    /// <summary>
    /// Update checking for the platforms Velopack cannot serve: the macOS
    /// <c>.app</c> and Linux archives are hand-packaged, so Velopack reports
    /// "not installed" there and its whole update path is inert. This checker
    /// asks the GitHub Releases API instead and answers one question — is there
    /// a newer release on MY channel? — leaving the actual download to the
    /// browser, since a zip cannot swap itself in place the way Velopack does.
    /// </summary>
    /// <remarks>
    /// Version comparison is numeric on (major, minor, patch, build), never
    /// lexicographic: string ordering says <c>beta.10 &lt; beta.4</c>, which is
    /// exactly the kind of bug that only detonates months after shipping.
    /// Channel comes from the informational version (<c>1.5.0-beta.4</c>); the
    /// numeric file version carries no channel and must not be used for this.
    /// </remarks>
    public static class GitHubReleaseChecker
    {
        private const string ReleasesApi =
            "https://api.github.com/repos/aliacollins/EveLens/releases";

        internal readonly record struct ParsedVersion(
            int Major, int Minor, int Patch, string Channel, int Build);

        /// <summary>Parses <c>1.5.0</c>, <c>1.5.0-beta.4</c>, or the same with a
        /// <c>+commit</c> suffix (SourceLink appends one to ProductVersion).</summary>
        internal static ParsedVersion? Parse(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return null;
            string v = version.Trim().TrimStart('v');
            int plus = v.IndexOf('+');
            if (plus >= 0)
                v = v[..plus];
            var m = System.Text.RegularExpressions.Regex.Match(
                v, @"^(\d+)\.(\d+)\.(\d+)(?:-(alpha|beta)\.(\d+))?$");
            if (!m.Success)
                return null;
            return new ParsedVersion(
                int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value),
                int.Parse(m.Groups[3].Value),
                m.Groups[4].Success ? m.Groups[4].Value : "stable",
                m.Groups[5].Success ? int.Parse(m.Groups[5].Value) : 0);
        }

        /// <summary>Whether <paramref name="candidate"/> is a newer release than
        /// <paramref name="current"/> for a user on the current build's channel.
        /// A stable release outranks any pre-release of the same numbers.</summary>
        internal static bool IsNewer(ParsedVersion current, ParsedVersion candidate)
        {
            int byNumbers = (candidate.Major, candidate.Minor, candidate.Patch)
                .CompareTo((current.Major, current.Minor, current.Patch));
            if (byNumbers != 0)
                return byNumbers > 0;
            // Same numbers: stable is the finished form of its pre-releases.
            bool curPre = current.Channel != "stable";
            bool candPre = candidate.Channel != "stable";
            if (curPre && !candPre)
                return true;
            if (!curPre)
                return false;
            if (current.Channel != candidate.Channel)
                return candidate.Channel == "beta";   // beta outranks alpha
            return candidate.Build > current.Build;
        }

        /// <summary>Whether a release belongs to what this channel's user should
        /// be offered: beta users take beta + stable, alpha takes everything,
        /// stable users take only stable.</summary>
        internal static bool ChannelAccepts(string channel, string releaseChannel) =>
            channel switch
            {
                "alpha" => true,
                "beta" => releaseChannel != "alpha",
                _ => releaseChannel == "stable",
            };

        /// <summary>
        /// Returns the newest release that outranks <paramref name="currentVersion"/>
        /// on its own channel, or null when up to date or unreachable — this is a
        /// polling path, so network trouble is a quiet "not now", never a dialog.
        /// </summary>
        public static async Task<GitHubReleaseInfo?> CheckAsync(
            string? currentVersion, CancellationToken ct = default)
        {
            ParsedVersion? cur = Parse(currentVersion);
            if (cur == null)
                return null;
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.Timeout = TimeSpan.FromSeconds(20);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("EveLens");
                string json = await http.GetStringAsync(ReleasesApi, ct)
                    .ConfigureAwait(false);

                GitHubReleaseInfo? best = null;
                ParsedVersion bestVer = cur.Value;
                using var doc = JsonDocument.Parse(json);
                foreach (JsonElement release in doc.RootElement.EnumerateArray())
                {
                    if (release.TryGetProperty("draft", out var d) && d.GetBoolean())
                        continue;
                    string tag = release.GetProperty("tag_name").GetString() ?? "";
                    ParsedVersion? rel = Parse(tag);
                    if (rel == null || !ChannelAccepts(cur.Value.Channel, rel.Value.Channel))
                        continue;
                    if (!IsNewer(bestVer, rel.Value))
                        continue;
                    bestVer = rel.Value;
                    best = new GitHubReleaseInfo
                    {
                        Version = tag.TrimStart('v'),
                        Url = release.TryGetProperty("html_url", out var u)
                            ? u.GetString() ?? "" : "",
                        NotesMarkdown = release.TryGetProperty("body", out var b)
                            ? b.GetString() ?? "" : "",
                    };
                }
                return best;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"GitHubReleaseChecker: check failed: {ex.Message}");
                return null;
            }
        }
    }
}
