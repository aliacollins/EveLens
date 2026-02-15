using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of standing group.
    /// </summary>
    public enum StandingGroup
    {
        [Description("Agents")]
        Agents,

        [Description("NPC Corporations")]
        NPCCorporations,

        [Description("Factions")]
        Factions
    }
}