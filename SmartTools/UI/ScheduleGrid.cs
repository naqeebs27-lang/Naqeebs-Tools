using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace SmartTools.UI;

/// <summary>One display row of the amortisation schedule (already formatted text).</summary>
public sealed class ScheduleLine
{
    public string Year { get; set; } = "";
    public string Principal { get; set; } = "";
    public string Interest { get; set; } = "";
    public string Payment { get; set; } = "";
    public string Balance { get; set; } = "";

    public static ScheduleLine FromCells(string[] c)
    {
        return new ScheduleLine { Year = c[0], Principal = c[1], Interest = c[2], Payment = c[3], Balance = c[4] };
    }
}

/// <summary>Creates the styled amortisation-schedule table used in the loan calculator and report window.</summary>
public static class ScheduleGrid
{
    public static DataGrid Create()
    {
        var g = new DataGrid
        {
            IsReadOnly = true,
            AutoGenerateColumns = false,
            CanUserResizeColumns = false,
            CanUserSortColumns = false,
            CanUserReorderColumns = false,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            SelectionMode = DataGridSelectionMode.Single,
            RowHeight = 34,
            ColumnHeaderHeight = 40,
            BorderThickness = new Thickness(0),
            Background = Brushes.White,
            RowBackground = Brushes.White,
            HorizontalGridLinesBrush = new SolidColorBrush(Theme.Border)
        };

        AddColumn(g, "Year", false, r => r.Year);
        AddColumn(g, "Principal Paid", true, r => r.Principal);
        AddColumn(g, "Interest Paid", true, r => r.Interest);
        AddColumn(g, "Total Yearly Payment", true, r => r.Payment);
        AddColumn(g, "Remaining Balance", true, r => r.Balance);
        return g;
    }

    private static void AddColumn(DataGrid g, string header, bool right, Func<ScheduleLine, string> text)
    {
        var align = right ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        var column = new DataGridTemplateColumn
        {
            Header = new TextBlock
            {
                Text = header,
                HorizontalAlignment = align,
                Margin = new Thickness(10, 0)
            },
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CanUserSort = false,
            CellTemplate = new FuncDataTemplate<ScheduleLine>((row, _) => new TextBlock
            {
                Text = row == null ? "" : text(row),
                HorizontalAlignment = align,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0)
            }, true)
        };
        g.Columns.Add(column);
    }
}
