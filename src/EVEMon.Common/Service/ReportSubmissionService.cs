using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EVEMon.Common.Constants;
using EVEMon.Common.Net;

namespace EVEMon.Common.Service
{
    /// <summary>
    /// Submits error/diagnostic reports to the webhook, which creates GitHub issues.
    /// </summary>
    public static class ReportSubmissionService
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Submits a report to the webhook endpoint.
        /// Never throws — returns a result for all outcomes.
        /// </summary>
        public static async Task<ReportSubmissionResult> SubmitReportAsync(
            string title, string reportType, string reportBody, string crashSummary = null)
        {
            try
            {
                string webhookUrl = NetworkConstants.ReportWebhookUrl;
                if (string.IsNullOrWhiteSpace(webhookUrl))
                    return new ReportSubmissionResult(false, null, "Webhook URL is not configured.");

                string version;
                try
                {
                    version = EveMonClient.FileVersionInfo?.FileVersion ?? "(unknown)";
                }
                catch
                {
                    version = "(unknown)";
                }

                string os = Environment.OSVersion.VersionString;

                var payload = new ReportPayload
                {
                    Title = title,
                    ReportType = reportType,
                    Version = version,
                    Os = os,
                    CrashSummary = crashSummary,
                    ReportBody = reportBody
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

                string issueUrl = TryExtractIssueUrl(responseBody);
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

        private static string TryExtractIssueUrl(string json)
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

        private static string TryExtractError(string json)
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
            public string Title { get; set; }
            public string ReportType { get; set; }
            public string Version { get; set; }
            public string Os { get; set; }
            public string CrashSummary { get; set; }
            public string ReportBody { get; set; }
        }
    }

    /// <summary>
    /// Result of a report submission attempt.
    /// </summary>
    public sealed class ReportSubmissionResult
    {
        public bool Success { get; }
        public string IssueUrl { get; }
        public string ErrorMessage { get; }

        public ReportSubmissionResult(bool success, string issueUrl, string errorMessage)
        {
            Success = success;
            IssueUrl = issueUrl;
            ErrorMessage = errorMessage;
        }
    }
}
