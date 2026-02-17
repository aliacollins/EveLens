namespace EVEMon.Core.Enumerations
{
    /// <summary>
    /// Platform-agnostic dialog result choices.
    /// Maps to WinForms DialogResult, Avalonia equivalents, or test stubs.
    /// </summary>
    public enum DialogChoice
    {
        OK,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No
    }

    /// <summary>
    /// Platform-agnostic dialog button combinations.
    /// Maps to WinForms MessageBoxButtons or Avalonia equivalents.
    /// </summary>
    public enum DialogButtons
    {
        OK,
        OKCancel,
        AbortRetryIgnore,
        YesNoCancel,
        YesNo,
        RetryCancel
    }

    /// <summary>
    /// Platform-agnostic dialog icon types.
    /// Maps to WinForms MessageBoxIcon or Avalonia equivalents.
    /// </summary>
    public enum DialogIcon
    {
        None,
        Error,
        Warning,
        Information,
        Question
    }
}
