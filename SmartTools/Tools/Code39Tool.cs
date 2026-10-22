using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public class Code39Tool : ToolPage
{
    private static readonly int[] DpiValues = { 96, 150, 300 };

    private readonly ModernTextBox _data = new ModernTextBox { Multiline = true, Text = "Aa-1234" };
    private readonly ModernTextBox _module = new ModernTextBox { NumericOnly = true, Text = "2", Suffix = "px" };
    private readonly ModernComboBox _dpi = new ModernComboBox();
    private readonly ModernComboBox _format = new ModernComboBox();
    private readonly PictureBox _preview = new PictureBox
    {
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Theme.Background
    };
    private readonly Label _status = Ui.Text("", Theme.Small, Theme.Danger);
    private Bitmap _current;

    public Code39Tool() : base("Code 39 Barcode", "Create a Code 39 barcode and save it as an image.")
    {
        _data.Height = Theme.S(110);
        _preview.Height = Theme.S(260);
        _dpi.SetItems("96 DPI (Web)", "150 DPI", "300 DPI (Print)");
        _format.SetItems("PNG", "JPEG");

        var note = Ui.Text("Supported: " + Code39.SupportedCharacters + ". Lower-case letters are converted to upper-case. Only the first line is encoded.",
            Theme.Small, Theme.TextMuted, false);
        note.Height = Theme.S(44);

        var left = new CardPanel();
        left.AddHeading("Data & settings");
        left.AddField("Data to encode", _data, 8);
        left.AddRow(note, 6, 0);
        left.AddField("Module width", _module);
        left.AddField("Image resolution", _dpi);
        left.AddField("Image format", _format);

        var download = ModernButton.Primary("Download barcode");
        download.Click += (s, e) => ImageExport.SaveWithDialog(this, _current, "code39", _format.SelectedItem);

        var right = new CardPanel();
        right.AddHeading("Preview");
        right.AddRow(_preview, 8, 0);
        right.AddRow(_status, 8, 0);
        right.AddRow(download, 14, 0);

        Stack.AddRow(Ui.TwoCards(left, right, 46f));

        _data.TextChanged += (s, e) => Generate();
        _module.TextChanged += (s, e) => Generate();
        _dpi.SelectedIndexChanged += (s, e) => Generate();
        Generate();
    }

    private void Generate()
    {
        string[] lines = (_data.Text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string data = lines.Length > 0 ? lines[0] : " ";

        double width = Fmt.ParseOr(_module.Text, 2);
        if (width < 0.25) width = 0.25;
        if (width > 20) width = 20;
        int dpi = DpiValues[Math.Max(0, Math.Min(DpiValues.Length - 1, _dpi.SelectedIndex))];

        try
        {
            Bitmap bmp = Code39.Render(data, width, dpi);
            Bitmap old = _current;
            _current = bmp;
            _preview.Image = bmp;
            old?.Dispose();
            _status.Text = "";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _preview.Image = null;
            _current?.Dispose();
        }
        base.Dispose(disposing);
    }
}
