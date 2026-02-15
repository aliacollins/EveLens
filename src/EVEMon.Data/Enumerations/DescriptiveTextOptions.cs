using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Flags options for the text representation format of a skill.
    /// </summary>
    [Flags]
    public enum DescriptiveTextOptions
    {
        None = 0,
        FullText = 1,
        UppercaseText = 2,
        SpaceText = 4,
        IncludeCommas = 8,
        IncludeZeroes = 16,
        SpaceBetween = 32,
        FirstLetterUppercase = 64
    }
}