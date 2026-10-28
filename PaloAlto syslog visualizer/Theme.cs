using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PaloAlto_syslog_visualizer
{
    // Color palette of the UI, one instance for the light theme and one for the dark theme
    public sealed class Theme
    {
        public bool IsDark;
        public Color Window;        // form background
        public Color Surface;       // toolbar and status bar
        public Color Input;         // text boxes
        public Color Border;
        public Color Text;
        public Color TextMuted;
        public Color Accent;        // primary button
        public Color AccentHover;
        public Color AccentText;
        public Color Button;
        public Color ButtonHover;
        public Color GridBack;
        public Color GridAltRow;
        public Color GridHeader;
        public Color GridLine;
        public Color Selection;
        public Color SelectionText;
        public Color Allow;
        public Color Deny;

        public static readonly Theme Light = new Theme
        {
            IsDark = false,
            Window = Color.FromArgb(243, 243, 243),
            Surface = Color.FromArgb(251, 251, 251),
            Input = Color.White,
            Border = Color.FromArgb(209, 209, 209),
            Text = Color.FromArgb(27, 27, 27),
            TextMuted = Color.FromArgb(96, 96, 96),
            Accent = Color.FromArgb(0, 103, 192),
            AccentHover = Color.FromArgb(25, 117, 197),
            AccentText = Color.White,
            Button = Color.White,
            ButtonHover = Color.FromArgb(240, 240, 240),
            GridBack = Color.White,
            GridAltRow = Color.FromArgb(247, 249, 252),
            GridHeader = Color.FromArgb(243, 243, 243),
            GridLine = Color.FromArgb(229, 229, 229),
            Selection = Color.FromArgb(204, 228, 247),
            SelectionText = Color.FromArgb(27, 27, 27),
            Allow = Color.FromArgb(15, 123, 15),
            Deny = Color.FromArgb(196, 43, 28),
        };

        public static readonly Theme Dark = new Theme
        {
            IsDark = true,
            Window = Color.FromArgb(32, 32, 32),
            Surface = Color.FromArgb(39, 39, 39),
            Input = Color.FromArgb(45, 45, 45),
            Border = Color.FromArgb(70, 70, 70),
            Text = Color.FromArgb(243, 243, 243),
            TextMuted = Color.FromArgb(170, 170, 170),
            Accent = Color.FromArgb(76, 194, 255),
            AccentHover = Color.FromArgb(71, 177, 232),
            AccentText = Color.Black,
            Button = Color.FromArgb(55, 55, 55),
            ButtonHover = Color.FromArgb(65, 65, 65),
            GridBack = Color.FromArgb(28, 28, 28),
            GridAltRow = Color.FromArgb(36, 36, 36),
            GridHeader = Color.FromArgb(43, 43, 43),
            GridLine = Color.FromArgb(51, 51, 51),
            Selection = Color.FromArgb(14, 74, 115),
            SelectionText = Color.White,
            Allow = Color.FromArgb(108, 203, 95),
            Deny = Color.FromArgb(255, 153, 164),
        };

        // Windows "Choose your app mode" setting
        public static bool SystemUsesDarkMode()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    // Applies a Theme to a form and all its child controls
    public static class ThemeManager
    {
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

        const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // Windows 10 before 20H1
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_FRAMECHANGED = 0x20;

        public static void Apply(Form form, Theme theme)
        {
            form.BackColor = theme.Window;
            form.ForeColor = theme.Text;
            ApplyToChildren(form, theme);
            SetDarkTitleBar(form, theme.IsDark);
        }

        static void ApplyToChildren(Control parent, Theme theme)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case DataGridView grid:
                        ApplyToGrid(grid, theme);
                        break;
                    case Button button when button.Tag as string == "icon":
                        ApplyToIconButton(button, theme);
                        break;
                    case Button button:
                        ApplyToButton(button, theme, button.Tag as string == "primary");
                        break;
                    case TextBox textBox:
                        textBox.BackColor = theme.Input;
                        textBox.ForeColor = theme.Text;
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case CheckBox checkBox:
                        ApplyToCheckBox(checkBox, theme);
                        break;
                    case Label label:
                        label.ForeColor = theme.TextMuted;
                        // the hint is drawn over the empty grid
                        label.BackColor = label.Tag as string == "hint" ? theme.GridBack : Color.Transparent;
                        break;
                    case StatusStrip statusStrip:
                        statusStrip.Renderer = new FlatToolStripRenderer(theme);
                        statusStrip.BackColor = theme.Surface;
                        statusStrip.ForeColor = theme.TextMuted;
                        foreach (ToolStripItem item in statusStrip.Items)
                            item.ForeColor = theme.TextMuted;
                        break;
                    case Panel panel:
                        panel.BackColor = panel.Tag as string == "toolbar" ? theme.Surface : Color.Transparent;
                        break;
                }
                ApplyToChildren(control, theme);
            }
        }

        // shown as a small toggle button, filled with the accent color when checked
        public static void ApplyToCheckBox(CheckBox checkBox, Theme theme)
        {
            checkBox.Appearance = Appearance.Button;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.TextAlign = ContentAlignment.MiddleCenter;
            checkBox.FlatAppearance.BorderColor = theme.Border;
            checkBox.FlatAppearance.CheckedBackColor = theme.Accent;
            checkBox.FlatAppearance.MouseOverBackColor = theme.ButtonHover;
            checkBox.BackColor = theme.Input;
            checkBox.ForeColor = checkBox.Checked ? theme.AccentText : theme.Text;
            checkBox.Cursor = Cursors.Hand;
        }

        static void ApplyToButton(Button button, Theme theme, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = theme.Border;
            button.BackColor = primary ? theme.Accent : theme.Button;
            button.ForeColor = primary ? theme.AccentText : theme.Text;
            button.FlatAppearance.MouseOverBackColor = primary ? theme.AccentHover : theme.ButtonHover;
            button.FlatAppearance.MouseDownBackColor = primary ? theme.Accent : theme.Border;
            button.Cursor = Cursors.Hand;
        }

        // borderless glyph button that only shows a background on hover
        static void ApplyToIconButton(Button button, Theme theme)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = theme.Window;
            button.ForeColor = theme.TextMuted;
            button.FlatAppearance.MouseOverBackColor = theme.ButtonHover;
            button.FlatAppearance.MouseDownBackColor = theme.Border;
            button.Cursor = Cursors.Hand;
        }

        // Segoe Fluent Icons on Windows 11, Segoe MDL2 Assets on Windows 10, same code points
        public static Font IconFont(float size)
        {
            foreach (string family in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
            {
                Font font = new Font(family, size);
                if (font.Name == family)
                    return font;
                font.Dispose();
            }
            return null;
        }

        static void ApplyToGrid(DataGridView grid, Theme theme)
        {
            grid.BackgroundColor = theme.GridBack;
            grid.GridColor = theme.GridLine;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.GridHeader;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = theme.TextMuted;

            grid.DefaultCellStyle.BackColor = theme.GridBack;
            grid.DefaultCellStyle.ForeColor = theme.Text;
            grid.DefaultCellStyle.SelectionBackColor = theme.Selection;
            grid.DefaultCellStyle.SelectionForeColor = theme.SelectionText;
            grid.AlternatingRowsDefaultCellStyle.BackColor = theme.GridAltRow;

            ApplyToScrollBars(grid, theme);
            grid.Invalidate();
        }

        // dark scroll bars, Windows ignores the theme name when it doesn't support it
        public static void ApplyToScrollBars(DataGridView grid, Theme theme)
        {
            foreach (Control scrollBar in grid.Controls)
                if (scrollBar is ScrollBar && scrollBar.IsHandleCreated)
                    SetWindowTheme(scrollBar.Handle, theme.IsDark ? "DarkMode_Explorer" : "Explorer", null);
        }

        static void SetDarkTitleBar(Form form, bool dark)
        {
            if (!form.IsHandleCreated)
                return;
            int value = dark ? 1 : 0;
            if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, sizeof(int));
            // repaint the title bar now instead of on the next activation
            SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        // menus such as the column picker
        public static void ApplyToMenu(ToolStripDropDown menu, Theme theme)
        {
            menu.Renderer = new FlatToolStripRenderer(theme);
            menu.BackColor = theme.Surface;
            foreach (ToolStripItem item in menu.Items)
                item.ForeColor = theme.Text;
        }

        // Flat status bar and menus in the theme colors, without the gradients of the default renderer
        sealed class FlatToolStripRenderer : ToolStripProfessionalRenderer
        {
            readonly Theme theme;

            public FlatToolStripRenderer(Theme theme) : base(new ThemeColorTable(theme))
            {
                this.theme = theme;
                RoundedEdges = false;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                if (!(e.ToolStrip is StatusStrip))
                {
                    base.OnRenderToolStripBackground(e);
                    return;
                }
                using (SolidBrush brush = new SolidBrush(theme.Surface))
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                using (Pen pen = new Pen(theme.Border))
                    e.Graphics.DrawLine(pen, 0, 0, e.AffectedBounds.Width, 0);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (!(e.ToolStrip is ToolStripDropDown))
                    return;
                using (Pen pen = new Pen(theme.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = theme.Text;
                base.OnRenderItemText(e);
            }

            // the default check mark is black, invisible in the dark theme
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                Rectangle box = e.ImageRectangle;
                float unit = box.Height / 16f;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(theme.Accent, 2f * unit))
                    e.Graphics.DrawLines(pen, new[] {
                        new PointF(box.X + 3 * unit, box.Y + 8.5f * unit),
                        new PointF(box.X + 6.5f * unit, box.Y + 12 * unit),
                        new PointF(box.X + 13 * unit, box.Y + 4.5f * unit) });
            }

            protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
            {
            }
        }

        // colors of the menus: theme background, hover like the buttons, no image margin gradient
        sealed class ThemeColorTable : ProfessionalColorTable
        {
            readonly Theme theme;

            public ThemeColorTable(Theme theme)
            {
                this.theme = theme;
                UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground => theme.Surface;
            public override Color ImageMarginGradientBegin => theme.Surface;
            public override Color ImageMarginGradientMiddle => theme.Surface;
            public override Color ImageMarginGradientEnd => theme.Surface;
            public override Color MenuBorder => theme.Border;
            public override Color MenuItemBorder => theme.ButtonHover;
            public override Color MenuItemSelected => theme.ButtonHover;
            public override Color MenuItemSelectedGradientBegin => theme.ButtonHover;
            public override Color MenuItemSelectedGradientEnd => theme.ButtonHover;
            public override Color CheckBackground => Color.Transparent;
            public override Color CheckSelectedBackground => Color.Transparent;
            public override Color CheckPressedBackground => Color.Transparent;
            public override Color SeparatorDark => theme.Border;
            public override Color SeparatorLight => theme.Surface;
        }
    }
}
