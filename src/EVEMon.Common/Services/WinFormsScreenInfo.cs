using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// WinForms implementation of <see cref="IScreenInfo"/>.
    /// Wraps <see cref="Screen.PrimaryScreen"/> and <see cref="Screen.AllScreens"/>.
    /// </summary>
    internal sealed class WinFormsScreenInfo : IScreenInfo
    {
        public (int X, int Y, int Width, int Height) PrimaryWorkingArea
        {
            get
            {
                var area = Screen.PrimaryScreen?.WorkingArea
                    ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
                return (area.X, area.Y, area.Width, area.Height);
            }
        }

        public IReadOnlyList<(int X, int Y, int Width, int Height)> AllScreenBounds
        {
            get
            {
                return Screen.AllScreens
                    .Select(s => (s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height))
                    .ToList()
                    .AsReadOnly();
            }
        }
    }
}
