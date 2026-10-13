using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SmartTools.Tools;
using SmartTools.UI;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools;

public partial class MainWindow : Window
{
    private sealed class ToolDef
    {
        public string Key;
        public string Name;
        public string Badge;
        public Color BadgeColor;
        public Func<Control> Factory;
    }

    private readonly List<ToolDef> _tools = new List<ToolDef>();
    private readonly Dictionary<string, NavButton> _buttons = new Dictionary<string, NavButton>();
    private readonly Dictionary<string, Control> _pages = new Dictionary<string, Control>();

    public MainWindow()
    {
        InitializeComponent();

        DefineTools();
        BuildSidebar();
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
        Add("currency", "Currency Calculator", "FX", "#6366F1", () => new CurrencyCalculatorTool());
    }

    private void Add(string key, string name, string badge, string color, Func<Control> factory)
    {
        _tools.Add(new ToolDef
        {
            Key = key,
            Name = name,
            Badge = badge,
            BadgeColor = ThemePalette.FromHex(color),
            Factory = factory
        });
    }

    private void BuildSidebar()
    {
        AddGroup("DOCUMENTS & CODES", 0);
        for (int i = 0; i < 4; i++) AddNav(_tools[i]);

        AddGroup("CALCULATORS", 14);
        for (int i = 4; i < _tools.Count; i++) AddNav(_tools[i]);
    }

    private void AddGroup(string text, double top)
    {
        NavHost.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(ThemePalette.FromHex("#475569")),
            Margin = new Thickness(7, top, 0, 8)
        });
    }

    private void AddNav(ToolDef tool)
    {
        var b = new NavButton
        {
            Title = tool.Name,
            Badge = tool.Badge,
            BadgeColor = tool.BadgeColor,
            Margin = new Thickness(0, 0, 0, 4)
        };
        b.Click += (s, e) => ShowTool(tool.Key);
        NavHost.Children.Add(b);
        _buttons[tool.Key] = b;
    }

    private void ShowTool(string key)
    {
        foreach (var kv in _buttons) kv.Value.Selected = kv.Key == key;

        if (!_pages.TryGetValue(key, out Control page))
        {
            var def = _tools.First(t => t.Key == key);
            page = def.Factory();
            _pages[key] = page;
        }

        ContentHost.Content = page;
    }
}
