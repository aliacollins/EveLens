namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Discriminator for display items in the segmented plan list.
    /// Used to select the correct DataTemplate in the ItemsControl.
    /// </summary>
    internal enum PlanDisplayItemKind
    {
        SkillEntry,
        SectionHeader,
        RemapDivider
    }

    /// <summary>
    /// Marker interface for all display items in the plan list
    /// (PlanEntryDisplayItem, PlanSectionHeader, PlanRemapDivider).
    /// </summary>
    internal interface IPlanDisplayItem
    {
        PlanDisplayItemKind Kind { get; }
    }
}
