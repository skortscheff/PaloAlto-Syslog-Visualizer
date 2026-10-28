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
        static bool captureStatus = true;
        bool captureOn = true;
        Theme theme;
        Font actionFont;

        public FormMain()
        {
            // saved choice of the toggle, otherwise follow the Windows app mode
            string savedTheme = Properties.Settings.Default.Theme;
            if (savedTheme == "Dark")
                theme = Theme.Dark;
            else if (savedTheme == "Light")
                theme = Theme.Light;
            else
                theme = Theme.SystemUsesDarkMode() ? Theme.Dark : Theme.Light;

            InitializeComponent();

            // smoother scrolling and repaint of the grid
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(dataGridViewLogs, true);
            dataGridViewLogs.CellFormatting += dataGridViewLogs_CellFormatting;

            ToolTip toolTip = new ToolTip();
            foreach (CheckBox checkBox in new[] { checkBoxSouceAddress, checkBoxDestinationAddress, checkBoxDestinationPort, checkBoxAction, checkBoxInboundInterface, checkBoxRuleName })
                toolTip.SetToolTip(checkBox, "Exclude the logs that match this filter");
            toolTip.SetToolTip(buttonSearch, "Show the last 50 logs that match the filters (Enter)");
            toolTip.SetToolTip(buttonClean, "Empty the buffer");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // the designer size is taller than many screens once scaled, so the status bar would be hidden
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            if (Width > workingArea.Width || Height > workingArea.Height)
                WindowState = FormWindowState.Maximized;

            // square exclude toggles next to their text box, sized after the DPI scaling
            foreach ((CheckBox checkBox, TextBox textBox) in new[] {
                (checkBoxSouceAddress, textBoxSouceAddress), (checkBoxDestinationAddress, textBoxDestinationAddress),
                (checkBoxDestinationPort, textBoxDestinationPort), (checkBoxAction, textBoxAction),
                (checkBoxInboundInterface, textBoxInboundInterface), (checkBoxRuleName, textBoxRuleName) })
            {
                checkBox.AutoSize = false;
                checkBox.Bounds = new Rectangle(textBox.Right + 2, textBox.Top, textBox.Height, textBox.Height);
                checkBox.CheckedChanged += (s, ev) => ApplyTheme();
            }

            ApplyTheme();
            // keep the hint centered, Anchor None keeps it there on resize
            lableSearchTip.Location = new Point((panelLogs.Width - lableSearchTip.Width) / 2, (panelLogs.Height - lableSearchTip.Height) / 2);
        }

        private void ApplyTheme()
        {
            ThemeManager.Apply(this, theme);
            SetCaptureStatus(captureOn);
            buttonTheme.Text = theme.IsDark ? "Light mode" : "Dark mode";
        }

        private void buttonTheme_Click(object sender, EventArgs e)
        {
            theme = theme.IsDark ? Theme.Light : Theme.Dark;
            Properties.Settings.Default.Theme = theme.IsDark ? "Dark" : "Light";
            Properties.Settings.Default.Save();
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

        private String[] columnsHeader =
        {
            "Receive Time",
            "Source Address",
            "Destination Address",
            "NAT Source IP",
            "NAT Destination Address",
            "Rule Name",
            "Destination User",
            "Application",
            "Source Zone",
            "Destination Zone",
            "Inbound Interface",
            "Outbound Interface",
            "Session ID",
            "Source Port",
            "Destination Port",
            "NAT Source Port",
            "NAT Destination Port",
            "Flags",
            "Protocol",
            "Action",
            "Bytes",
            "Bytes Received",
            "Bytes Sent",
            "Packets",
            "Elapsed Time",
            "Category",
            "Packets Sent",
            "Packets Received",
            "Session End Reason",
            "Action Source"
        };

        private enum searchParameter
        {
            SourceAddress,
            DestinationAddress,
            DestinationPort,
            Action,
            InboundInterface,
            RuleName
        };

        private int[] getIndexOfIndexSearchParameter(int indexDB)
        {
            int[] indexOfIndexSearchParameter = new int[Enum.GetNames(typeof(searchParameter)).Length];
            indexOfIndexSearchParameter[(int)searchParameter.SourceAddress] = Program.database[indexDB].strSourceAddress.IndexOf(textBoxSouceAddress.Text, StringComparison.CurrentCultureIgnoreCase);
            indexOfIndexSearchParameter[(int)searchParameter.DestinationAddress] = Program.database[indexDB].strDestinationAddress.IndexOf(textBoxDestinationAddress.Text, StringComparison.CurrentCultureIgnoreCase);
            indexOfIndexSearchParameter[(int)searchParameter.DestinationPort] = Program.database[indexDB].strDestinationPort.IndexOf(textBoxDestinationPort.Text, StringComparison.CurrentCultureIgnoreCase);
            indexOfIndexSearchParameter[(int)searchParameter.Action] = Program.database[indexDB].strAction.IndexOf(textBoxAction.Text, StringComparison.CurrentCultureIgnoreCase);
            indexOfIndexSearchParameter[(int)searchParameter.InboundInterface] = Program.database[indexDB].strInboundInterface.IndexOf(textBoxInboundInterface.Text, StringComparison.CurrentCultureIgnoreCase);
            indexOfIndexSearchParameter[(int)searchParameter.RuleName] = Program.database[indexDB].strRuleName.IndexOf(textBoxRuleName.Text, StringComparison.CurrentCultureIgnoreCase);
            
            return indexOfIndexSearchParameter;
        }

        private bool[] getCheckBoxCheckedSearchParameter()
        {
            bool[] checkBoxCheckedSearchParameter = new bool[Enum.GetNames(typeof(searchParameter)).Length];
            checkBoxCheckedSearchParameter[(int)searchParameter.SourceAddress] = checkBoxSouceAddress.Checked;
            checkBoxCheckedSearchParameter[(int)searchParameter.DestinationAddress] = checkBoxDestinationAddress.Checked;
            checkBoxCheckedSearchParameter[(int)searchParameter.DestinationPort] = checkBoxDestinationPort.Checked;
            checkBoxCheckedSearchParameter[(int)searchParameter.Action] = checkBoxAction.Checked;
            checkBoxCheckedSearchParameter[(int)searchParameter.InboundInterface] = checkBoxInboundInterface.Checked;
            checkBoxCheckedSearchParameter[(int)searchParameter.RuleName] = checkBoxRuleName.Checked;

            return checkBoxCheckedSearchParameter;
        }

        private bool[] getSearchByParameter()
        {
            bool[] searchByParameter = new bool[Enum.GetNames(typeof(searchParameter)).Length]; //all false by default

            if (textBoxSouceAddress.Text != "")
                searchByParameter[(int)searchParameter.SourceAddress] = true;
            if (textBoxDestinationAddress.Text != "")
                searchByParameter[(int)searchParameter.DestinationAddress] = true;
            if (textBoxDestinationPort.Text != "")
                searchByParameter[(int)searchParameter.DestinationPort] = true;
            if (textBoxAction.Text != "")
                searchByParameter[(int)searchParameter.Action] = true;
            if (textBoxInboundInterface.Text != "")
                searchByParameter[(int)searchParameter.InboundInterface] = true;
            if (textBoxRuleName.Text != "")
                searchByParameter[(int)searchParameter.RuleName] = true;

            return searchByParameter;
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            lableSearchTip.Visible = false;
            DataTable dt = new DataTable();


            foreach (String column in columnsHeader)
                dt.Columns.Add(column, typeof(string));

            bool[] searchByParameter = getSearchByParameter();

            bool searchIsOn = false;
            foreach (int enumIndex in Enum.GetValues(typeof(searchParameter)))
                if (searchByParameter[enumIndex])
                    searchIsOn = true;

            int itemLeftToPrint = 50;
            int lastItemWrited = (int)Program.databaseIndexLastItem;
            if (lastItemWrited != -1)
            {
                bool[] checkBoxCheckedSearchParameter = getCheckBoxCheckedSearchParameter();
                for (int indexDB = lastItemWrited; indexDB >= 0 && itemLeftToPrint > 0; indexDB--)
                {
                    if (searchIsOn)
                    {
                        int[] indexOfIndexSearchParameter = getIndexOfIndexSearchParameter(indexDB);

                        bool matchTheFilter = true;
                        foreach (int enumIndex in Enum.GetValues(typeof(searchParameter)))
                            if (matchTheFilter) //if it's false I don't care anymore
                                matchTheFilter = !searchByParameter[enumIndex] || (checkBoxCheckedSearchParameter[enumIndex] ? indexOfIndexSearchParameter[enumIndex] == -1 : indexOfIndexSearchParameter[enumIndex] != -1);

                        if(matchTheFilter)
                        {
                            dt.Rows.Add(Program.database[indexDB].getAll);
                            itemLeftToPrint--;
                        }
                    }
                    else
                    {
                        dt.Rows.Add(Program.database[indexDB].getAll);
                        itemLeftToPrint--;
                    }
                }

                if (itemLeftToPrint > 0 && Program.databaseOverwrite)
                {
                    int indexDB = (int)Program.databaseSize - 1;
                    while (indexDB > lastItemWrited && itemLeftToPrint > 0)
                    {
                        if (searchIsOn)
                        {
                            int[] indexOfIndexSearchParameter = getIndexOfIndexSearchParameter(indexDB);

                            bool matchTheFilter = true;
                            foreach (int enumIndex in Enum.GetValues(typeof(searchParameter)))
                                if (matchTheFilter) //if it's false I don't care anymore
                                    matchTheFilter = !searchByParameter[enumIndex] || (checkBoxCheckedSearchParameter[enumIndex] ? indexOfIndexSearchParameter[enumIndex] == -1 : indexOfIndexSearchParameter[enumIndex] != -1);

                            if (matchTheFilter)
                            {
                                dt.Rows.Add(Program.database[indexDB].getAll);
                                itemLeftToPrint--;
                            }
                        }
                        else {
                            dt.Rows.Add(Program.database[indexDB].getAll);
                            itemLeftToPrint--;
                        }
                        indexDB--;
                    }
                }
            }

            dataGridViewLogs.DataSource = dt;

            // not sortable
            foreach (DataGridViewColumn column in dataGridViewLogs.Columns)
                column.SortMode = DataGridViewColumnSortMode.NotSortable;

            // remove the empty one
            dataGridViewLogs.AllowUserToAddRows = false;

            // resize the columns
            foreach (DataGridViewColumn column in dataGridViewLogs.Columns)
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            // the scroll bars are created with the first data, theme them too
            ApplyTheme();

        }

        private void buttonPauseStartCapture_Click(object sender, EventArgs e)
        {
            // if it's capturing -> stop it and set the button to restart it later
            if (captureStatus)
            {
                // Stop the syslog capture
                Program.StopCapture();
                buttonPauseStartCapture.Text = "Start capture";
            }
            // if it's not capturing -> start it and set the button to stop it later
            else
            {
                // Restart the syslog capture
                Program.StartCapture();
                buttonPauseStartCapture.Text = "Stop capture";
            }
            captureStatus = !captureStatus;
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Program.StopCapture();
            Program.StopRefresh();
        }

        private void buttonClean_Click(object sender, EventArgs e)
        {
            Program.StopCapture();
            Program.resetDB();
            Program.StartCapture();
        }

    }
}
