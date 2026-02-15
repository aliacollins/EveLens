using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The contract availability.
    /// </summary>
    public enum ContractAvailability
    {
        None,

        [Description("Public")]
        Public,

        [Description("Private")]
        Private
    }
}