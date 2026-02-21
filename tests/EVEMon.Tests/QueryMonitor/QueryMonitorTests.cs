// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Net;
using EVEMon.Common.Net;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.QueryMonitor
{
    /// <summary>
    /// Tests for QueryMonitor infrastructure classes.
    /// Direct QueryMonitor<T> testing requires EveMonClient + CCPCharacter initialization.
    /// These tests cover the supporting types that can be tested in isolation.
    /// </summary>
    public class ResponseParamsTests
    {
        [Fact]
        public void Constructor_WithStatusCode_SetsResponseCode()
        {
            var response = new ResponseParams(200);
            response.ResponseCode.Should().Be(200);
        }

        [Fact]
        public void IsOKResponse_Returns200_True()
        {
            var response = new ResponseParams(200);
            response.IsOKResponse.Should().BeTrue();
        }

        [Fact]
        public void IsOKResponse_Returns404_False()
        {
            var response = new ResponseParams(404);
            response.IsOKResponse.Should().BeFalse();
        }

        [Fact]
        public void IsNotModifiedResponse_Returns304_True()
        {
            var response = new ResponseParams(304);
            response.IsNotModifiedResponse.Should().BeTrue();
        }

        [Fact]
        public void IsNotModifiedResponse_Returns200_False()
        {
            var response = new ResponseParams(200);
            response.IsNotModifiedResponse.Should().BeFalse();
        }

        [Fact]
        public void ETag_DefaultsToNull()
        {
            var response = new ResponseParams(200);
            response.ETag.Should().BeNull();
        }

        [Fact]
        public void ETag_CanBeSetAndRetrieved()
        {
            var response = new ResponseParams(200) { ETag = "\"abc123\"" };
            response.ETag.Should().Be("\"abc123\"");
        }

        [Fact]
        public void Expires_DefaultsToNull()
        {
            var response = new ResponseParams(200);
            response.Expires.Should().BeNull();
        }

        [Fact]
        public void Expires_CanBeSetAndRetrieved()
        {
            var expires = DateTimeOffset.UtcNow.AddHours(1);
            var response = new ResponseParams(200) { Expires = expires };
            response.Expires.Should().Be(expires);
        }

        [Fact]
        public void ErrorCount_DefaultsToNull()
        {
            var response = new ResponseParams(200);
            response.ErrorCount.Should().BeNull();
        }

        [Fact]
        public void Time_DefaultsToNull()
        {
            var response = new ResponseParams(200);
            response.Time.Should().BeNull();
        }

        [Fact]
        public void Pages_DefaultsToZero()
        {
            var response = new ResponseParams(200);
            response.Pages.Should().Be(0);
        }
    }

    public class DownloadResultIntegrationTests
    {
        [Fact]
        public void SuccessResult_WithResponse_CapturesBoth()
        {
            var response = new ResponseParams(200) { ETag = "\"v1\"" };
            var result = new DownloadResult<string>("data", null, response);

            result.Result.Should().Be("data");
            result.Error.Should().BeNull();
            result.Response.ETag.Should().Be("\"v1\"");
            result.Response.IsOKResponse.Should().BeTrue();
        }

        [Fact]
        public void ErrorResult_WithResponse_CapturesStatusCode()
        {
            var response = new ResponseParams(503);
            var error = new HttpWebClientServiceException("Service Unavailable");
            var result = new DownloadResult<string>(null, error, response);

            result.Result.Should().BeNull();
            result.Error.Should().NotBeNull();
            result.Response.ResponseCode.Should().Be(503);
            result.Response.IsOKResponse.Should().BeFalse();
        }

        [Fact]
        public void NotModifiedResponse_IndicatesCache()
        {
            var response = new ResponseParams(304) { ETag = "\"same\"" };
            var result = new DownloadResult<string>(null, null, response);

            result.Response.IsNotModifiedResponse.Should().BeTrue();
        }
    }
}
