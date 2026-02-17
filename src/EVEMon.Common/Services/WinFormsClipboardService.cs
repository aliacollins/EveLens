using System.Windows.Forms;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// WinForms implementation of <see cref="IClipboardService"/>.
    /// Wraps <see cref="System.Windows.Forms.Clipboard"/>.
    /// </summary>
    internal sealed class WinFormsClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            Clipboard.SetText(text, TextDataFormat.Text);
        }

        public string? GetText()
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
    }
}
