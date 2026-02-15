namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts diagnostic tracing for decoupled logging.
    /// Replaces direct dependency on <c>EveMonClient.Trace()</c>.
    /// </summary>
    public interface ITraceService
    {
        /// <summary>
        /// Writes a trace message.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        /// <param name="printMethod">When true, includes the calling method name.</param>
        void Trace(string message, bool printMethod = true);

        /// <summary>
        /// Writes a formatted trace message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        void Trace(string format, params object[] args);
    }
}
