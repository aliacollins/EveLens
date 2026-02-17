namespace EVEMon.Core.Enumerations
{
    /// <summary>
    /// Severity levels for diagnostic trace messages.
    /// </summary>
    public enum TraceLevel
    {
        /// <summary>Verbose diagnostic detail, useful during development.</summary>
        Debug = 0,
        /// <summary>Normal operational messages.</summary>
        Info = 1,
        /// <summary>Non-critical issues that may require attention.</summary>
        Warning = 2,
        /// <summary>Errors that affect functionality.</summary>
        Error = 3
    }
}
