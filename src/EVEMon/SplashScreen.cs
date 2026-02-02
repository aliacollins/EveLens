using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EVEMon
{
    /// <summary>
    /// Splash screen displayed during application startup while loading settings and data.
    /// Provides visual feedback about loading progress.
    /// </summary>
    public partial class SplashScreen : Form
    {
        // EVE-inspired dark theme colors
        private static readonly Color BackgroundColor = Color.FromArgb(23, 26, 33);
        private static readonly Color AccentColor = Color.FromArgb(232, 181, 79); // EVE gold
        private static readonly Color TextColor = Color.FromArgb(230, 230, 230);
        private static readonly Color SubtextColor = Color.FromArgb(160, 160, 170);
        private static readonly Color ProgressBackColor = Color.FromArgb(35, 39, 49);
        private static readonly Color BorderColor = Color.FromArgb(50, 55, 65);

        private int _progressValue;
        private string _statusText = "Initializing...";

        /// <summary>
        /// Creates a new splash screen instance.
        /// </summary>
        public SplashScreen()
        {
            InitializeComponent();
            SetupUI();
        }

        /// <summary>
        /// Sets up the splash screen UI elements.
        /// </summary>
        private void SetupUI()
        {
            // Form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 200);
            this.BackColor = BackgroundColor;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;

            // Enable custom painting
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>
        /// Custom paint handler for the splash screen.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw border
            using (var borderPen = new Pen(BorderColor, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Draw EVEMon title
            using (var titleFont = new Font("Segoe UI", 24, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(AccentColor))
            {
                g.DrawString("EVEMon", titleFont, titleBrush, 30, 30);
            }

            // Draw subtitle
            using (var subtitleFont = new Font("Segoe UI", 10))
            using (var subtitleBrush = new SolidBrush(SubtextColor))
            {
                g.DrawString("EVE Online Character Monitor", subtitleFont, subtitleBrush, 32, 70);
            }

            // Draw progress bar background
            int progressY = 120;
            int progressHeight = 8;
            int progressMargin = 30;
            Rectangle progressBgRect = new Rectangle(progressMargin, progressY, Width - (progressMargin * 2), progressHeight);

            using (var bgBrush = new SolidBrush(ProgressBackColor))
            {
                FillRoundedRectangle(g, bgBrush, progressBgRect, 4);
            }

            // Draw progress bar fill
            if (_progressValue > 0)
            {
                int fillWidth = (int)((progressBgRect.Width - 2) * (_progressValue / 100.0));
                if (fillWidth > 0)
                {
                    Rectangle progressFillRect = new Rectangle(progressBgRect.X + 1, progressBgRect.Y + 1,
                        fillWidth, progressBgRect.Height - 2);
                    using (var fillBrush = new SolidBrush(AccentColor))
                    {
                        FillRoundedRectangle(g, fillBrush, progressFillRect, 3);
                    }
                }
            }

            // Draw status text
            using (var statusFont = new Font("Segoe UI", 9))
            using (var statusBrush = new SolidBrush(TextColor))
            {
                g.DrawString(_statusText, statusFont, statusBrush, progressMargin, progressY + progressHeight + 12);
            }

            // Draw version info in bottom right
            using (var versionFont = new Font("Segoe UI", 8))
            using (var versionBrush = new SolidBrush(SubtextColor))
            {
                string versionText = "Loading...";
                SizeF versionSize = g.MeasureString(versionText, versionFont);
                g.DrawString(versionText, versionFont, versionBrush,
                    Width - versionSize.Width - 15, Height - versionSize.Height - 10);
            }
        }

        /// <summary>
        /// Fills a rounded rectangle.
        /// </summary>
        private void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            using (GraphicsPath path = new GraphicsPath())
            {
                int diameter = radius * 2;
                Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

                // Top left arc
                path.AddArc(arc, 180, 90);

                // Top right arc
                arc.X = rect.Right - diameter;
                path.AddArc(arc, 270, 90);

                // Bottom right arc
                arc.Y = rect.Bottom - diameter;
                path.AddArc(arc, 0, 90);

                // Bottom left arc
                arc.X = rect.Left;
                path.AddArc(arc, 90, 90);

                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// Updates the progress bar and status text.
        /// </summary>
        /// <param name="progress">Progress value (0-100)</param>
        /// <param name="status">Status text to display</param>
        public void UpdateProgress(int progress, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateProgress(progress, status)));
                return;
            }

            _progressValue = Math.Max(0, Math.Min(100, progress));
            _statusText = status ?? string.Empty;
            Invalidate();
            Application.DoEvents(); // Process paint message immediately
        }

        /// <summary>
        /// Updates only the status text without changing progress.
        /// </summary>
        /// <param name="status">Status text to display</param>
        public void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateStatus(status)));
                return;
            }

            _statusText = status ?? string.Empty;
            Invalidate();
            Application.DoEvents();
        }

        /// <summary>
        /// Closes the splash screen with a fade-out effect.
        /// </summary>
        public void FadeOut()
        {
            Timer fadeTimer = new Timer { Interval = 20 };
            fadeTimer.Tick += (s, e) =>
            {
                if (Opacity > 0.1)
                {
                    Opacity -= 0.1;
                }
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    Close();
                }
            };
            fadeTimer.Start();
        }
    }
}
