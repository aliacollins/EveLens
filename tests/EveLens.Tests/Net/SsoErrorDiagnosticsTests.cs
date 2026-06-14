// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Net;
using EveLens.Common.Net;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Net
{
    /// <summary>
    /// Regression coverage for Issue #94 ("Error logging in to EVE SSO"). When a token
    /// (re)fresh request failed, EveLens discarded CCP's response body and surfaced only a
    /// generic message — leaving no way to tell why SSO failed. The HTTP layer now captures the
    /// error body on <see cref="HttpWebClientServiceException.ResponseBody"/> so the real reason
    /// (e.g. <c>invalid_grant</c>) reaches the log and the diagnostic stream.
    /// </summary>
    public class SsoErrorDiagnosticsTests
    {
        [Fact]
        public void HttpWebClientServiceException_CanCarryResponseBody()
        {
            var ex = new HttpWebClientServiceException("token request failed")
            {
                StatusCode = HttpStatusCode.BadRequest,
                ResponseBody = "{\"error\":\"invalid_grant\",\"error_description\":\"refresh token expired\"}",
            };

            ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.ResponseBody.Should().Contain("invalid_grant");
            ex.ResponseBody.Should().Contain("refresh token expired");
        }

        [Fact]
        public void HttpWebClientServiceException_ResponseBody_DefaultsToNull_WhenNotSet()
        {
            var ex = new HttpWebClientServiceException("some error");

            // Not set on the non-HTTP construction path; must not throw when read.
            ex.ResponseBody.Should().BeNull();
        }

        [Fact]
        public void HttpWebClientException_Factory_ProducesExceptionThatAcceptsBody()
        {
            // Mirrors the SendAsync error path: build via the factory, then attach the body.
            var url = new System.Uri("https://login.eveonline.com/v2/oauth/token");
            var inner = new System.Net.Http.HttpRequestException("400");

            var ex = HttpWebClientServiceException.HttpWebClientException(
                url, inner, HttpStatusCode.BadRequest);
            ex.ResponseBody = "{\"error\":\"invalid_client\"}";

            ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Url.Should().Be(url);
            ex.ResponseBody.Should().Contain("invalid_client");
        }
    }
}
