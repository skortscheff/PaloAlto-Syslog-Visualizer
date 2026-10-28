using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaloAlto_syslog_visualizer
{
    public partial class FormMain : Form
    {
        Theme theme;
        Font actionFont;
        // each filter text box with its exclude toggle and the log field it searches
        readonly (CheckBox checkBox, TextBox textBox, Func<StructEntryLog, string> field)[] filters;
        readonly ToolTip toolTip = new ToolTip();
        // columns shown in the grid, in log field order
        List<LogField> visibleColumns;
        // logs of the last search, shown again when the columns change
        List<StructEntryLog> lastResults;
        readonly ContextMenuStrip columnsMenu = new ContextMenuStrip();
        // sun and moon glyphs of the icon font, null when neither icon font is installed
        readonly Font iconFont = ThemeManager.IconFont(11F);
        const string glyphSun = "", glyphMoon = "";

        public FormMain()
        {
            // saved choice of the toggle, otherwise follow the Windows app mode
            string savedTheme = ReadSetting(() => Properties.Settings.Default.Theme);
            visibleColumns = ParseColumns(ReadSetting(() => Properties.Settings.Default.Columns));
            if (savedTheme == "Dark")
                theme = Theme.Dark;
            else if (savedTheme == "Light")
                theme = Theme.Light;
            else
                theme = Theme.SystemUsesDarkMode() ? Theme.Dark : Theme.Light;

            InitializeComponent();

            filters = new (CheckBox, TextBox, Func<StructEntryLog, string>)[] {
                (checkBoxSouceAddress, textBoxSouceAddress, entry => entry.strSourceAddress),
                (checkBoxDestinationAddress, textBoxDestinationAddress, entry => entry.strDestinationAddress),
                (checkBoxDestinationPort, textBoxDestinationPort, entry => entry.strDestinationPort),
                (checkBoxAction, textBoxAction, entry => entry.strAction),
                (checkBoxInboundInterface, textBoxInboundInterface, entry => entry.strInboundInterface),
                (checkBoxRuleName, textBoxRuleName, entry => entry.strRuleName) };

            // column picker, from the Columns button or a right click on a column header
            columnsMenu.Opening += (s, e) => BuildColumnsMenu();
            // stay open while columns are checked, like the Monitor tab
            columnsMenu.Closing += (s, e) => { if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) e.Cancel = true; };
            dataGridViewLogs.ColumnHeaderMouseClick += (s, e) => { if (e.Button == MouseButtons.Right) columnsMenu.Show(Cursor.Position); };

            // smoother scrolling and repaint of the grid
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(dataGridViewLogs, true);
            dataGridViewLogs.CellFormatting += dataGridViewLogs_CellFormatting;
            dataGridViewLogs.VirtualMode = true;
            dataGridViewLogs.CellValueNeeded += dataGridViewLogs_CellValueNeeded;

            if (iconFont != null)
                buttonTheme.Font = iconFont;

            foreach ((CheckBox checkBox, TextBox _, var _) in filters)
                toolTip.SetToolTip(checkBox, "Exclude the logs that match this filter");
            toolTip.SetToolTip(buttonColumns, "Choose the columns to show, also with a right click on a column header");
            toolTip.SetToolTip(buttonSearch, "Show every log that matches the filters, newest first (Enter)");
            toolTip.SetToolTip(buttonClean, "Empty the buffer");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // the designer size is taller than many screens once scaled, so the status bar would be hidden
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            if (Width > workingArea.Width || Height > workingArea.Height)
                WindowState = FormWindowState.Maximized;

            foreach ((CheckBox checkBox, TextBox _, var _) in filters)
            {
                checkBox.AutoSize = false;
                checkBox.CheckedChanged += (s, ev) => ThemeManager.ApplyToCheckBox(checkBox, theme);
            }
            ApplyDpiSizes();
            DpiChanged += (s, ev) => BeginInvoke((MethodInvoker)ApplyDpiSizes);

            ApplyTheme();
            // keep the hint centered, Anchor None keeps it there on resize
            lableSearchTip.Location = new Point((panelLogs.Width - lableSearchTip.Width) / 2, (panelLogs.Height - lableSearchTip.Height) / 2);
        }

        // sizes WinForms doesn't scale by itself, set again when the window moves to a monitor with another DPI
        private void ApplyDpiSizes()
        {
            // square exclude toggles next to their text box
            foreach ((CheckBox checkBox, TextBox textBox, var _) in filters)
                checkBox.Bounds = new Rectangle(textBox.Right + LogicalToDeviceUnits(2), textBox.Top, textBox.Height, textBox.Height);

            dataGridViewLogs.RowTemplate.Height = LogicalToDeviceUnits(26);
            dataGridViewLogs.DefaultCellStyle.Padding = new Padding(LogicalToDeviceUnits(4), 0, LogicalToDeviceUnits(4), 0);
            int headerX = LogicalToDeviceUnits(4), headerY = LogicalToDeviceUnits(6);
            dataGridViewLogs.ColumnHeadersDefaultCellStyle.Padding = new Padding(headerX, headerY, headerX, headerY);
        }

        private void ApplyTheme()
        {
            ThemeManager.Apply(this, theme);
            SetCaptureStatus(Program.IsCapturing);

            // the icon shows the theme the button switches to
            string action = theme.IsDark ? "Switch to light mode" : "Switch to dark mode";
            if (iconFont != null)
                buttonTheme.Text = theme.IsDark ? glyphSun : glyphMoon;
            else
                buttonTheme.Text = theme.IsDark ? "☀" : "☾";
            buttonTheme.AccessibleName = action;
            toolTip.SetToolTip(buttonTheme, action);
        }

        // a corrupt user.config (e.g. truncated by a power loss) must not stop the app from starting
        private static string ReadSetting(Func<string> read)
        {
            try
            {
                return read();
            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                // delete it so the next save writes a new one
                string file = (ex.InnerException as System.Configuration.ConfigurationErrorsException)?.Filename ?? ex.Filename;
                try
                {
                    if (!string.IsNullOrEmpty(file))
                        System.IO.File.Delete(file);
                    Properties.Settings.Default.Reload();
                }
                catch (Exception)
                {
                    ;
                }
                return "";
            }
        }

        private void buttonTheme_Click(object sender, EventArgs e)
        {
            theme = theme.IsDark ? Theme.Light : Theme.Dark;
            try
            {
                Properties.Settings.Default.Theme = theme.IsDark ? "Dark" : "Light";
                Properties.Settings.Default.Save();
            }
            catch (System.Configuration.ConfigurationErrorsException)
            {
                ; // the theme still changes, it is just not remembered
            }
            ApplyTheme();
        }

        // color the Action column: allow in green, everything else (deny, drop, reset) in red
        private void dataGridViewLogs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value is string action && dataGridViewLogs.Columns[e.ColumnIndex].Name == "Action")
            {
                e.CellStyle.ForeColor = action.Equals("allow", StringComparison.OrdinalIgnoreCase) ? theme.Allow : theme.Deny;
                if (actionFont == null)
                    actionFont = new Font(dataGridViewLogs.Font, FontStyle.Bold);
                e.CellStyle.Font = actionFont;
            }
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            // only the filters with a text, the ! toggle shows the logs that don't match
            var activeFilters = filters.Where(f => f.textBox.Text != "")
                .Select(f => (text: f.textBox.Text, exclude: f.checkBox.Checked, f.field)).ToArray();

            List<StructEntryLog> found = new List<StructEntryLog>();
            // the parse thread waits while the buffer is scanned, new logs stay in its queue
            lock (Program.databaseLock)
            {
                int size = (int)Program.databaseSize;
                int last = Program.databaseIndexLastItem;
                int count = Program.databaseOverwrite ? size : last + 1;
                // every match, from the newest log back to the oldest one of the circular buffer
                for (int n = 0; n < count; n++)
                {
                    StructEntryLog entry = Program.database[(last - n + size) % size];
                    if (activeFilters.All(f => (f.field(entry).IndexOf(f.text, StringComparison.CurrentCultureIgnoreCase) != -1) != f.exclude))
                        found.Add(entry);
                }
            }

            lastResults = found;
            ShowResults();
        }

        // shows the last search with the selected columns. The grid is in virtual mode: it asks
        // for the cells on screen only, so it can hold the whole buffer
        private void ShowResults()
        {
            if (lastResults == null)
                return;
            lableSearchTip.Visible = false;

            dataGridViewLogs.RowCount = 0;
            dataGridViewLogs.Columns.Clear();
            foreach (LogField column in visibleColumns)
                dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = column.Name,
                    HeaderText = column.Name,
                    Tag = column,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                });
            cachedRow = -1;
            dataGridViewLogs.RowCount = lastResults.Count;

            // fit the columns to the logs on screen, then leave them to the user
            dataGridViewLogs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            toolStripStatusResults.Text = "Search: " + lastResults.Count.ToString("N0") + " logs";

            // the scroll bars are created with the first data, theme them too
            ThemeManager.ApplyToScrollBars(dataGridViewLogs, theme);
        }

        // fields of the last row the grid asked for, a row is parsed once for all its cells
        private int cachedRow = -1;
        private string[] cachedFields;

        private void dataGridViewLogs_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (lastResults == null || e.RowIndex >= lastResults.Count)
                return;
            if (e.RowIndex != cachedRow)
            {
                cachedFields = lastResults[e.RowIndex].getAll;
                cachedRow = e.RowIndex;
            }
            int index = ((LogField)dataGridViewLogs.Columns[e.ColumnIndex].Tag).Index;
            e.Value = index < cachedFields.Length ? cachedFields[index] : "";
        }

        // like the Monitor tab: the shown columns first, then the others by name
        private void BuildColumnsMenu()
        {
            columnsMenu.Items.Clear();
            ToolStripMenuItem reset = new ToolStripMenuItem("Reset to default columns");
            reset.Click += (s, e) => { SetVisibleColumns(LogFields.All.Where(f => f.Default)); columnsMenu.Close(); };
            columnsMenu.Items.Add(reset);
            columnsMenu.Items.Add(new ToolStripSeparator());

            foreach (LogField field in visibleColumns.Concat(LogFields.All.Except(visibleColumns).OrderBy(f => f.Name)))
            {
                ToolStripMenuItem item = new ToolStripMenuItem(field.Name) { Checked = visibleColumns.Contains(field), CheckOnClick = true, Tag = field };
                item.CheckedChanged += columnItem_CheckedChanged;
                columnsMenu.Items.Add(item);
            }
            ThemeManager.ApplyToMenu(columnsMenu, theme);
        }

        private void columnItem_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            LogField field = (LogField)item.Tag;
            // keep at least one column
            if (!item.Checked && visibleColumns.Count == 1)
            {
                item.Checked = true;
                return;
            }
            SetVisibleColumns(item.Checked ? visibleColumns.Append(field) : visibleColumns.Where(f => f != field));
        }

        // shows the columns in log field order, remembers them and redraws the last search
        private void SetVisibleColumns(IEnumerable<LogField> columns)
        {
            HashSet<LogField> selected = new HashSet<LogField>(columns);
            visibleColumns = LogFields.All.Where(selected.Contains).ToList();
            try
            {
                Properties.Settings.Default.Columns = string.Join(",", visibleColumns.Select(c => c.Index));
                Properties.Settings.Default.Save();
            }
            catch (System.Configuration.ConfigurationErrorsException)
            {
                ; // the columns still change, they are just not remembered
            }
            ShowResults();
        }

        // saved column indexes, the default columns when none are saved or none is valid
        private static List<LogField> ParseColumns(string saved)
        {
            HashSet<int> indexes = new HashSet<int>();
            foreach (string part in (saved ?? "").Split(','))
                if (int.TryParse(part, out int index))
                    indexes.Add(index);
            List<LogField> columns = LogFields.All.Where(f => indexes.Contains(f.Index)).ToList();
            return columns.Count > 0 ? columns : LogFields.All.Where(f => f.Default).ToList();
        }

        private void buttonColumns_Click(object sender, EventArgs e)
        {
            columnsMenu.Show(buttonColumns, new Point(0, buttonColumns.Height));
        }

        private void buttonPauseStartCapture_Click(object sender, EventArgs e)
        {
            // the button text follows the capture state through SetCaptureStatus
            if (Program.IsCapturing)
                Program.StopCapture();
            else
                Program.StartCapture();
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Program.StopCapture();
            Program.StopRefresh();
        }

        private void buttonClean_Click(object sender, EventArgs e)
        {
            // empty the buffer and keep the capture as it was, running or stopped
            bool wasCapturing = Program.IsCapturing;
            Program.StopCapture();
            Program.resetDB();
            if (wasCapturing)
                Program.StartCapture();

            // show the empty buffer right away instead of the rows of the last search
            lastResults = null;
            dataGridViewLogs.RowCount = 0;
            dataGridViewLogs.Columns.Clear();
            toolStripStatusResults.Text = "";
            lableSearchTip.Visible = true;
            SetStatusValues(0, 0);
        }

    }
}
