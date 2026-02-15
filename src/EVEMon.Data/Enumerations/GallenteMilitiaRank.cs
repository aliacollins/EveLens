using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of Gallente Militia ranking.
    /// </summary>
    public enum GallenteMilitiaRank
    {
        [Description("Federation Minuteman")]
        FederationMinuteman = 0,

        [Description("Defender Lieutenant")]
        DefenderLieutenant = 1,

        [Description("Guardian Lieutenant")]
        GuardianLieutenant = 2,

        [Description("Lieutenant Sentinel")]
        LieutenantSentinel = 3,

        [Description("Shield Commander")]
        ShieldCommander = 4,

        [Description("Aegis Commander")]
        AegisCommander = 5,

        [Description("Vice Commander")]
        ViceCommander = 6,

        [Description("Major General")]
        MajorGeneral = 7,

        [Description("Lieutenant General")]
        LieutenantGeneral = 8,

        [Description("Luminaire General")]
        LuminaireGeneral = 9,
    }
}