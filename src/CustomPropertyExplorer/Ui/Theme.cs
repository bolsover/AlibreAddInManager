using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomPropertyExplorer.Ui
{
    /// <summary>
    /// The scripts' dark colour scheme and DPI helper, shared by every window.
    /// Pixel sizes are written at 96 DPI and scaled with <see cref="Scale"/>;
    /// fonts are given in points, which Windows already scales, so they are not.
    /// </summary>
    internal static class Theme
    {
        public static readonly Color Primary = Color.FromArgb(45, 45, 48);
        public static readonly Color Secondary = Color.FromArgb(62, 62, 66);
        public static readonly Color Accent = Color.FromArgb(0, 122, 204);
        public static readonly Color Surface = Color.FromArgb(37, 37, 38);
        public static readonly Color TextPrimary = Color.FromArgb(241, 241, 241);
        public static readonly Color TextSecondary = Color.FromArgb(204, 204, 204);
        public static readonly Color Border = Color.FromArgb(63, 63, 70);
        public static readonly Color Success = Color.FromArgb(73, 156, 84);
        public static readonly Color Warning = Color.FromArgb(252, 196, 25);
        public static readonly Color Error = Color.FromArgb(255, 100, 100);

        private static readonly Lazy<float> DpiScale = new Lazy<float>(() =>
        {
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                    return g.DpiX / 96f;
            }
            catch (Exception)
            {
                return 1f;
            }
        });

        /// <summary>A 96-DPI pixel size, scaled to the screen.</summary>
        public static int Scale(int pixels) => (int) Math.Round(pixels * DpiScale.Value);

        public static Padding Scale(int left, int top, int right, int bottom) =>
            new Padding(Scale(left), Scale(top), Scale(right), Scale(bottom));

        public static Font UiFont(float points = 9f, FontStyle style = FontStyle.Regular) =>
            new Font("Segoe UI", points, style);

        /// <summary>Applies the form-level colours and font; layout is scaled by hand.</summary>
        public static void ApplyTo(Form form)
        {
            form.AutoScaleMode = AutoScaleMode.None;
            form.BackColor = Primary;
            form.ForeColor = TextPrimary;
            form.Font = UiFont();
        }

        public static StatusStrip StatusStrip(out ToolStripStatusLabel status, out ToolStripStatusLabel selection)
        {
            var strip = new StatusStrip
            {
                BackColor = Surface,
                ForeColor = TextSecondary,
                Padding = Scale(12, 6, 12, 6),
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
            };
            status = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextSecondary,
                Margin = Scale(8, 3, 8, 3)
            };
            selection = new ToolStripStatusLabel
            {
                AutoSize = true,
                ForeColor = TextSecondary,
                Margin = Scale(8, 3, 8, 3)
            };
            strip.Items.AddRange(new ToolStripItem[] { status, selection });
            return strip;
        }

        public static SplitContainer Splitter()
        {
            return new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = Border,
                SplitterWidth = Scale(8)
            };
        }

        public static Panel Panel(Padding padding)
        {
            return new Panel { Dock = DockStyle.Fill, BackColor = Secondary, Padding = padding };
        }

        public static Label Header(string text)
        {
            return new Label
            {
                Text = text,
                Font = UiFont(10f, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Surface,
                Height = Scale(40),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = Scale(16, 0, 0, 0),
                Dock = DockStyle.Top
            };
        }

        public static ToolStrip ToolStrip()
        {
            return new ToolStrip
            {
                BackColor = Surface,
                ForeColor = TextPrimary,
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()),
                GripStyle = ToolStripGripStyle.Hidden,
                Padding = Scale(10, 6, 10, 6),
                AutoSize = false,
                Height = Scale(48),
                LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
                Dock = DockStyle.Top
            };
        }

        public static ToolStripButton ToolButton(string text, string toolTip, Color foreColor)
        {
            return new ToolStripButton(text)
            {
                ForeColor = foreColor,
                ToolTipText = toolTip,
                AutoSize = true,
                Margin = Scale(6, 3, 6, 3),
                Padding = Scale(8, 4, 8, 4)
            };
        }

        public static ToolStripSeparator ToolSeparator()
        {
            return new ToolStripSeparator { Margin = Scale(10, 0, 10, 0) };
        }

        public static TreeView TreeView()
        {
            return new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Primary,
                ForeColor = TextPrimary,
                LineColor = Border,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                HideSelection = false,
                FullRowSelect = true,
                BorderStyle = BorderStyle.None,
                Indent = Scale(20),
                ItemHeight = Scale(24)
            };
        }

        public static PropertyGrid PropertyGrid()
        {
            return new PropertyGrid
            {
                Dock = DockStyle.Fill,
                BackColor = Primary,
                LineColor = Border,
                CategoryForeColor = TextPrimary,
                CategorySplitterColor = Border,
                HelpBackColor = Surface,
                HelpForeColor = TextSecondary,
                ViewBackColor = Primary,
                ViewForeColor = TextPrimary,
                // Read-only entries are drawn in this colour; the default grey is unreadable on dark.
                DisabledItemForeColor = TextSecondary,
                CommandsBackColor = Surface,
                CommandsForeColor = TextPrimary,
                ToolbarVisible = true,
                PropertySort = PropertySort.Categorized,
                HelpVisible = true
            };
        }

        public static Button Button(string text, Color backColor)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(Scale(100), Scale(32)),
                BackColor = backColor,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderColor = Border;
            return button;
        }

        public static TextBox TextBox()
        {
            return new TextBox
            {
                BackColor = Surface,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        /// <summary>Dark backgrounds for the tool and status strips' professional renderer.</summary>
        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => Surface;
            public override Color ToolStripGradientMiddle => Surface;
            public override Color ToolStripGradientEnd => Surface;
            public override Color ToolStripBorder => Surface;
            public override Color StatusStripGradientBegin => Surface;
            public override Color StatusStripGradientEnd => Surface;
            public override Color ButtonSelectedHighlight => Secondary;
            public override Color ButtonSelectedGradientBegin => Secondary;
            public override Color ButtonSelectedGradientMiddle => Secondary;
            public override Color ButtonSelectedGradientEnd => Secondary;
            public override Color ButtonSelectedBorder => Accent;
            public override Color ButtonPressedGradientBegin => Border;
            public override Color ButtonPressedGradientMiddle => Border;
            public override Color ButtonPressedGradientEnd => Border;
            public override Color ButtonCheckedGradientBegin => Border;
            public override Color ButtonCheckedGradientMiddle => Border;
            public override Color ButtonCheckedGradientEnd => Border;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Border;
            public override Color OverflowButtonGradientBegin => Surface;
            public override Color OverflowButtonGradientMiddle => Surface;
            public override Color OverflowButtonGradientEnd => Surface;
        }
    }
}
