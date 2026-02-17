using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EVEMon.Common.Logging
{
    /// <summary>
    /// MEL logger provider that streams JSON-lines to connected TCP clients.
    /// Claude Code connects via <c>nc localhost 5555</c> from WSL to receive structured log entries.
    /// </summary>
    /// <remarks>
    /// Listens on <c>IPAddress.Loopback</c> only (no external exposure).
    /// Port configurable via <c>EVEMON_DIAG_PORT</c> environment variable (default 5555).
    /// Dead clients are cleaned up on write failure.
    /// </remarks>
    public sealed class TcpJsonLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentBag<TcpClient> _clients = new ConcurrentBag<TcpClient>();
        private readonly TcpListener? _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        /// <summary>
        /// Creates a new TCP JSON logger provider that listens for diagnostic clients.
        /// </summary>
        public TcpJsonLoggerProvider()
        {
            int port = 5555;
            string? envPort = Environment.GetEnvironmentVariable("EVEMON_DIAG_PORT");
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out int parsed) && parsed > 0 && parsed <= 65535)
                port = parsed;

            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _ = AcceptClientsAsync(_cts.Token);
            }
            catch (SocketException)
            {
                // Port occupied — continue without TCP streaming (non-fatal)
                _listener = null;
                System.Diagnostics.Trace.WriteLine(
                    $"TcpJsonLoggerProvider: port {port} occupied, TCP streaming disabled");
            }
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName) => new TcpJsonLogger(categoryName, this);

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cts.Cancel();

            _listener?.Stop();

            while (_clients.TryTake(out var client))
            {
                try { client.Close(); } catch { /* best effort */ }
            }

            _cts.Dispose();
        }

        private async Task AcceptClientsAsync(CancellationToken ct)
        {
            if (_listener == null)
                return;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    client.NoDelay = true;
                    _clients.Add(client);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    // Transient accept failure — continue listening
                }
            }
        }

        /// <summary>
        /// Writes a JSON-line to all connected TCP clients. Dead clients are removed.
        /// </summary>
        internal void WriteToClients(string jsonLine)
        {
            if (_disposed || _clients.IsEmpty)
                return;

            byte[] data = Encoding.UTF8.GetBytes(jsonLine + "\n");
            var deadClients = new ConcurrentBag<TcpClient>();

            foreach (var client in _clients)
            {
                try
                {
                    if (!client.Connected)
                    {
                        deadClients.Add(client);
                        continue;
                    }

                    var stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
                catch
                {
                    deadClients.Add(client);
                }
            }

            // Clean up dead clients
            if (!deadClients.IsEmpty)
            {
                // ConcurrentBag doesn't support removal, so rebuild
                var survivors = new ConcurrentBag<TcpClient>();
                while (_clients.TryTake(out var c))
                {
                    bool isDead = false;
                    foreach (var dead in deadClients)
                    {
                        if (ReferenceEquals(c, dead))
                        {
                            isDead = true;
                            try { c.Close(); } catch { /* best effort */ }
                            break;
                        }
                    }
                    if (!isDead)
                        survivors.Add(c);
                }

                foreach (var s in survivors)
                    _clients.Add(s);
            }
        }

        /// <summary>
        /// Logger that formats entries as JSON lines and writes to TCP clients.
        /// </summary>
        private sealed class TcpJsonLogger : ILogger
        {
            private readonly string _category;
            private readonly TcpJsonLoggerProvider _provider;

            public TcpJsonLogger(string category, TcpJsonLoggerProvider provider)
            {
                _category = category;
                _provider = provider;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                string msg = formatter(state, exception);
                if (exception != null)
                    msg += " | " + exception.Message;

                string lvl = logLevel switch
                {
                    LogLevel.Trace => "TRC",
                    LogLevel.Debug => "DBG",
                    LogLevel.Information => "INF",
                    LogLevel.Warning => "WRN",
                    LogLevel.Error => "ERR",
                    LogLevel.Critical => "CRT",
                    _ => "???"
                };

                string tag = !string.IsNullOrEmpty(eventId.Name) ? eventId.Name : "LOG";

                string json = JsonSerializer.Serialize(new
                {
                    ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    lvl,
                    tag,
                    cat = _category,
                    msg
                });

                _provider.WriteToClients(json);
            }
        }
    }
}
