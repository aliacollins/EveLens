using System.Windows.Forms;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// WinForms implementation of <see cref="IApplicationLifecycle"/>.
    /// Wraps <see cref="Application.Exit()"/> and <see cref="Application.Restart()"/>.
    /// </summary>
    internal sealed class WinFormsApplicationLifecycle : IApplicationLifecycle
    {
        public void Exit()
        {
            Application.Exit();
        }

        public void Restart()
        {
            Application.Restart();
        }
    }
}
