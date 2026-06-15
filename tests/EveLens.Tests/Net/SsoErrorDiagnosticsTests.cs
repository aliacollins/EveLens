// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Net;
using EveLens.Common.Enumerations;
using EveLens.Common.Net;
using EveLens.Common.Service;
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

        // --- Classification (Issue #94): the body's OAuth error code drives the user-facing
        // behaviour. CCP returns 400 for BOTH invalid_grant and invalid_client, so the body —
        // not the status — must be authoritative.

        [Theory]
        [InlineData("invalid_grant", SsoTokenError.InvalidGrant)]
        [InlineData("unauthorized_client", SsoTokenError.InvalidGrant)]
        [InlineData("invalid_client", SsoTokenError.InvalidClient)]
        [InlineData("invalid_request", SsoTokenError.InvalidClient)]
        [InlineData("invalid_scope", SsoTokenError.InvalidClient)]
        public void ClassifyTokenError_UsesOAuthErrorCodeFromBody(string code, SsoTokenError expected)
        {
            var ex = new HttpWebClientServiceException("token request failed")
            {
                StatusCode = HttpStatusCode.BadRequest,
                ResponseBody = "{\"error\":\"" + code + "\"}",
            };

            SSOAuthenticationService.ClassifyTokenError(ex).Should().Be(expected);
        }

        [Fact]
        public void ClassifyTokenError_DeadRefreshToken_IsInvalidGrant_NotTransient()
        {
            // The core Issue #94 case: a rotated/expired refresh token. Must be InvalidGrant so
            // ESIKey clears the token and stops retrying every 5s.
            var ex = new HttpWebClientServiceException("token request failed")
            {
                StatusCode = HttpStatusCode.BadRequest,
                ResponseBody = "{\"error\":\"invalid_grant\",\"error_description\":\"refresh token expired\"}",
            };

            SSOAuthenticationService.ClassifyTokenError(ex).Should().Be(SsoTokenError.InvalidGrant);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        [InlineData(HttpStatusCode.RequestTimeout)]
        public void ClassifyTokenError_ServerOrTimeout_IsTransient(HttpStatusCode status)
        {
            // No usable body — a 5xx/timeout is not the token's fault and should retry silently.
            var ex = new HttpWebClientServiceException("server error")
            {
                StatusCode = status,
            };

            SSOAuthenticationService.ClassifyTokenError(ex).Should().Be(SsoTokenError.Transient);
        }

        [Fact]
        public void ClassifyTokenError_4xxWithoutBody_IsInvalidGrant()
        {
            // A 4xx rejection with no readable error code: the request can't succeed on retry,
            // so treat it as a dead grant rather than spinning forever.
            var ex = new HttpWebClientServiceException("bad request")
            {
                StatusCode = HttpStatusCode.BadRequest,
            };

            SSOAuthenticationService.ClassifyTokenError(ex).Should().Be(SsoTokenError.InvalidGrant);
        }

        [Fact]
        public void ClassifyTokenError_UnrecognizedBodyCode_FallsBackToStatus()
        {
            // Unknown OAuth code + 5xx → status fallback says Transient.
            var ex = new HttpWebClientServiceException("weird")
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ResponseBody = "{\"error\":\"teapot\"}",
            };

            SSOAuthenticationService.ClassifyTokenError(ex).Should().Be(SsoTokenError.Transient);
        }
    }
}
