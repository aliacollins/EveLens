using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of Minmatar Militia ranking.
    /// </summary>
    public enum MinmatarMilitiaRank
    {
        [Description("Nation Warrior")]
        NationWarrior = 0,

        [Description("Spike Lieutenant")]
        SpikeLieutenant = 1,

        [Description("Spear Lieutenant")]
        SpearLieutenant = 2,

        [Description("Venge Captain")]
        VengeCaptain = 3,

        [Description("Lance Commander")]
        LanceCommander = 4,

        [Description("Blade Commander")]
        BladeCommander = 5,

        [Description("Talon Commander")]
        TalonCommander = 6,

        [Description("Voshud Major")]
        VoshudMajor = 7,

        [Description("Matar Colonel")]
        MatarGeneral = 8,

        [Description("Valklear General")]
        ValklearGeneral = 9,
    }
}