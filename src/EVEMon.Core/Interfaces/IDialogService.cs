using EVEMon.Core.Enumerations;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Platform-agnostic service for showing dialogs (message boxes, file dialogs).
    /// Replaces direct calls to <c>MessageBox.Show()</c>, <c>SaveFileDialog</c>,
    /// <c>OpenFileDialog</c>, and <c>FolderBrowserDialog</c>.
    /// </summary>
    /// <remarks>
    /// Production (WinForms): <c>WinFormsDialogService</c> in <c>EVEMon.Common</c>.
    /// Production (Avalonia): Will delegate to Avalonia dialog APIs.
    /// Testing: Return canned <see cref="DialogChoice"/> values for deterministic tests.
    /// </remarks>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a message box with the specified text, caption, buttons, and icon.
        /// </summary>
        /// <param name="text">The message text to display.</param>
        /// <param name="caption">The dialog title/caption.</param>
        /// <param name="buttons">The button combination to show.</param>
        /// <param name="icon">The icon to display.</param>
        /// <returns>The user's choice.</returns>
        DialogChoice ShowMessage(string text, string caption,
            DialogButtons buttons = DialogButtons.OK,
            DialogIcon icon = DialogIcon.None);

        /// <summary>
        /// Shows a save file dialog. Returns the selected file path, or null if cancelled.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="filter">The file type filter (e.g. "Text Files (*.txt)|*.txt").</param>
        /// <param name="defaultFileName">Optional default file name.</param>
        /// <param name="initialDirectory">Optional starting directory.</param>
        /// <returns>The selected file path, or null if the user cancelled.</returns>
        string? ShowSaveDialog(string title, string filter,
            string? defaultFileName = null, string? initialDirectory = null);

        /// <summary>
        /// Shows an open file dialog. Returns the selected file path, or null if cancelled.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="filter">The file type filter.</param>
        /// <param name="initialDirectory">Optional starting directory.</param>
        /// <returns>The selected file path, or null if the user cancelled.</returns>
        string? ShowOpenDialog(string title, string filter,
            string? initialDirectory = null);

        /// <summary>
        /// Shows a folder browser dialog. Returns the selected path, or null if cancelled.
        /// </summary>
        /// <param name="description">Description text shown in the dialog.</param>
        /// <param name="selectedPath">Optional initially selected path.</param>
        /// <returns>The selected folder path, or null if the user cancelled.</returns>
        string? ShowFolderBrowser(string description, string? selectedPath = null);
    }
}
