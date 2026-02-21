// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using EVEMon.Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EVEMon.Tests.Logging
{
    public class TcpJsonLoggerProviderTests : IDisposable
    {
        // Use a unique port range to avoid conflicts with running app
        private static int s_nextPort = 15550;
        private readonly int _port;
        private readonly TcpJsonLoggerProvider _provider;

        public TcpJsonLoggerProviderTests()
        {
            _port = Interlocked.Increment(ref s_nextPort);
            Environment.SetEnvironmentVariable("EVEMON_DIAG_PORT", _port.ToString());
            _provider = new TcpJsonLoggerProvider();
        }

        public void Dispose()
        {
            _provider.Dispose();
            Environment.SetEnvironmentVariable("EVEMON_DIAG_PORT", null);
        }

        [Fact]
        public void CreateLogger_ReturnsNonNull()
        {
            // Act
            var logger = _provider.CreateLogger("TestCategory");

            // Assert
            logger.Should().NotBeNull();
        }

        [Fact]
        public void Logger_WritesJsonToConnectedClient()
        {
            // Arrange
            var logger = _provider.CreateLogger("TestCat");
            using var client = new TcpClient();
            client.Connect("127.0.0.1", _port);
            Thread.Sleep(100); // Allow accept loop to process

            // Act
            logger.LogInformation(new EventId(1, "TST"), "hello world");
            Thread.Sleep(100); // Allow write to propagate

            // Assert
            var stream = client.GetStream();
            stream.ReadTimeout = 1000;
            byte[] buffer = new byte[4096];
            int read = stream.Read(buffer, 0, buffer.Length);
            string json = Encoding.UTF8.GetString(buffer, 0, read).Trim();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            root.GetProperty("lvl").GetString().Should().Be("INF");
            root.GetProperty("tag").GetString().Should().Be("TST");
            root.GetProperty("cat").GetString().Should().Be("TestCat");
            root.GetProperty("msg").GetString().Should().Be("hello world");
            root.GetProperty("ts").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Logger_HandlesNoConnectedClients_WithoutException()
        {
            // Arrange
            var logger = _provider.CreateLogger("NobodyListening");

            // Act & Assert — should not throw
            var act = () => logger.LogInformation("message with no clients");
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_StopsListenerCleanly()
        {
            // Act
            _provider.Dispose();

            // Assert — second dispose should not throw
            var act = () => _provider.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void MultipleClients_AllReceiveSameMessage()
        {
            // Arrange
            var logger = _provider.CreateLogger("Multi");
            using var client1 = new TcpClient();
            using var client2 = new TcpClient();
            client1.Connect("127.0.0.1", _port);
            client2.Connect("127.0.0.1", _port);
            Thread.Sleep(150); // Allow accept loop

            // Act
            logger.LogWarning(new EventId(2, "WRN"), "dual message");
            Thread.Sleep(150);

            // Assert
            foreach (var client in new[] { client1, client2 })
            {
                var stream = client.GetStream();
                stream.ReadTimeout = 1000;
                byte[] buffer = new byte[4096];
                int read = stream.Read(buffer, 0, buffer.Length);
                string json = Encoding.UTF8.GetString(buffer, 0, read).Trim();

                using var doc = JsonDocument.Parse(json);
                doc.RootElement.GetProperty("msg").GetString().Should().Be("dual message");
                doc.RootElement.GetProperty("lvl").GetString().Should().Be("WRN");
            }
        }

        [Fact]
        public void DeadClient_IsCleanedUpOnWriteFailure()
        {
            // Arrange
            var logger = _provider.CreateLogger("Cleanup");
            using var client = new TcpClient();
            client.Connect("127.0.0.1", _port);
            Thread.Sleep(100);

            // Disconnect the client
            client.Close();
            Thread.Sleep(50);

            // Act & Assert — writing after client disconnect should not throw
            var act = () => logger.LogInformation("after disconnect");
            act.Should().NotThrow();
        }
    }
}
