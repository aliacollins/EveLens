using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using EVEMon.Common.Constants;
using EVEMon.Core.Interfaces;
using TraceLevel = EVEMon.Core.Enumerations.TraceLevel;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Standalone trace service that owns all diagnostic logging logic.
    /// Replaces the previous <see cref="TraceServiceAdapter"/> which delegated to EveMonClient.
    /// </summary>
    public sealed class TraceService : ITraceService
    {
        private StreamWriter? _traceStream;
        private TextWriterTraceListener? _traceListener;

        /// <inheritdoc />
        public TraceLevel MinimumLevel { get; set; } = TraceLevel.Debug;

        /// <inheritdoc />
        public void Trace(string message, bool printMethod = true)
            => Trace(TraceLevel.Info, message, printMethod);

        /// <inheritdoc />
        public void Trace(string format, params object[] args)
        {
            string message = string.Format(CultureConstants.DefaultCulture, format, args);
            Trace(TraceLevel.Info, message);
        }

        /// <inheritdoc />
        public void Trace(TraceLevel level, string message, bool printMethod = true)
        {
            if (level < MinimumLevel)
                return;

            string header = string.Empty;

            if (printMethod)
            {
                var stackTrace = new StackTrace();
                StackFrame? frame = stackTrace.GetFrame(1);
                MethodBase? method = frame?.GetMethod();

                if (method != null)
                {
                    int frameIndex = 1;
                    while (method.DeclaringType == typeof(TraceService) ||
                           method.Name == "MoveNext")
                    {
                        frameIndex++;
                        frame = stackTrace.GetFrame(frameIndex);
                        if (frame == null) break;
                        method = frame.GetMethod();
                    }

                    if (method?.Name == "MoveNext")
                    {
                        frame = stackTrace.GetFrame(frameIndex + 2);
                        if (frame != null)
                            method = frame.GetMethod();
                    }
                }

                Type? declaringType = method?.DeclaringType;
                header = $"{declaringType?.Name}.{method?.Name}";
            }

            string levelTag = level != TraceLevel.Info ? $"[{level}] " : string.Empty;
            string timeStr = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z > ";
            message = string.IsNullOrWhiteSpace(message) || !printMethod ? message : $" - {message}";
            string msgStr = $"{levelTag}{header}{message}";

            System.Diagnostics.Trace.WriteLine(
                $"{timeStr}{msgStr.TrimEnd(Environment.NewLine.ToCharArray())}");
        }

        /// <inheritdoc />
        public void Trace(TraceLevel level, string format, params object[] args)
        {
            if (level < MinimumLevel)
                return;

            string message = string.Format(CultureConstants.DefaultCulture, format, args);
            Trace(level, message);
        }

        /// <inheritdoc />
        public void StartLogging(string filePath)
        {
            try
            {
                System.Diagnostics.Trace.AutoFlush = true;
                _traceStream = File.CreateText(filePath);
                _traceListener = new TextWriterTraceListener(_traceStream);
                System.Diagnostics.Trace.Listeners.Add(_traceListener);
            }
            catch (IOException)
            {
                // Trace file is locked or path is inaccessible.
                // Continue without file logging — diagnostics are non-essential.
                _traceStream = null;
                _traceListener = null;
            }
        }

        /// <inheritdoc />
        public void StopLogging()
        {
            if (_traceListener == null)
                return;

            System.Diagnostics.Trace.Listeners.Remove(_traceListener);
            _traceListener.Close();
            _traceStream?.Close();
            _traceListener = null;
            _traceStream = null;
        }
    }
}
