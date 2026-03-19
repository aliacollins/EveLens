// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using EveLens.Common.Logging;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class DiagnosticStreamWindow : Window
    {
        private const int MaxLines = 2000;
        private readonly TcpJsonLoggerProvider? _provider;

        // Store all raw lines + formatted lines — filter rebuilds the display from these
        private readonly List<LogEntry> _allLines = new();
        private int _filterIndex;
        private bool _autoScroll = true;

        public DiagnosticStreamWindow()
        {
            InitializeComponent();

            _provider = AppServices.DiagnosticStream;

            FilterCombo.SelectionChanged += (_, _) =>
            {
                _filterIndex = FilterCombo.SelectedIndex;
                RebuildDisplay();
            };

            ClearBtn.Click += (_, _) =>
            {
                _allLines.Clear();
                LogOutput.Text = "";
                LineCountText.Text = "0 lines";
            };

            AutoScrollCheck.IsCheckedChanged += (_, _) =>
                _autoScroll = AutoScrollCheck.IsChecked == true;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (_provider == null)
            {
                StatusText.Text = "Diagnostic provider not available";
                return;
            }

            // Start the TCP listener if not already running
            if (!_provider.IsListening)
                _provider.Start();

            StatusText.Text = $"Listening on port {_provider.Port} | TCP clients can also connect via: nc localhost {_provider.Port}";
            _provider.OnLogLine += OnLogLine;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_provider != null)
                _provider.OnLogLine -= OnLogLine;

            base.OnClosed(e);
        }

        private void OnLogLine(string jsonLine)
        {
            var formatted = FormatLine(jsonLine);
            var entry = new LogEntry(jsonLine, formatted);
            Dispatcher.UIThread.Post(() => AppendEntry(entry));
        }

        private void AppendEntry(LogEntry entry)
        {
            try
            {
                _allLines.Add(entry);

                // Trim oldest if over capacity
                while (_allLines.Count > MaxLines)
                    _allLines.RemoveAt(0);

                // Only append to display if it passes current filter
                if (PassesFilter(entry.Raw, _filterIndex))
                {
                    LogOutput.Text += entry.Formatted + "\n";
                    LineCountText.Text = $"{CountVisible()} / {_allLines.Count} lines";

                    if (_autoScroll)
                        LogScroller.ScrollToEnd();
                }
            }
            catch
            {
                // Non-critical
            }
        }

        /// <summary>
        /// Rebuilds the entire display from stored lines using the current filter.
        /// Called when the filter changes.
        /// </summary>
        private void RebuildDisplay()
        {
            var sb = new StringBuilder();
            int visible = 0;

            foreach (var entry in _allLines)
            {
                if (PassesFilter(entry.Raw, _filterIndex))
                {
                    sb.AppendLine(entry.Formatted);
                    visible++;
                }
            }

            LogOutput.Text = sb.ToString();
            LineCountText.Text = $"{visible} / {_allLines.Count} lines";

            if (_autoScroll)
                LogScroller.ScrollToEnd();
        }

        private int CountVisible()
        {
            int count = 0;
            foreach (var entry in _allLines)
                if (PassesFilter(entry.Raw, _filterIndex))
                    count++;
            return count;
        }

        private static string FormatLine(string jsonLine)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonLine);
                var root = doc.RootElement;

                string ts = root.TryGetProperty("ts", out var tsEl) ? tsEl.GetString() ?? "" : "";
                string lvl = root.TryGetProperty("lvl", out var lvlEl) ? lvlEl.GetString() ?? "" : "";
                string tag = root.TryGetProperty("tag", out var tagEl) ? tagEl.GetString() ?? "" : "";
                string msg = root.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() ?? "" : "";

                // Extract just time portion for compact display
                if (ts.Length > 11)
                    ts = ts.Substring(11, Math.Min(12, ts.Length - 11)).TrimEnd('Z');

                return $"[{ts}] {lvl,-3} {tag,-5} {msg}";
            }
            catch
            {
                return jsonLine;
            }
        }

        private static bool PassesFilter(string jsonLine, int filterIndex)
        {
            if (filterIndex == 0) return true;

            try
            {
                return filterIndex switch
                {
                    1 => jsonLine.Contains("\"ESI\"") || jsonLine.Contains("\"FETCH\""),
                    2 => jsonLine.Contains("\"EVT\""),
                    3 => jsonLine.Contains("\"WRN\"") || jsonLine.Contains("\"ERR\"")
                         || jsonLine.Contains("\"CRT\""),
                    4 => jsonLine.Contains("Health") || jsonLine.Contains("health"),
                    5 => jsonLine.Contains("Scheduler") || jsonLine.Contains("scheduler")
                         || jsonLine.Contains("\"FETCH\""),
                    _ => true
                };
            }
            catch
            {
                return true;
            }
        }

        private readonly record struct LogEntry(string Raw, string Formatted);
    }
}
