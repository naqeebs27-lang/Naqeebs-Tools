using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public class DiscountTool : ToolPage
{
    private readonly ModernTextBox _price = new ModernTextBox { NumericOnly = true, Prefix = "$", Text = "100" };
    private readonly ModernTextBox _percent = new ModernTextBox { NumericOnly = true, Suffix = "%", Text = "15" };

    private readonly Label _original = Ui.Text("$ 100.00", Theme.BodyBold);
    private readonly Label _saved = Ui.Text("$ 15.00", Theme.Large, Theme.Success);
    private readonly Label _final = Ui.Text("$ 85.00", Theme.Big, Theme.Success);
    private readonly ModernButton _print = ModernButton.Secondary("Print / Preview");

    public DiscountTool() : base("Discount Calculator", "Work out the sale price and see exactly how much you save.")
    {
        var inputs = new CardPanel();
        inputs.AddHeading("Price details", "Results update as you type");
        inputs.AddField("Original price", _price, 8);
        inputs.AddField("Discount", _percent);
        _print.Click += (s, e) => PrintReport();
        inputs.AddRow(_print, 22, 0);

        var results = new CardPanel();
        results.AddHeading("Your price");
        results.AddRow(Ui.KeyValue("Original price", _original), 10, 0);
        results.AddRow(Ui.Divider(), 12, 12);
        results.AddRow(Ui.KeyValue("You save", _saved), 0, 0);
        results.AddRow(Ui.Divider(), 12, 12);
        results.AddRow(Ui.Text("Final price", Theme.Caption, Theme.TextMuted), 0, 0);
        results.AddRow(_final, 2, 0);

        Stack.AddRow(Ui.TwoCards(inputs, results, 46f));

        _price.TextChanged += (s, e) => Recalculate();
        _percent.TextChanged += (s, e) => Recalculate();
        Recalculate();
    }

    private void Recalculate()
    {
        double price = Fmt.ParseOr(_price.Text);
        double percent = Fmt.ParseOr(_percent.Text);
        double saving = price * (percent / 100.0);
        double finalAmount = price - saving;

        _original.Text = "$ " + Fmt.Fixed2(price);
        _saved.Text = "$ " + Fmt.Fixed2(saving);
        _final.Text = "$ " + Fmt.Fixed2(finalAmount);
    }

    private void PrintReport()
    {
        double price = Fmt.ParseOr(_price.Text);
        double percent = Fmt.ParseOr(_percent.Text);
        double saving = price * (percent / 100.0);
        double finalAmount = price - saving;

        ReportPrintHelper.ShowPreview(FindForm(), "Discount Report", new[]
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
