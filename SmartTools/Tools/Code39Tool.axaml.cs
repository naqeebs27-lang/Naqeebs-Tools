using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public partial class Code39Tool : ToolPage
{
    private static readonly int[] DpiValues = { 96, 150, 300 };

    private RasterImage _current;

    public Code39Tool()
    {
        InitializeComponent();

        DpiBox.SetItems("96 DPI (Web)", "150 DPI", "300 DPI (Print)");
        FormatBox.SetItems("PNG", "JPEG");
        NoteText.Text = "Supported: " + Code39.SupportedCharacters +
                        ". Lower-case letters are converted to upper-case. Only the first line is encoded.";

        DownloadButton.Click += async (s, e) =>
            await ImageExport.SaveWithDialogAsync(this, _current, "code39", FormatBox.SelectedText);

        DataBox.TextChanged += (s, e) => Generate();
        ModuleBox.TextChanged += (s, e) => Generate();
        DpiBox.SelectedIndexChanged += (s, e) => Generate();
        Generate();
    }

    private void Generate()
    {
        string[] lines = (DataBox.Text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string data = lines.Length > 0 ? lines[0] : " ";

        double width = Fmt.ParseOr(ModuleBox.Text, 2);
        if (width < 0.25) width = 0.25;
        if (width > 20) width = 20;
        int dpi = DpiValues[Math.Max(0, Math.Min(DpiValues.Length - 1, DpiBox.SelectedIndex))];

        try
        {
            RasterImage img = Code39.Render(data, width, dpi);
            RasterImage old = _current;
            _current = img;

            var oldSource = PreviewImage.Source as IDisposable;
            PreviewImage.Source = img.ToAvalonia();
            oldSource?.Dispose();
            old?.Dispose();
            StatusText.Text = "";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }
}
