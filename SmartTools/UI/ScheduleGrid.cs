namespace SmartTools.UI;

/// <summary>Creates the styled amortisation-schedule table used in the loan calculator and report.</summary>
public static class ScheduleGrid
{
    public static DataGridView Create()
    {
        var g = new DataGridView
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            GridColor = Theme.Border,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = Theme.S(40),
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        };

        g.RowTemplate.Height = Theme.S(34);

        var header = g.ColumnHeadersDefaultCellStyle;
        header.BackColor = Theme.Neutral;
        header.ForeColor = Theme.TextPrimary;
        header.Font = Theme.Caption;
        header.SelectionBackColor = Theme.Neutral;
        header.SelectionForeColor = Theme.TextPrimary;
        header.Alignment = DataGridViewContentAlignment.MiddleLeft;
        header.Padding = new Padding(Theme.S(10), 0, Theme.S(10), 0);

        var cell = g.DefaultCellStyle;
        cell.BackColor = Color.White;
        cell.ForeColor = Theme.TextPrimary;
        cell.Font = Theme.Body;
        cell.SelectionBackColor = Theme.AccentSoft;
        cell.SelectionForeColor = Theme.TextPrimary;
        cell.Padding = new Padding(Theme.S(10), 0, Theme.S(10), 0);

        g.AlternatingRowsDefaultCellStyle.BackColor = Theme.FromHex("#F8FAFC");
        g.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.AccentSoft;
        g.AlternatingRowsDefaultCellStyle.SelectionForeColor = Theme.TextPrimary;

        AddColumn(g, "Year", "Year", false);
        AddColumn(g, "Principal", "Principal Paid", true);
        AddColumn(g, "Interest", "Interest Paid", true);
        AddColumn(g, "Payment", "Total Yearly Payment", true);
        AddColumn(g, "Balance", "Remaining Balance", true);
        return g;
    }

    private static void AddColumn(DataGridView g, string name, string header, bool right)
    {
        int index = g.Columns.Add(name, header);
        var align = right ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft;
        g.Columns[index].DefaultCellStyle.Alignment = align;
        g.Columns[index].HeaderCell.Style.Alignment = align;
        g.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
    }
}
