// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Net;
using EveLens.Common.Net;
using EveLens.Common.Serialization.Eve;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.QueryMonitor
{
    /// <summary>
    /// Behavior tests for QueryMonitor infrastructure.
    /// Complements QueryMonitorTests (ResponseParamsTests, DownloadResultIntegrationTests)
    /// by covering additional EsiResult behavior, ResponseParams edge cases,
    /// and DownloadResult patterns not already tested.
    /// Direct QueryMonitor&lt;T&gt; testing is impractical without full EveLensClient
    /// initialization; these tests cover the supporting types in isolation.
    /// </summary>
    public class EsiResultBehaviorTests
    {
        #region EsiResult from ResponseParams

        [Fact]
        public void EsiResult_200Response_HasNoError()
        {
            var response = new ResponseParams(200)
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            var result = new EsiResult<string>(response, "test-data");

            result.HasError.Should().BeFalse();
            result.HasData.Should().BeTrue();
            result.Result.Should().Be("test-data");
        }

        [Fact]
        public void EsiResult_304NotModified_HasDataIsFalse()
        {
            var response = new ResponseParams((int)HttpStatusCode.NotModified)
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            var result = new EsiResult<string>(response);

            result.HasData.Should().BeFalse();
        }

        [Fact]
        public void EsiResult_CachedUntil_SetFromExpires()
        {
            var futureExpiry = DateTimeOffset.UtcNow.AddMinutes(60);
            var response = new ResponseParams(200)
            {
                Expires = futureExpiry
            };

            var result = new EsiResult<string>(response, "data");

            // CachedUntil should be approximately the expires time plus jitter (5 seconds)
            result.CachedUntil.Should().BeAfter(DateTime.UtcNow);
            result.CachedUntil.Should().BeCloseTo(futureExpiry.UtcDateTime.AddSeconds(5), TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void EsiResult_NullExpires_CachedUntilIsErrorCacheTime()
        {
            var response = new ResponseParams(200);
            // Expires is null by default

            var result = new EsiResult<string>(response, "data");

            // Should use error cache time: UtcNow + 2 minutes
            result.CachedUntil.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(2), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void EsiResult_ErrorCode_MatchesResponseCode()
        {
            var response = new ResponseParams(403);

            var result = new EsiResult<string>(response);

            result.ErrorCode.Should().Be(403);
        }

        [Fact]
        public void EsiResult_FromException_HasError()
        {
            var ex = new HttpWebClientServiceException("API Error");

            var result = new EsiResult<string>(ex);

            result.HasError.Should().BeTrue();
            result.HasData.Should().BeFalse();
            result.CachedUntil.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(2), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void EsiResult_PastExpires_GetsAdjustedToServerTime()
        {
            // When expires is in the past (relative to server time), CachedUntil
            // should be adjusted to server time + jitter
            var pastExpiry = DateTimeOffset.UtcNow.AddMinutes(-10);
            var serverTime = DateTime.UtcNow;
            var response = new ResponseParams(200)
            {
                Expires = pastExpiry,
                Time = serverTime
            };

            var result = new EsiResult<string>(response, "data");

            // Should be server time + 5s jitter, not the past expires value
            result.CachedUntil.Should().BeAfter(DateTime.UtcNow.AddSeconds(-1));
        }

        #endregion

        #region EsiResult with Null Results

        [Fact]
        public void EsiResult_NullResult_StillHasData()
        {
            var response = new ResponseParams(200)
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            var result = new EsiResult<string>(response);

            // HasData is based on response code (200 != 304), not the result value
            result.HasData.Should().BeTrue();
            result.Result.Should().BeNull();
        }

        #endregion
    }

    /// <summary>
    /// Additional ResponseParams behavior tests beyond what QueryMonitorTests covers.
    /// </summary>
    public class ResponseParamsBehaviorTests
    {
        [Theory]
        [InlineData(200, true)]
        [InlineData(201, false)]
        [InlineData(301, false)]
        [InlineData(304, false)]
        [InlineData(400, false)]
        [InlineData(500, false)]
        public void IsOKResponse_OnlyTrueFor200(int statusCode, bool expected)
        {
            var response = new ResponseParams(statusCode);
            response.IsOKResponse.Should().Be(expected);
        }

        [Theory]
        [InlineData(304, true)]
        [InlineData(200, false)]
        [InlineData(302, false)]
        [InlineData(404, false)]
        public void IsNotModifiedResponse_OnlyTrueFor304(int statusCode, bool expected)
        {
            var response = new ResponseParams(statusCode);
            response.IsNotModifiedResponse.Should().Be(expected);
        }

        [Fact]
        public void ResponseParams_AllSettableProperties_CanBeSetTogether()
        {
            var now = DateTime.UtcNow;
            var expires = DateTimeOffset.UtcNow.AddMinutes(30);

            var response = new ResponseParams(200)
            {
                ETag = "\"etag-value\"",
                Expires = expires,
                Time = now,
                ErrorCount = 5
            };

            response.ResponseCode.Should().Be(200);
            response.ETag.Should().Be("\"etag-value\"");
            response.Expires.Should().Be(expires);
            response.Time.Should().Be(now);
            response.ErrorCount.Should().Be(5);
            // Pages is read-only (set from HttpResponseMessage), defaults to 0
            response.Pages.Should().Be(0);
        }

        [Fact]
        public void ResponseParams_DefaultValues()
        {
            var response = new ResponseParams(200);

            response.ETag.Should().BeNull();
            response.Expires.Should().BeNull();
            response.Time.Should().BeNull();
            response.ErrorCount.Should().BeNull();
            response.Pages.Should().Be(0);
        }
    }

    /// <summary>
    /// Additional DownloadResult behavior tests.
    /// </summary>
    public class DownloadResultBehaviorTests
    {
        [Fact]
        public void DownloadResult_SuccessWithNullError_NoErrorFlag()
        {
            var response = new ResponseParams(200);
            var result = new DownloadResult<string>("data", null, response);

            result.Error.Should().BeNull();
            result.Result.Should().Be("data");
        }

        [Fact]
        public void DownloadResult_WithError_KeepsErrorInfo()
        {
            var response = new ResponseParams(429);
            var error = new HttpWebClientServiceException("Rate limited");
            var result = new DownloadResult<string>(null, error, response);

            result.Error.Should().NotBeNull();
            result.Error!.Message.Should().Be("Rate limited");
            result.Response.ResponseCode.Should().Be(429);
        }

        [Fact]
        public void DownloadResult_CanHoldComplexTypes()
        {
            var response = new ResponseParams(200);
            var data = new[] { 1, 2, 3 };
            var result = new DownloadResult<int[]>(data, null, response);

            result.Result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void DownloadResult_ResponseParams_Preserved()
        {
            var expires = DateTimeOffset.UtcNow.AddHours(1);
            var response = new ResponseParams(200)
            {
                ETag = "\"v2\"",
                Expires = expires
            };

            var result = new DownloadResult<string>("data", null, response);

            result.Response.ETag.Should().Be("\"v2\"");
            result.Response.Expires.Should().Be(expires);
            // Pages is read-only; defaults to 0 when constructed from int
            result.Response.Pages.Should().Be(0);
        }
    }
}
