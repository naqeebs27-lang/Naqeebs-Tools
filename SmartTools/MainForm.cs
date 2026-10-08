using System.Drawing.Drawing2D;
using SmartTools.Tools;
using SmartTools.UI;

namespace SmartTools;

public class MainForm : Form
{
    private sealed class ToolDef
    {
        public string Key;
        public string Name;
        public string Badge;
        public Color BadgeColor;
        public Func<UserControl> Factory;
    }

    private readonly Panel _sidebar = new Panel();
    private readonly Panel _content = new Panel();
    private readonly List<ToolDef> _tools = new List<ToolDef>();
    private readonly Dictionary<string, NavButton> _buttons = new Dictionary<string, NavButton>();
    private readonly Dictionary<string, UserControl> _pages = new Dictionary<string, UserControl>();

    public MainForm()
    {
        Text = "Naqeebs Multi Services - Smart Tools";
        AutoScaleMode = AutoScaleMode.None;
        Font = Theme.Body;
        BackColor = Theme.Background;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(Theme.S(1100), Theme.S(700));
        Size = new Size(Theme.S(1320), Theme.S(820));
        Icon = Branding.AppIcon ?? BuildIcon();

        DefineTools();
        BuildSidebar();

        _content.Dock = DockStyle.Fill;
        _content.BackColor = Theme.Background;

        Controls.Add(_content);
        Controls.Add(_sidebar);

        ShowTool(_tools[0].Key);
    }

    private void DefineTools()
    {
        Add("pdf", "PDF to Image", "PDF", "#EF4444", () => new PdfToImageTool());
        Add("code39", "Code 39 Barcode", "C39", "#F97316", () => new Code39Tool());
        Add("pdf417", "PDF417 Barcode", "417", "#D97706", () => new Pdf417Tool());
        Add("qr", "QR Code", "QR", "#8B5CF6", () => new QrCodeTool());
        Add("age", "Age Calculator", "AGE", "#10B981", () => new AgeCalculatorTool());
        Add("salary", "Salaried Tax", "TAX", "#0EA5E9", () => new SalaryTaxTool());
        Add("business", "Business Tax", "BIZ", "#2563EB", () => new BusinessTaxTool());
        Add("loan", "Loan EMI", "EMI", "#F43F5E", () => new LoanCalculatorTool());
        Add("discount", "Discount", "%", "#14B8A6", () => new DiscountTool());
    }

    private void Add(string key, string name, string badge, string color, Func<UserControl> factory)
    {
        _tools.Add(new ToolDef
        {
            Key = key,
            Name = name,
            Badge = badge,
            BadgeColor = Theme.FromHex(color),
            Factory = factory
        });
    }

    private void BuildSidebar()
    {
        _sidebar.Dock = DockStyle.Left;
        _sidebar.Width = Theme.S(264);
        _sidebar.BackColor = Theme.Sidebar;

        int pad = Theme.S(14);
        int y = Theme.S(26);

        var logoBox = new Panel
        {
            Location = new Point(pad, y - Theme.S(8)),
            Size = new Size(_sidebar.Width - 2 * pad, Theme.S(112)),
            BackColor = Theme.Sidebar
        };
        logoBox.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var r = new Rectangle(0, 0, logoBox.Width - 1, logoBox.Height - 1);
            using (var path = Theme.RoundedRect(r, Theme.S(14)))
            using (var white = new SolidBrush(Color.White))
            {
                g.FillPath(white, path);
            }
            var logo = Branding.Logo;
            if (logo != null)
            {
                int maxW = r.Width - Theme.S(20);
                int maxH = r.Height - Theme.S(16);
                double k = Math.Min((double)maxW / logo.Width, (double)maxH / logo.Height);
                int w = (int)(logo.Width * k), h = (int)(logo.Height * k);
                g.DrawImage(logo, (r.Width - w) / 2, (r.Height - h) / 2, w, h);
            }
        };
        _sidebar.Controls.Add(logoBox);
        y += Theme.S(112);

        var sub = new Label
        {
            Text = "Smart Tools  \u2022  calculators & codes",
            Font = Theme.Small,
            ForeColor = Theme.SidebarText,
            AutoSize = true,
            Location = new Point(pad + Theme.S(4), y)
        };
        _sidebar.Controls.Add(sub);
        y += Theme.S(36);

        y = AddGroup("DOCUMENTS & CODES", y, pad);
        for (int i = 0; i < 4; i++) y = AddNav(_tools[i], y, pad);

        y += Theme.S(14);
        y = AddGroup("CALCULATORS", y, pad);
        for (int i = 4; i < _tools.Count; i++) y = AddNav(_tools[i], y, pad);

        var footer = new Label
        {
            Text = "Offline  \u2022  Windows desktop edition",
            Font = Theme.Small,
            ForeColor = Theme.FromHex("#475569"),
            Dock = DockStyle.Bottom,
            Height = Theme.S(44),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(pad + Theme.S(7), 0, 0, 0)
        };
        _sidebar.Controls.Add(footer);
    }

    private int AddGroup(string text, int y, int pad)
    {
        var l = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Theme.FromHex("#475569"),
            AutoSize = true,
            Location = new Point(pad + Theme.S(7), y)
        };
        _sidebar.Controls.Add(l);
        return y + Theme.S(26);
    }

    private int AddNav(ToolDef tool, int y, int pad)
    {
        var b = new NavButton
        {
            Text = tool.Name,
            Badge = tool.Badge,
            BadgeColor = tool.BadgeColor,
            Location = new Point(pad, y),
            Width = _sidebar.Width - 2 * pad
        };
        b.Click += (s, e) => ShowTool(tool.Key);
        _sidebar.Controls.Add(b);
        _buttons[tool.Key] = b;
        return y + b.Height + Theme.S(4);
    }

    private void ShowTool(string key)
    {
        foreach (var kv in _buttons) kv.Value.Selected = kv.Key == key;

        if (!_pages.TryGetValue(key, out UserControl page))
        {
            var def = _tools.First(t => t.Key == key);
            page = def.Factory();
            _pages[key] = page;
        }

        _content.SuspendLayout();
        _content.Controls.Clear();
        page.Dock = DockStyle.Fill;
        _content.Controls.Add(page);
        _content.ResumeLayout(true);
    }

    private static Icon BuildIcon()
    {
        using var bmp = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var path = Theme.RoundedRect(new Rectangle(2, 2, 60, 60), 14);
            using var brush = new LinearGradientBrush(new Rectangle(0, 0, 64, 64),
                Theme.FromHex("#2563EB"), Theme.FromHex("#7C3AED"), 45f);
            g.FillPath(brush, path);
            using var font = new Font("Segoe UI", 26f, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, "ST", font, new Rectangle(0, 0, 64, 64), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
