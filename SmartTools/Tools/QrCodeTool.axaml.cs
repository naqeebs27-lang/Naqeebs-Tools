using Avalonia;
using Avalonia.Controls;
using SmartTools.Services;
using SmartTools.UI;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace SmartTools.Tools;

public partial class QrCodeTool : ToolPage
{
    private static readonly string[] Modes = { "URL", "Text", "WiFi", "Email", "vCard" };

    private readonly Button[] _chips = new Button[5];

    private string _mode = "URL";
    private ModernTextBox _f1, _f2, _f3, _f4, _f5;
    private RasterImage _current;

    public QrCodeTool()
    {
        InitializeComponent();

        for (int i = 0; i < Modes.Length; i++)
        {
            string mode = Modes[i];
            var b = new Button
            {
                Content = mode,
                Height = 38,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, i < Modes.Length - 1 ? 6 : 0, 0),
                Classes = { "btn", "secondary" }
            };
            b.Click += (s, e) => SelectMode(mode);
            _chips[i] = b;
            ChipRow.Children.Add(b);
        }

        GenerateButton.Click += async (s, e) => await GenerateAsync();
        DownloadButton.Click += async (s, e) => await ImageExport.SaveWithDialogAsync(this, _current, "qrcode", "PNG");

        SelectMode("URL");
    }

    private void SelectMode(string mode)
    {
        _mode = mode;
        for (int i = 0; i < Modes.Length; i++)
        {
            _chips[i].SetClass("on", Modes[i] == mode);
        }

        InputHost.Children.Clear();
        _f1 = _f2 = _f3 = _f4 = _f5 = null;

        switch (mode)
        {
            case "URL":
                _f1 = new ModernTextBox { Placeholder = "https://example.com" };
                InputHost.AddField("Website address", _f1, 6);
                break;
            case "Text":
                _f1 = new ModernTextBox { Multiline = true, Height = 110, Placeholder = "Enter your text here..." };
                InputHost.AddField("Text", _f1, 6);
                break;
            case "WiFi":
                _f1 = new ModernTextBox { Placeholder = "Network name (SSID)" };
                _f2 = new ModernTextBox { Placeholder = "Password", UsePasswordChar = true };
                InputHost.AddField("Network name", _f1, 6);
                InputHost.AddField("Password", _f2, 12);
                break;
            case "Email":
                _f1 = new ModernTextBox { Placeholder = "name@example.com" };
                _f2 = new ModernTextBox { Placeholder = "Subject" };
                InputHost.AddField("Email address", _f1, 6);
                InputHost.AddField("Subject", _f2, 12);
                break;
            default:
                _f1 = new ModernTextBox { Placeholder = "First name" };
                _f2 = new ModernTextBox { Placeholder = "Last name" };
                _f3 = new ModernTextBox { Placeholder = "Phone" };
                _f4 = new ModernTextBox { Placeholder = "Email" };
                _f5 = new ModernTextBox { Placeholder = "Company" };
                InputHost.AddField("First name", _f1, 6);
                InputHost.AddField("Last name", _f2, 10);
                InputHost.AddField("Phone", _f3, 10);
                InputHost.AddField("Email", _f4, 10);
                InputHost.AddField("Company", _f5, 10);
                break;
        }
    }

    private static string T(ModernTextBox box)
    {
        return box?.Text ?? "";
    }

    private string BuildPayload()
    {
        switch (_mode)
        {
            case "URL":
            case "Text":
                return T(_f1);
            case "WiFi":
                return "WIFI:S:" + T(_f1) + ";T:WPA;P:" + T(_f2) + ";;";
            case "Email":
                return "mailto:" + T(_f1) + "?subject=" + Uri.EscapeDataString(T(_f2));
            default:
                return "BEGIN:VCARD\nVERSION:3.0\nN:" + T(_f2) + ";" + T(_f1) + ";;;\nFN:" + T(_f1) + " " + T(_f2) +
                       "\nTEL:" + T(_f3) + "\nEMAIL:" + T(_f4) + "\nORG:" + T(_f5) + "\nEND:VCARD";
        }
    }

    private async Task GenerateAsync()
    {
        string data = BuildPayload();
        if (string.IsNullOrEmpty(data))
        {
            await Warn("Please fill in the fields!");
            return;
        }

        try
        {
            var hints = new Dictionary<EncodeHintType, object>
            {
                { EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.H },
                { EncodeHintType.CHARACTER_SET, "UTF-8" },
                { EncodeHintType.MARGIN, 2 }
            };

            var writer = new QRCodeWriter();
            BitMatrix matrix = writer.encode(data, BarcodeFormat.QR_CODE, 1, 1, hints);
            int scale = Math.Max(1, 480 / matrix.Width);
            RasterImage bmp = MatrixRenderer.Render(matrix, scale, scale);

            RasterImage old = _current;
            _current = bmp;

            var oldSource = PreviewImage.Source as IDisposable;
            PreviewImage.Source = bmp.ToAvalonia();
            oldSource?.Dispose();
            old?.Dispose();
            HintText.Text = "Ready - press Download PNG to save it.";
        }
        catch (Exception ex)
        {
            HintText.Text = "Could not generate QR code: " + ex.Message;
        }
    }
}
