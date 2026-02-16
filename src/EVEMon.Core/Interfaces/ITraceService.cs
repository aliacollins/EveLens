namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts diagnostic trace logging for decoupled diagnostics.
    /// Replaces direct calls to the static <c>EveMonClient.Trace()</c> method.
    /// </summary>
    /// <remarks>
    /// Trace output is written to the application's trace log file located at
    /// <c>IApplicationPaths.TraceFilePath</c>. In debug builds, output is also
    /// mirrored to <c>System.Diagnostics.Debug</c>.
    ///
    /// Production: <c>TraceServiceAdapter</c> in <c>EVEMon.Common/Services/TraceServiceAdapter.cs</c>
    /// (delegates to <c>EveMonClient.Trace()</c>).
    /// Testing: Provide a no-op stub, or capture messages in a <c>List&lt;string&gt;</c>
    /// for assertion.
    /// </remarks>
    public interface ITraceService
    {
        /// <summary>
        /// Writes a trace message. When <paramref name="printMethod"/> is true (the default),
        /// the calling method name is prepended to the output.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        /// <param name="printMethod">When true, includes the calling method name in the output.</param>
        void Trace(string message, bool printMethod = true);

        /// <summary>
        /// Writes a formatted trace message using <c>string.Format</c> semantics.
        /// Always includes the calling method name in the output.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="args">The format arguments.</param>
        void Trace(string format, params object[] args);
    }
}
