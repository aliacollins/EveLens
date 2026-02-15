using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of Agent type.
    /// </summary>
    public enum AgentType
    {
        [Description]
        NonAgent,

        [Description("Agents")]
        BasicAgent,

        [Description("Tutorial")]
        TutorialAgent,

        [Description("Research")]
        ResearchAgent,

        [Description("CONCORD")]
        CONCORDAgent,

        [Description("Storyline")]
        GenericStorylineMissionAgent,

        [Description("Stolyline")]
        StorylineMissionAgent,

        [Description("Event")]
        EventMissionAgent,

        [Description("Factional Warfare")]
        FactionalWarfareAgent,

        [Description("Epic")]
        EpicArcAgent,

        [Description("Aura")]
        AuraAgent,

        [Description("Career")]
        CareerAgent
    }
}