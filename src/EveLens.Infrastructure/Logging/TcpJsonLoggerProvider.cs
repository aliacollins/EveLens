// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EveLens.Common.Logging
{
    /// <summary>
    /// MEL logger provider that streams JSON-lines to connected TCP clients.
    /// Connect via <c>nc localhost 5555</c> to receive structured log entries.
    /// </summary>
    /// <remarks>
    /// Listens on <c>IPAddress.Any</c>.
    /// Port configurable via <c>EVELENS_DIAG_PORT</c> environment variable (default 5555).
    /// Dead clients are cleaned up on write failure.
    /// In debug builds, listening is deferred until toggled on via the Debug menu.
    /// </remarks>
    public sealed class TcpJsonLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentBag<TcpClient> _clients = new ConcurrentBag<TcpClient>();
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private bool _disposed;
        private readonly int _port;

        /// <summary>Whether the TCP listener is currently active.</summary>
        public bool IsListening => _listener != null && !_disposed;

        /// <summary>The port the listener uses (or would use).</summary>
        public int Port => _port;

        /// <summary>Number of connected clients.</summary>
        public int ClientCount => _clients.Count;

        /// <summary>
        /// Creates a new TCP JSON logger provider.
        /// </summary>
        /// <param name="autoStart">If true, starts listening immediately. If false, waits for <see cref="Start"/>.</param>
        public TcpJsonLoggerProvider(bool autoStart = true)
        {
            _port = 5555;
            string? envPort = Environment.GetEnvironmentVariable("EVELENS_DIAG_PORT");
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out int parsed) && parsed > 0 && parsed <= 65535)
                _port = parsed;

            if (autoStart)
                Start();
        }

        /// <summary>
        /// Starts the TCP listener. No-op if already listening.
        /// </summary>
        /// <returns>True if started successfully, false if port is occupied.</returns>
        public bool Start()
        {
            if (_disposed || _listener != null)
                return _listener != null;

            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _ = AcceptClientsAsync(_cts.Token);
                return true;
            }
            catch (SocketException)
            {
                _listener = null;
                _cts?.Dispose();
                _cts = null;
                System.Diagnostics.Trace.WriteLine(
                    $"TcpJsonLoggerProvider: port {_port} occupied, TCP streaming disabled");
                return false;
            }
        }

        /// <summary>
        /// Stops the TCP listener and disconnects all clients.
        /// Can be restarted with <see cref="Start"/>.
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;

            while (_clients.TryTake(out var client))
            {
                try { client.Close(); } catch { /* best effort */ }
            }

            _cts?.Dispose();
            _cts = null;
        }

        public ILogger CreateLogger(string categoryName) => new TcpJsonLogger(categoryName, this);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
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
        /// In-process callback for log lines. The in-app diagnostic viewer subscribes here
        /// instead of connecting via TCP. Invoked on the logging thread — marshal to UI if needed.
        /// </summary>
        public event Action<string>? OnLogLine;

        /// <summary>
        /// Writes a JSON-line to all connected TCP clients and in-process subscribers.
        /// Dead clients are removed.
        /// </summary>
        internal void WriteToClients(string jsonLine)
        {
            if (_disposed)
                return;

            // Always fire in-process callback (even if no TCP clients)
            OnLogLine?.Invoke(jsonLine);

            if (_clients.IsEmpty)
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
