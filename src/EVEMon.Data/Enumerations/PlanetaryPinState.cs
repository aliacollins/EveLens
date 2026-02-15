using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The status of a planetary pin.
    /// </summary>
    public enum PlanetaryPinState
    {
        None,

        [Description("Extracting")]
        Extracting,

        [Description("In production")]
        Producing,

        [Description("Idle")]
        Idle
    }
}