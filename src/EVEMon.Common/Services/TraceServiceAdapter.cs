using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    public sealed class TraceServiceAdapter : ITraceService
    {
        public void Trace(string message, bool printMethod = true)
        {
            EveMonClient.Trace(message, printMethod);
        }

        public void Trace(string format, params object[] args)
        {
            EveMonClient.Trace(format, args);
        }
    }
}
