// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using System;
using System.Net;
using System.Net.Http;

namespace EveLens.Common.Net
{
    /// <summary>
    /// Groups together response parameters from HTTP requests. Included are the status code,
    /// ETag (if present), expiration date (if present), and encoding.
    /// </summary>
    public sealed class ResponseParams
    {
        /// <summary>
        /// The error count reported from ESI to avoid running into backoff. This represents
        /// the number of errors remaining in the current period (X-Esi-Error-Limit-Remain).
        /// </summary>
        public int? ErrorCount { get; set; }

        /// <summary>
        /// The UTC time when the ESI error count will reset, calculated from
        /// X-Esi-Error-Limit-Reset header.
        /// </summary>
        public DateTime? ErrorResetTime { get; set; }

        /// <summary>
        /// The E-Tag received from the server. Null if no e-tag was sent.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// The time when this data expires. Null if no expiry was sent.
        /// </summary>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Reports true if the response indicates that data was not modified. Reports false
        /// for all other status codes.
        /// </summary>
        public bool IsNotModifiedResponse
        {
            get
            {
                return ResponseCode == (int)HttpStatusCode.NotModified;
            }
        }

        /// <summary>
        /// Reports true if the response indicates that it was successful (HTTP response code).
        /// Reports false for all other status codes.
        /// </summary>
        public bool IsOKResponse
        {
            get
            {
                return ResponseCode == (int)HttpStatusCode.OK;
            }
        }

        /// <summary>
        /// The maximum number of pages required. Will be 0 if no paging is required, 1 if
        /// there is only one page.
        /// </summary>
        public int Pages { get; }

        /// <summary>
        /// The response code from the server.
        /// </summary>
        public int ResponseCode { get; }

        /// <summary>
        /// The date and time reported by the server in UTC.
        /// </summary>
        public DateTime? Time { get; set; }

        /// <summary>Rate limit group from X-ESI-Error-Limit header (e.g., "esi-characters").</summary>
        public string? RateLimitGroup { get; set; }

        /// <summary>Remaining requests in current window from X-Esi-Error-Limit-Remain.</summary>
        public int? RateLimitRemaining { get; set; }

        /// <summary>Total request limit from X-Esi-Error-Limit-Reset.</summary>
        public int? RateLimitLimit { get; set; }

        /// <summary>Retry-After header value in seconds (for 429 responses).</summary>
        public int? RetryAfterSeconds { get; set; }

        /// <summary>
        /// Creates a new set of response parameters.
        /// </summary>
        /// <param name="code">The response returned by the server</param>
        public ResponseParams(HttpResponseMessage response) : this((int)response.StatusCode)
        {
            // Fill in header data
            var headers = response.Headers;
            ErrorCount = headers.ErrorCount();
            Pages = headers.PageCount();
            // ETag has quotes on it, keep them to reuse on output tag
            ETag = headers.ETag?.Tag;
            Expires = response.Content?.Headers?.Expires;
            Time = headers.Date?.UtcDateTime ?? DateTime.UtcNow;
            // ESI error reset time per best practices
            ErrorResetTime = headers.ErrorResetTime(Time);
            // Mirror error limit headers for TokenTracker consumption
            RateLimitRemaining = ErrorCount;
            RateLimitLimit = headers.ErrorResetSeconds();
            // Retry-After for 429 responses
            if (response.Headers.RetryAfter?.Delta is TimeSpan retryDelta)
                RetryAfterSeconds = (int)retryDelta.TotalSeconds;
            else if (response.Headers.RetryAfter?.Date is DateTimeOffset retryDate)
                RetryAfterSeconds = Math.Max(0, (int)(retryDate - DateTimeOffset.UtcNow).TotalSeconds);
        }

        /// <summary>
        /// Creates a new set of response parameters.
        /// </summary>
        /// <param name="code">The status code returned by the server</param>
        public ResponseParams(int responseCode)
        {
            ErrorCount = null;
            ETag = null;
            Expires = null;
            ResponseCode = responseCode;
            Time = null;
        }

        public override string ToString()
        {
            return string.Format("ResponseParams[code={0:D},expires={1}]", ResponseCode,
                Expires);
        }
    }
}
