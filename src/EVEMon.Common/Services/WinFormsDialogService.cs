using System;
using System.Windows.Forms;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// WinForms implementation of <see cref="IDialogService"/>.
    /// Wraps MessageBox, SaveFileDialog, OpenFileDialog, and FolderBrowserDialog.
    /// </summary>
    internal sealed class WinFormsDialogService : IDialogService
    {
        public DialogChoice ShowMessage(string text, string caption,
            DialogButtons buttons = DialogButtons.OK,
            DialogIcon icon = DialogIcon.None)
        {
            MessageBoxButtons mbButtons = MapButtons(buttons);
            MessageBoxIcon mbIcon = MapIcon(icon);

            DialogResult result = MessageBox.Show(text, caption, mbButtons, mbIcon);
            return MapResult(result);
        }

        public string? ShowSaveDialog(string title, string filter,
            string? defaultFileName = null, string? initialDirectory = null)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = filter;

                if (defaultFileName != null)
                    dialog.FileName = defaultFileName;

                if (initialDirectory != null)
                    dialog.InitialDirectory = initialDirectory;

                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        public string? ShowOpenDialog(string title, string filter,
            string? initialDirectory = null)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = filter;

                if (initialDirectory != null)
                    dialog.InitialDirectory = initialDirectory;

                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        public string? ShowFolderBrowser(string description, string? selectedPath = null)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = description;

                if (selectedPath != null)
                    dialog.SelectedPath = selectedPath;

                return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        private static MessageBoxButtons MapButtons(DialogButtons buttons) => buttons switch
        {
            DialogButtons.OK => MessageBoxButtons.OK,
            DialogButtons.OKCancel => MessageBoxButtons.OKCancel,
            DialogButtons.AbortRetryIgnore => MessageBoxButtons.AbortRetryIgnore,
            DialogButtons.YesNoCancel => MessageBoxButtons.YesNoCancel,
            DialogButtons.YesNo => MessageBoxButtons.YesNo,
            DialogButtons.RetryCancel => MessageBoxButtons.RetryCancel,
            _ => MessageBoxButtons.OK,
        };

        private static MessageBoxIcon MapIcon(DialogIcon icon) => icon switch
        {
            DialogIcon.Error => MessageBoxIcon.Error,
            DialogIcon.Warning => MessageBoxIcon.Warning,
            DialogIcon.Information => MessageBoxIcon.Information,
            DialogIcon.Question => MessageBoxIcon.Question,
            DialogIcon.None => MessageBoxIcon.None,
            _ => MessageBoxIcon.None,
        };

        private static DialogChoice MapResult(DialogResult result) => result switch
        {
            DialogResult.OK => DialogChoice.OK,
            DialogResult.Cancel => DialogChoice.Cancel,
            DialogResult.Abort => DialogChoice.Abort,
            DialogResult.Retry => DialogChoice.Retry,
            DialogResult.Ignore => DialogChoice.Ignore,
            DialogResult.Yes => DialogChoice.Yes,
            DialogResult.No => DialogChoice.No,
            _ => DialogChoice.Cancel,
        };
    }
}
