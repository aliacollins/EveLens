// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EveLens.Common.Constants;
using EveLens.Common.Net;
using EveLens.Common.Services;

namespace EveLens.Common.Service
{
    /// <summary>
    /// Submits error/diagnostic reports to the webhook, which creates GitHub issues.
    /// The report body is gzip-compressed and base64-encoded to stay within webhook
    /// payload limits. The webhook server decompresses and creates a GitHub Gist,
    /// then links the Gist in the created issue.
    /// </summary>
    public static class ReportSubmissionService
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Submits a report to the webhook endpoint.
        /// The report body is gzip-compressed to avoid 413 errors.
        /// Never throws — returns a result for all outcomes.
        /// </summary>
        public static async Task<ReportSubmissionResult> SubmitReportAsync(
            string title, string reportType, string reportBody, string? crashSummary = null)
        {
            try
            {
                string webhookUrl = NetworkConstants.ReportWebhookUrl;
                if (string.IsNullOrWhiteSpace(webhookUrl))
                    return new ReportSubmissionResult(false, null, "Webhook URL is not configured.");

                string version;
                try
                {
                    version = AppServices.FileVersionInfo?.FileVersion ?? "(unknown)";
                }
                catch
                {
                    version = "(unknown)";
                }

                string os = Environment.OSVersion.VersionString;

                // Gzip-compress and base64-encode the report body to minimize payload size.
                // The webhook server decompresses this and creates a GitHub Gist.
                string compressedBody = CompressToBase64(reportBody ?? string.Empty);

                var payload = new ReportPayload
                {
                    Title = title,
                    ReportType = reportType,
                    Version = version,
                    Os = os,
                    CrashSummary = crashSummary,
                    ReportBodyGzip = compressedBody
                };

                string json = JsonSerializer.Serialize(payload, s_jsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpClient client = HttpWebClientService.GetHttpClient();
                using HttpResponseMessage response = await client.PostAsync(webhookUrl, content)
                    .ConfigureAwait(false);

                string responseBody = await response.Content.ReadAsStringAsync()
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string serverError = TryExtractError(responseBody)
                        ?? $"Server returned {(int)response.StatusCode}";
                    return new ReportSubmissionResult(false, null, serverError);
                }

                string? issueUrl = TryExtractIssueUrl(responseBody);
                if (issueUrl == null)
                    return new ReportSubmissionResult(false, null, "Could not parse server response.");

                return new ReportSubmissionResult(true, issueUrl, null);
            }
            catch (Exception ex) when (ex is HttpRequestException
                                    || ex is TaskCanceledException
                                    || ex is JsonException
                                    || ex is InvalidOperationException)
            {
                return new ReportSubmissionResult(false, null, ex.Message);
            }
        }

        /// <summary>
        /// Gzip-compresses a string and returns the base64-encoded result.
        /// </summary>
        private static string CompressToBase64(string text)
        {
            byte[] raw = Encoding.UTF8.GetBytes(text);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(raw, 0, raw.Length);
            }
            return Convert.ToBase64String(output.ToArray());
        }

        private static string? TryExtractIssueUrl(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("issueUrl", out JsonElement el))
                    return el.GetString();
            }
            catch { }
            return null;
        }

        private static string? TryExtractError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out JsonElement el))
                    return el.GetString();
            }
            catch { }
            return null;
        }

        private class ReportPayload
        {
            public string Title { get; set; } = string.Empty;
            public string ReportType { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string? Os { get; set; }
            public string? CrashSummary { get; set; }
            public string? ReportBodyGzip { get; set; }
        }
    }

    /// <summary>
    /// Result of a report submission attempt.
    /// </summary>
    public sealed class ReportSubmissionResult
    {
        public bool Success { get; }
        public string? IssueUrl { get; }
        public string? ErrorMessage { get; }

        public ReportSubmissionResult(bool success, string? issueUrl, string? errorMessage)
        {
            Success = success;
            IssueUrl = issueUrl;
            ErrorMessage = errorMessage;
        }
    }
}
