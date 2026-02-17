namespace EVEMon.Core.Enumerations
{
    /// <summary>
    /// Priority level for ESI fetch jobs. Determines scheduling frequency and jitter.
    /// </summary>
    public enum FetchPriority
    {
        /// <summary>Visible character — fetched immediately when cache expires.</summary>
        Active,

        /// <summary>Background character — fetched with small jitter after cache expires.</summary>
        Background,

        /// <summary>Character not viewed recently — larger jitter, reduced frequency.</summary>
        Dormant,

        /// <summary>Endpoint disabled or character unmonitored — not scheduled.</summary>
        Off
    }
}
