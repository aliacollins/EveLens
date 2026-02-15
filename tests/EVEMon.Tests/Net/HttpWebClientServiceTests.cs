using System;
using System.Net.Http;
using System.Net.Http.Headers;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Net;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Net
{
    public class DownloadResultTests
    {
        [Fact]
        public void Constructor_SuccessResult_StoresValue()
        {
            var result = new DownloadResult<string>("hello", null);
            result.Result.Should().Be("hello");
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Constructor_ErrorResult_StoresError()
        {
            var error = new HttpWebClientServiceException("test error");
            var result = new DownloadResult<string>(null, error);
            result.Result.Should().BeNull();
            result.Error.Should().BeSameAs(error);
        }

        [Fact]
        public void Constructor_NullResponse_CreatesDefaultResponse()
        {
            var result = new DownloadResult<string>("data", null, null);
            result.Response.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithResponse_StoresResponse()
        {
            var response = new ResponseParams(200);
            var result = new DownloadResult<string>("data", null, response);
            result.Response.Should().BeSameAs(response);
        }

        [Fact]
        public void GenericType_WorksWithDifferentTypes()
        {
            var intResult = new DownloadResult<int>(42, null);
            intResult.Result.Should().Be(42);

            var boolResult = new DownloadResult<bool>(true, null);
            boolResult.Result.Should().BeTrue();
        }
    }

    public class RequestParamsTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var p = new RequestParams();
            p.AcceptEncoded.Should().BeFalse();
            p.Authentication.Should().BeNull();
            p.Compression.Should().Be(DataCompression.None);
            p.Content.Should().BeNull();
            p.ContentType.Should().Be("application/x-www-form-urlencoded");
            p.ETag.Should().BeNull();
            p.IfModifiedSince.Should().BeNull();
            p.Method.Should().Be(HttpMethod.Get);
        }

        [Fact]
        public void ContentConstructor_SetsPostMethod()
        {
            var p = new RequestParams("key=value");
            p.Content.Should().Be("key=value");
            p.Method.Should().Be(HttpMethod.Post);
        }

        [Fact]
        public void ContentConstructor_EmptyContent_StaysGet()
        {
            var p = new RequestParams("");
            p.Method.Should().Be(HttpMethod.Get);
            p.Content.Should().BeNull();
        }

        [Fact]
        public void ContentConstructor_NullContent_StaysGet()
        {
            var p = new RequestParams((string)null!);
            p.Method.Should().Be(HttpMethod.Get);
        }

        [Fact]
        public void ResponseConstructor_CopiesETagAndExpires()
        {
            var expires = DateTimeOffset.UtcNow.AddHours(1);
            var response = new ResponseParams(200) { ETag = "abc123", Expires = expires };
            var p = new RequestParams(response);
            p.ETag.Should().Be("abc123");
            p.IfModifiedSince.Should().Be(expires);
        }

        [Fact]
        public void AuthHeader_BearerToken_CreatesCorrectHeader()
        {
            var p = new RequestParams { Authentication = "my-token-123" };
            var header = p.AuthHeader;
            header.Should().NotBeNull();
            header.Scheme.Should().Be("Bearer");
            header.Parameter.Should().Be("my-token-123");
        }

        [Fact]
        public void AuthHeader_CustomScheme_ParsesCorrectly()
        {
            var p = new RequestParams { Authentication = "Basic dXNlcjpwYXNz" };
            var header = p.AuthHeader;
            header.Scheme.Should().Be("Basic");
            header.Parameter.Should().Be("dXNlcjpwYXNz");
        }

        [Fact]
        public void AuthHeader_NullAuth_ReturnsNull()
        {
            var p = new RequestParams();
            p.AuthHeader.Should().BeNull();
        }

        [Fact]
        public void MethodChecked_NoContent_ReturnsGet()
        {
            var p = new RequestParams { Method = HttpMethod.Post, Content = null! };
            p.MethodChecked.Should().Be(HttpMethod.Get);
        }

        [Fact]
        public void MethodChecked_WithContent_ReturnsConfiguredMethod()
        {
            var p = new RequestParams("data") { Method = HttpMethod.Post };
            p.MethodChecked.Should().Be(HttpMethod.Post);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var p = new RequestParams();
            p.ToString().Should().Contain("RequestParams");
            p.ToString().Should().Contain("GET");
        }
    }
}
