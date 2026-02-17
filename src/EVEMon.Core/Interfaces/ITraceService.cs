using EVEMon.Core.Enumerations;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts diagnostic trace logging for decoupled diagnostics.
    /// Replaces direct calls to the static <c>EveMonClient.Trace()</c> method.
    /// </summary>
    /// <remarks>
    /// Trace output is written to the application's trace log file.
    /// In debug builds, output is also mirrored to <c>System.Diagnostics.Debug</c>.
    ///
    /// Production: <c>TraceService</c> in <c>EVEMon.Common/Services/TraceService.cs</c>.
    /// Testing: Provide a no-op stub, or capture messages in a <c>List&lt;string&gt;</c>
    /// for assertion.
    /// </remarks>
    public interface ITraceService
    {
        /// <summary>
        /// Gets or sets the minimum trace level. Messages below this level are suppressed.
        /// Defaults to <see cref="TraceLevel.Debug"/> (all messages shown).
        /// </summary>
        TraceLevel MinimumLevel { get; set; }

        /// <summary>
        /// Writes a trace message at <see cref="TraceLevel.Info"/> level.
        /// When <paramref name="printMethod"/> is true (the default),
        /// the calling method name is prepended to the output.
        /// </summary>
        void Trace(string message, bool printMethod = true);

        /// <summary>
        /// Writes a formatted trace message at <see cref="TraceLevel.Info"/> level.
        /// Always includes the calling method name in the output.
        /// </summary>
        void Trace(string format, params object[] args);

        /// <summary>
        /// Writes a trace message at the specified severity level.
        /// </summary>
        void Trace(TraceLevel level, string message, bool printMethod = true);

        /// <summary>
        /// Writes a formatted trace message at the specified severity level.
        /// </summary>
        void Trace(TraceLevel level, string format, params object[] args);

        /// <summary>
        /// Starts logging trace messages to a file at the given path.
        /// </summary>
        void StartLogging(string filePath);

        /// <summary>
        /// Stops logging and closes the trace file.
        /// </summary>
        void StopLogging();
    }
}
