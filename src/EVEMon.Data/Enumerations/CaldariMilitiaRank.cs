using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of Caldari Militia ranking.
    /// </summary>
    public enum CaldariMilitiaRank
    {
        [Description("Protectorate Ensign")]
        ProtectorateEnsign = 0,

        [Description("Second Lieutenant")]
        SecondLieutenant = 1,

        [Description("First Lieutenant")]
        FirstLieutenant = 2,

        [Description("Captain")]
        Captain = 3,

        [Description("Major")]
        Major = 4,

        [Description("Lieutenant Colonel")]
        LieutenantColonel = 5,

        [Description("Colonel")]
        Colonel = 6,

        [Description("Major General")]
        MajorGeneral = 7,

        [Description("Lieutenant General")]
        LieutenantGeneral = 8,

        [Description("Brigadier General")]
        BrigadierGeneral = 9,
    }
}