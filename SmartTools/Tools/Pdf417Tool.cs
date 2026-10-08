using SmartTools.Services;
using SmartTools.UI;
using ZXing;
using ZXing.Common;
using ZXing.PDF417;

namespace SmartTools.Tools;

public class Pdf417Tool : ToolPage
{
    private static readonly int[] DpiValues = { 96, 150, 300 };

    private const string SampleData =
        "x4YY++pZMyuhIfl/gEc6DjwZC1rDI7y48mAuFQKOA3hjBS1FFERdCkVPhgBhoVk47ra5kpdKUjYIwhHkTo+wC6o3Mzh7boB1SjbFh/nbWu9HThQKEfrmuGVGBdiWXBNUEaeBNXDGQkFZRSoUapDZlw==,data";

    private readonly ModernTextBox _data = new ModernTextBox { Multiline = true, Text = SampleData };
    private readonly ModernComboBox _dpi = new ModernComboBox();
    private readonly ModernComboBox _rotation = new ModernComboBox();
    private readonly ModernComboBox _format = new ModernComboBox();
    private readonly PictureBox _preview = new PictureBox
    {
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Theme.Background
    };
    private readonly Label _status = Ui.Text("", Theme.Small, Theme.Danger);
    private Bitmap _current;

    public Pdf417Tool() : base("PDF417 Barcode", "Generate a PDF417 2D barcode (used on IDs, tickets and licences).")
    {
        _data.Font = Theme.Mono;
        _data.Height = Theme.S(190);
        _preview.Height = Theme.S(280);
        _dpi.SetItems("96 DPI", "150 DPI", "300 DPI");
        _rotation.SetItems("0\u00B0", "90\u00B0", "180\u00B0", "270\u00B0");
        _format.SetItems("PNG", "JPEG", "GIF");

        var left = new CardPanel();
        left.AddHeading("Data & settings");
        left.AddField("Data", _data, 8);
        left.AddField("Image resolution", _dpi);
        left.AddField("Image rotation", _rotation);
        left.AddField("Image format", _format);

        var download = ModernButton.Primary("Download");
        download.Click += (s, e) => ImageExport.SaveWithDialog(this, _current, "pdf417", _format.SelectedItem);

        var right = new CardPanel();
        right.AddHeading("Preview");
        right.AddRow(_preview, 8, 0);
        right.AddRow(_status, 8, 0);
        right.AddRow(download, 14, 0);

        Stack.AddRow(Ui.TwoCards(left, right, 46f));

        _data.TextChanged += (s, e) => Generate();
        _dpi.SelectedIndexChanged += (s, e) => Generate();
        _rotation.SelectedIndexChanged += (s, e) => Generate();
        Generate();
    }

    private void Generate()
    {
        string data = _data.Text ?? "";
        if (data.Length == 0)
        {
            _status.Text = "Enter some data to encode.";
            return;
        }

        int dpi = DpiValues[Math.Max(0, Math.Min(DpiValues.Length - 1, _dpi.SelectedIndex))];
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
            Bitmap bmp = MatrixRenderer.Render(matrix, cell, cell);
            bmp.SetResolution(dpi, dpi);

            switch (_rotation.SelectedIndex)
            {
                case 1: bmp.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                case 2: bmp.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                case 3: bmp.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
            }

            Bitmap old = _current;
            _current = bmp;
            _preview.Image = bmp;
            old?.Dispose();
            _status.Text = "";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not generate barcode: " + ex.Message;
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
