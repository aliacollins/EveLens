namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Platform-agnostic application lifecycle control.
    /// Replaces direct calls to <c>System.Windows.Forms.Application.Exit()</c>
    /// and <c>Application.Restart()</c>.
    /// </summary>
    /// <remarks>
    /// Production (WinForms): <c>WinFormsApplicationLifecycle</c> in <c>EVEMon.Common</c>.
    /// Production (Avalonia): Will delegate to Avalonia application shutdown.
    /// Testing: Record calls without actually exiting.
    /// </remarks>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Exits the application gracefully.
        /// </summary>
        void Exit();

        /// <summary>
        /// Restarts the application.
        /// </summary>
        void Restart();
    }
}
