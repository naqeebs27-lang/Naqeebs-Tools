using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public partial class DiscountTool : ToolPage
{
    public DiscountTool()
    {
        InitializeComponent();

        PrintButton.Click += async (s, e) => await PrintReportAsync();
        PriceBox.TextChanged += (s, e) => Recalculate();
        PercentBox.TextChanged += (s, e) => Recalculate();
        Recalculate();
    }

    private void Recalculate()
    {
        double price = Fmt.ParseOr(PriceBox.Text);
        double percent = Fmt.ParseOr(PercentBox.Text);
        double saving = price * (percent / 100.0);
        double finalAmount = price - saving;

        OriginalText.Text = "$ " + Fmt.Fixed2(price);
        SavedText.Text = "$ " + Fmt.Fixed2(saving);
        FinalText.Text = "$ " + Fmt.Fixed2(finalAmount);
    }

    private async Task PrintReportAsync()
    {
        double price = Fmt.ParseOr(PriceBox.Text);
        double percent = Fmt.ParseOr(PercentBox.Text);
        double saving = price * (percent / 100.0);
        double finalAmount = price - saving;

        await ReportPrintHelper.ShowPreviewAsync(this, "Discount Report", new[]
        {
            "Price details:",
            "Original price: $ " + Fmt.Fixed2(price),
            "Discount: " + Fmt.Fixed2(percent) + "%",
            "",
            "Discount result:",
            "You save: $ " + Fmt.Fixed2(saving),
            "Final price: $ " + Fmt.Fixed2(finalAmount)
        });
    }
}
