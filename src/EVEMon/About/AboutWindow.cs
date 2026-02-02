using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Helpers;

namespace EVEMon.About
{
    /// <summary>
    /// Modern About Window with card-based UI and theme support.
    /// </summary>
    public partial class AboutWindow : EVEMonForm
    {
        // Dark theme colors (EVE-inspired)
        private static readonly Color DarkBackground = Color.FromArgb(23, 26, 33);
        private static readonly Color DarkCard = Color.FromArgb(35, 39, 49);
        private static readonly Color DarkBorder = Color.FromArgb(50, 55, 65);
        private static readonly Color DarkAccent = Color.FromArgb(232, 181, 79); // EVE gold
        private static readonly Color DarkText = Color.FromArgb(230, 230, 230);
        private static readonly Color DarkSubtext = Color.FromArgb(160, 160, 170);
        private static readonly Color DarkLink = Color.FromArgb(100, 180, 255);
        private static readonly Color DarkListBg = Color.FromArgb(28, 31, 38);

        // Light theme colors
        private static readonly Color LightBackground = Color.FromArgb(245, 245, 248);
        private static readonly Color LightCard = Color.White;
        private static readonly Color LightBorder = Color.FromArgb(220, 220, 225);
        private static readonly Color LightAccent = Color.FromArgb(180, 130, 50);
        private static readonly Color LightText = Color.FromArgb(30, 30, 35);
        private static readonly Color LightSubtext = Color.FromArgb(100, 100, 110);
        private static readonly Color LightLink = Color.FromArgb(0, 100, 180);
        private static readonly Color LightListBg = Color.FromArgb(250, 250, 252);

        // Current theme
        private bool _isDarkMode = true;
        private Color _bgColor, _cardColor, _borderColor, _accentColor;
        private Color _textColor, _subtextColor, _linkColor, _listBgColor;

        // Animation state
        private bool _isAnimating = false;
        private Timer _fadeTimer;

        // Column width for cards (calculated at runtime)
        private int _columnWidth = 300;

        public AboutWindow()
        {
            InitializeComponent();
            ApplyTheme();
            SetupModernUI();
        }

        private void ApplyTheme()
        {
            if (_isDarkMode)
            {
                _bgColor = DarkBackground;
                _cardColor = DarkCard;
                _borderColor = DarkBorder;
                _accentColor = DarkAccent;
                _textColor = DarkText;
                _subtextColor = DarkSubtext;
                _linkColor = DarkLink;
                _listBgColor = DarkListBg;
            }
            else
            {
                _bgColor = LightBackground;
                _cardColor = LightCard;
                _borderColor = LightBorder;
                _accentColor = LightAccent;
                _textColor = LightText;
                _subtextColor = LightSubtext;
                _linkColor = LightLink;
                _listBgColor = LightListBg;
            }
        }

        private void ToggleTheme()
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme();

            // Rebuild UI with new theme
            this.Controls.Clear();
            SetupModernUI();
        }

        private void ToggleThemeWithFade()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            // Use form opacity for smooth transition
            int step = 0;
            _fadeTimer = new Timer { Interval = 15 };
            _fadeTimer.Tick += (s, e) =>
            {
                step++;
                if (step <= 10)
                {
                    // Fade out
                    this.Opacity = 1.0 - (step * 0.1);
                }
                else if (step == 11)
                {
                    // Switch theme at minimum opacity
                    ToggleTheme();
                }
                else if (step <= 21)
                {
                    // Fade in
                    this.Opacity = (step - 11) * 0.1;
                }
                else
                {
                    // Done
                    this.Opacity = 1.0;
                    _fadeTimer.Stop();
                    _fadeTimer.Dispose();
                    _fadeTimer = null;
                    _isAnimating = false;
                }
            };
            _fadeTimer.Start();
        }

        private void SetupModernUI()
        {
            // Form setup
            this.Text = "About EVEMon";
            this.BackColor = _bgColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(900, 520);

            // Calculate column width
            _columnWidth = (900 - 60) / 3;  // ~280px per column

            // Main 3-column layout
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(15),
                BackColor = _bgColor,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Column 1 - App info (single full-height card)
            var col1 = CreateInfoColumn();

            // Column 2 - Credits (single full-height card)
            var col2 = CreateCreditsColumn();

            // Column 3 - Contributors (single full-height card)
            var col3 = CreateContributorsPanel();

            mainTable.Controls.Add(col1, 0, 0);
            mainTable.Controls.Add(col2, 1, 0);
            mainTable.Controls.Add(col3, 2, 0);

            this.Controls.Add(mainTable);
        }

        private Panel CreateInfoColumn()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _cardColor,
                Margin = new Padding(4)
            };

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(_borderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            int y = 15;

            // EVEMon title
            panel.Controls.Add(new Label
            {
                Text = "EVEMon",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 35;

            // Version
            var version = EveMonClient.FileVersionInfo;
            var versionText = EveMonClient.IsDebugBuild ? $"{version.FileVersion} (Debug)" : version.ProductVersion;
            panel.Controls.Add(new Label
            {
                Text = $"Version {versionText}  |  {(Environment.Is64BitProcess ? "64" : "32")}-bit",
                Font = new Font("Segoe UI", 8),
                ForeColor = _subtextColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 25;

            // Description
            panel.Controls.Add(new Label
            {
                Text = "EVE Online Character Monitor\n& Skill Planner",
                Font = new Font("Segoe UI", 9),
                ForeColor = _textColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 40;

            // GitHub link
            var githubLink = CreateLinkLabel("github.com/aliacollins/evemon", 15, y);
            githubLink.Click += (s, e) => Util.OpenURL(new Uri("https://github.com/aliacollins/evemon"));
            panel.Controls.Add(githubLink);
            y += 30;

            // Divider line
            AddDivider(panel, y);
            y += 15;

            // Maintainer section
            panel.Controls.Add(new Label
            {
                Text = "MAINTAINER",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 20;

            panel.Controls.Add(new Label
            {
                Text = "Alia Collins",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = _textColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 25;

            panel.Controls.Add(new Label
            {
                Text = "Active Developer",
                Font = new Font("Segoe UI", 8),
                ForeColor = _subtextColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 25;

            // Divider line
            AddDivider(panel, y);
            y += 15;

            // Donation note
            panel.Controls.Add(new Label
            {
                Text = "SUPPORT EVEMon",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 20;

            panel.Controls.Add(new Label
            {
                Text = "I don't accept donations.\nPlease donate to Peter Han\nor the original EVEMonDevTeam\nwho built this tool.",
                Font = new Font("Segoe UI", 8),
                ForeColor = _subtextColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 60;

            // Divider line
            AddDivider(panel, y);
            y += 15;

            // Theme section (no trailing divider)
            panel.Controls.Add(new Label
            {
                Text = "THEME",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 20;

            var themeBtn = new Button
            {
                Text = _isDarkMode ? "\u263D  Light Mode" : "\u2600  Dark Mode",
                Font = new Font("Segoe UI", 9),
                ForeColor = _textColor,
                BackColor = _borderColor,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(15, y),
                Size = new Size(120, 28),
                Cursor = Cursors.Hand
            };
            themeBtn.FlatAppearance.BorderColor = _borderColor;
            themeBtn.FlatAppearance.MouseOverBackColor = _accentColor;
            themeBtn.Click += (s, e) => ToggleThemeWithFade();
            panel.Controls.Add(themeBtn);

            return panel;
        }

        private void AddDivider(Panel parent, int y)
        {
            var divider = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(parent.Width > 0 ? parent.Width - 30 : 240, 1),
                BackColor = _borderColor,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            parent.Controls.Add(divider);
        }

        private Panel CreateCreditsColumn()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _cardColor,
                Margin = new Padding(4)
            };

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(_borderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            int y = 15;

            // AI section
            panel.Controls.Add(new Label
            {
                Text = "BUILT WITH AI",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 20;

            panel.Controls.Add(new Label
            {
                Text = "Continued maintenance is made\npossible through AI-assisted\ndevelopment tools.",
                Font = new Font("Segoe UI", 9),
                ForeColor = _subtextColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 55;

            // Divider
            AddDivider(panel, y);
            y += 15;

            // External APIs section
            panel.Controls.Add(new Label
            {
                Text = "EXTERNAL APIs",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 20;

            var apis = new[] { "CCP Games - ESI API", "EVEMarketer - Market Data", "Fuzzwork - Static Data", "Stack Overflow - Solutions" };
            foreach (var api in apis)
            {
                panel.Controls.Add(new Label
                {
                    Text = api,
                    Font = new Font("Segoe UI", 8),
                    ForeColor = _subtextColor,
                    Location = new Point(15, y),
                    AutoSize = true,
                    BackColor = Color.Transparent
                });
                y += 18;
            }
            y += 10;

            // Divider
            AddDivider(panel, y);
            y += 15;

            // Build Tools section
            panel.Controls.Add(new Label
            {
                Text = "BUILD TOOLS",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 20;

            var buildTools = new[] { ".NET 8.0 - Runtime", "Inno Setup 6 - Installer" };
            foreach (var tool in buildTools)
            {
                panel.Controls.Add(new Label
                {
                    Text = tool,
                    Font = new Font("Segoe UI", 8),
                    ForeColor = _subtextColor,
                    Location = new Point(15, y),
                    AutoSize = true,
                    BackColor = Color.Transparent
                });
                y += 18;
            }
            y += 10;

            // Divider
            AddDivider(panel, y);
            y += 15;

            // Roadmap section (no trailing divider)
            panel.Controls.Add(new Label
            {
                Text = "ROADMAP",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            y += 20;

            var roadmap = new[] { "\u2022 UI Modernization", "\u2022 Enhanced Skill Planning", "\u2022 Better ESI Integration" };
            foreach (var item in roadmap)
            {
                panel.Controls.Add(new Label
                {
                    Text = item,
                    Font = new Font("Segoe UI", 8),
                    ForeColor = _subtextColor,
                    Location = new Point(15, y),
                    AutoSize = true,
                    BackColor = Color.Transparent
                });
                y += 18;
            }

            return panel;
        }

        private Panel CreateContributorsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _cardColor,
                Margin = new Padding(4)
            };

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(_borderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            panel.Controls.Add(new Label
            {
                Text = "CONTRIBUTORS",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(15, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            panel.Controls.Add(new Label
            {
                Text = "Originally by Six Anari",
                Font = new Font("Segoe UI", 8),
                ForeColor = _subtextColor,
                Location = new Point(15, 33),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // Scrollable contributor list (same background as card for consistency)
            var listPanel = new Panel
            {
                Location = new Point(15, 55),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = _cardColor,
                BorderStyle = BorderStyle.None
            };
            listPanel.Size = new Size(_columnWidth - 35, 390);

            var innerPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = _cardColor,
                Padding = new Padding(0, 0, 15, 5)
            };

            var contributors = GetContributorsList();
            foreach (var category in contributors)
            {
                // Category header
                innerPanel.Controls.Add(new Label
                {
                    Text = category.Key.ToUpperInvariant(),
                    Font = new Font("Segoe UI", 7, FontStyle.Bold),
                    ForeColor = _accentColor,
                    AutoSize = true,
                    Margin = new Padding(0, 8, 0, 3),
                    BackColor = Color.Transparent
                });

                // Names
                foreach (var name in category.Value)
                {
                    innerPanel.Controls.Add(new Label
                    {
                        Text = name,
                        Font = new Font("Segoe UI", 8),
                        ForeColor = _textColor,
                        AutoSize = true,
                        Margin = new Padding(5, 1, 0, 1),
                        BackColor = Color.Transparent
                    });
                }
            }

            listPanel.Controls.Add(innerPanel);
            panel.Controls.Add(listPanel);

            return panel;
        }

        private Dictionary<string, string[]> GetContributorsList()
        {
            return new Dictionary<string, string[]>
            {
                ["Active Developer"] = new[] { "Alia Collins" },
                ["Developers (Retired)"] = new[]
                {
                    "Peter Han", "Blitz Bandis", "Jimi", "Araan Sunn", "Six Anari",
                    "Anders Chydenius", "Brad Stone", "Eewec Ourbyni", "Richard Slater",
                    "Vehlin", "Collin Grady", "DCShadow", "DonQuiche", "Grauw",
                    "Jalon Mevek", "Labogh", "romanl", "Safrax", "Stevil Knevil", "TheBelgarion"
                },
                ["Consultants"] = new[] { "MrCue", "Nericus Demeeny", "Tonto Auri" },
                ["Contributors"] = new[]
                {
                    "Abomb", "Adam Butt", "Aethlyn", "Aevum Decessus", "aliceturing",
                    "aMUSiC", "Arengor", "ATGardner", "Barend", "berin", "bugusnot",
                    "Candle", "coeus", "CrazyMahone", "CyberTech", "Derath Ellecon",
                    "Dariana", "Eviro", "exi", "FangVV", "Femaref", "Flash",
                    "Galideeth", "gareth", "gavinl", "GoneWacko", "Good speed",
                    "happyslinky", "Innocent Enemy", "Jazzy_Josh", "jdread",
                    "Jeff Zellner", "jthiesen", "justinian", "Kelos Pelmand",
                    "Kingdud", "Kw4h", "Kunnis Niam", "lerthe61", "Lexiica",
                    "Master of Dice", "Maximilian Kernbach", "MaZ", "mexx24",
                    "Michayel Lyon", "mintoko", "misterilla", "Moq", "morgangreenacre",
                    "Namistai", "Nascent Nimbus", "NetMage", "Nagapito", "Nilyen",
                    "Nimrel", "Niom", "Pharazon", "Phoenix Flames", "phorge", "Protag",
                    "Optica", "Quantix Blackstar", "Risako", "Ruldar", "Safarian Lanar",
                    "scoobyrich", "Sertan Deras", "shaver", "Shocky", "Shwehan Juanis",
                    "skolima", "Spiff Nutter", "stiez", "Subkahnshus", "SyndicateAexeron",
                    "The_Assimilator", "TheConstructor", "Travis Puderbaugh", "Trin",
                    "vardoj", "Waste Land", "wrok", "xNomeda", "ykoehler", "Zarra Kri", "Zofu"
                }
            };
        }

        private LinkLabel CreateLinkLabel(string text, int x, int y)
        {
            return new LinkLabel
            {
                Text = text,
                Font = new Font("Segoe UI", 9),
                LinkColor = _linkColor,
                ActiveLinkColor = _accentColor,
                VisitedLinkColor = _linkColor,
                Location = new Point(x, y),
                AutoSize = true,
                BackColor = Color.Transparent,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
        }
    }
}
