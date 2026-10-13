using SmartTools.Services;
using SmartTools.UI;
using ZXing;
using ZXing.Common;
using ZXing.PDF417;

namespace SmartTools.Tools;

public partial class Pdf417Tool : ToolPage
{
    private static readonly int[] DpiValues = { 96, 150, 300 };

    private const string SampleData =
        "x4YY++pZMyuhIfl/gEc6DjwZC1rDI7y48mAuFQKOA3hjBS1FFERdCkVPhgBhoVk47ra5kpdKUjYIwhHkTo+wC6o3Mzh7boB1SjbFh/nbWu9HThQKEfrmuGVGBdiWXBNUEaeBNXDGQkFZRSoUapDZlw==,data";

    private RasterImage _current;

    public Pdf417Tool()
    {
        InitializeComponent();

        DataBox.Text = SampleData;
        DpiBox.SetItems("96 DPI", "150 DPI", "300 DPI");
        RotationBox.SetItems("0\u00B0", "90\u00B0", "180\u00B0", "270\u00B0");
        FormatBox.SetItems("PNG", "JPEG", "GIF");

        DownloadButton.Click += async (s, e) =>
            await ImageExport.SaveWithDialogAsync(this, _current, "pdf417", FormatBox.SelectedText);

        DataBox.TextChanged += (s, e) => Generate();
        DpiBox.SelectedIndexChanged += (s, e) => Generate();
        RotationBox.SelectedIndexChanged += (s, e) => Generate();
        Generate();
    }

    private void Generate()
    {
        string data = DataBox.Text ?? "";
        if (data.Length == 0)
        {
            StatusText.Text = "Enter some data to encode.";
            return;
        }

        int dpi = DpiValues[Math.Max(0, Math.Min(DpiValues.Length - 1, DpiBox.SelectedIndex))];
        int cell = Math.Max(1, (int)Math.Round(2.0 * dpi / 96.0));

        try
        {
            var hints = new Dictionary<EncodeHintType, object>
            {
                { EncodeHintType.MARGIN, 2 }
            };

            bool needsUtf8 = false;
            foreach (char ch in data)
            {
                if (ch > 255) { needsUtf8 = true; break; }
            }
            if (needsUtf8)
            {
                hints[EncodeHintType.CHARACTER_SET] = "UTF-8";
            }

            var writer = new PDF417Writer();
            BitMatrix matrix = writer.encode(data, BarcodeFormat.PDF_417, 1, 1, hints);
            RasterImage raw = MatrixRenderer.Render(matrix, cell, cell);
            raw.Dpi = dpi;

            // 0 / 90 / 180 / 270 degrees clockwise
            RasterImage bmp = RotationBox.SelectedIndex > 0 ? raw.Rotated(RotationBox.SelectedIndex) : raw;
            if (!ReferenceEquals(bmp, raw)) raw.Dispose();

            RasterImage old = _current;
            _current = bmp;

            var oldSource = PreviewImage.Source as IDisposable;
            PreviewImage.Source = bmp.ToAvalonia();
            oldSource?.Dispose();
            old?.Dispose();
            StatusText.Text = "";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not generate barcode: " + ex.Message;
        }
    }
}
