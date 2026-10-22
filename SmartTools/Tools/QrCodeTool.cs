using SmartTools.Services;
using SmartTools.UI;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace SmartTools.Tools;

public class QrCodeTool : ToolPage
{
    private static readonly string[] Modes = { "URL", "Text", "WiFi", "Email", "vCard" };

    private readonly ModernButton[] _chips = new ModernButton[5];
    private readonly TableLayoutPanel _inputHost = Ui.Stack();
    private readonly PictureBox _preview = new PictureBox
    {
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Theme.Background
    };
    private readonly Label _hint = Ui.Text("Fill in the details and press Generate.", Theme.Small, Theme.TextMuted);

    private string _mode = "URL";
    private ModernTextBox _f1, _f2, _f3, _f4, _f5;
    private Bitmap _current;

    public QrCodeTool() : base("QR Code Generator", "Choose a type, fill in the details and generate a QR code.")
    {
        _preview.Height = Theme.S(320);

        var chipRow = Ui.Columns(Ui.Pct(20), Ui.Pct(20), Ui.Pct(20), Ui.Pct(20), Ui.Pct(20));
        for (int i = 0; i < Modes.Length; i++)
        {
            string mode = Modes[i];
            var b = ModernButton.Secondary(mode);
            b.Height = Theme.S(38);
            b.Click += (s, e) => SelectMode(mode);
            _chips[i] = b;
            chipRow.AddCell(b, i, 0, i < Modes.Length - 1 ? 6 : 0);
        }

        var generate = ModernButton.Success("Generate QR Code");
        generate.Click += (s, e) => Generate();

        var left = new CardPanel();
        left.AddHeading("Content type");
        left.AddRow(chipRow, 8, 8);
        left.AddRow(_inputHost, 6, 0);
        left.AddRow(generate, 22, 0);

        var download = ModernButton.Primary("Download PNG");
        download.Click += (s, e) => ImageExport.SaveWithDialog(this, _current, "qrcode", "PNG");

        var right = new CardPanel();
        right.AddHeading("Your QR code");
        right.AddRow(_preview, 8, 0);
        right.AddRow(_hint, 8, 0);
        right.AddRow(download, 14, 0);

        Stack.AddRow(Ui.TwoCards(left, right, 50f));

        SelectMode("URL");
    }

    private void SelectMode(string mode)
    {
        _mode = mode;
        for (int i = 0; i < Modes.Length; i++)
        {
            bool on = Modes[i] == mode;
            var b = _chips[i];
            b.NormalColor = on ? Theme.Accent : Theme.Neutral;
            b.HoverColor = on ? Theme.AccentDark : Theme.NeutralHover;
            b.TextColor = on ? Color.White : Theme.TextPrimary;
            b.Invalidate();
        }

        _inputHost.SuspendLayout();
        foreach (Control c in _inputHost.Controls.Cast<Control>().ToList())
        {
            _inputHost.Controls.Remove(c);
            c.Dispose();
        }
        _inputHost.RowStyles.Clear();
        _inputHost.RowCount = 0;

        _f1 = _f2 = _f3 = _f4 = _f5 = null;

        switch (mode)
        {
            case "URL":
                _f1 = new ModernTextBox { Placeholder = "https://example.com" };
                _inputHost.AddField("Website address", _f1, 6);
                break;
            case "Text":
                _f1 = new ModernTextBox { Multiline = true, Height = Theme.S(110), Placeholder = "Enter your text here..." };
                _inputHost.AddField("Text", _f1, 6);
                break;
            case "WiFi":
                _f1 = new ModernTextBox { Placeholder = "Network name (SSID)" };
                _f2 = new ModernTextBox { Placeholder = "Password", UsePasswordChar = true };
                _inputHost.AddField("Network name", _f1, 6);
                _inputHost.AddField("Password", _f2, 12);
                break;
            case "Email":
                _f1 = new ModernTextBox { Placeholder = "name@example.com" };
                _f2 = new ModernTextBox { Placeholder = "Subject" };
                _inputHost.AddField("Email address", _f1, 6);
                _inputHost.AddField("Subject", _f2, 12);
                break;
            default:
                _f1 = new ModernTextBox { Placeholder = "First name" };
                _f2 = new ModernTextBox { Placeholder = "Last name" };
                _f3 = new ModernTextBox { Placeholder = "Phone" };
                _f4 = new ModernTextBox { Placeholder = "Email" };
                _f5 = new ModernTextBox { Placeholder = "Company" };
                _inputHost.AddField("First name", _f1, 6);
                _inputHost.AddField("Last name", _f2, 10);
                _inputHost.AddField("Phone", _f3, 10);
                _inputHost.AddField("Email", _f4, 10);
                _inputHost.AddField("Company", _f5, 10);
                break;
        }
        _inputHost.ResumeLayout(true);
    }

    private string BuildPayload()
    {
        switch (_mode)
        {
            case "URL":
            case "Text":
                return _f1.Text;
            case "WiFi":
                return "WIFI:S:" + _f1.Text + ";T:WPA;P:" + _f2.Text + ";;";
            case "Email":
                return "mailto:" + _f1.Text + "?subject=" + Uri.EscapeDataString(_f2.Text);
            default:
                return "BEGIN:VCARD\nVERSION:3.0\nN:" + _f2.Text + ";" + _f1.Text + ";;;\nFN:" + _f1.Text + " " + _f2.Text +
                       "\nTEL:" + _f3.Text + "\nEMAIL:" + _f4.Text + "\nORG:" + _f5.Text + "\nEND:VCARD";
        }
    }

    private void Generate()
    {
        string data = BuildPayload();
        if (string.IsNullOrEmpty(data))
        {
            Warn("Please fill in the fields!");
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
            Bitmap bmp = MatrixRenderer.Render(matrix, scale, scale);

            Bitmap old = _current;
            _current = bmp;
            _preview.Image = bmp;
            old?.Dispose();
            _hint.Text = "Ready - press Download PNG to save it.";
        }
        catch (Exception ex)
        {
            _hint.Text = "Could not generate QR code: " + ex.Message;
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
