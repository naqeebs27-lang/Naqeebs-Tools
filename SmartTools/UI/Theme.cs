using Avalonia.Media;

namespace SmartTools.UI;

/// <summary>
/// Colours used from code (the XAML side uses the Brand* resources in App.axaml).
/// Pixel/DPI helpers from the WinForms version are gone: Avalonia sizes are resolution independent.
/// </summary>
public static class Theme
{
    public static Color FromHex(string hex) => Color.Parse(hex);
    public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    // ---------- Colours ----------
    public static readonly Color Sidebar = FromHex("#0B1220");
    public static readonly Color SidebarText = FromHex("#94A3B8");

    public static readonly Color Background = FromHex("#F1F5F9");
    public static readonly Color Border = FromHex("#E2E8F0");
    public static readonly Color BorderStrong = FromHex("#CBD5E1");

    public static readonly Color TextPrimary = FromHex("#0F172A");
    public static readonly Color TextMuted = FromHex("#64748B");
    public static readonly Color TextLight = FromHex("#94A3B8");

    public static readonly Color Accent = FromHex("#2563EB");
    public static readonly Color Success = FromHex("#059669");
    public static readonly Color Warning = FromHex("#F59E0B");
    public static readonly Color Danger = FromHex("#DC2626");
    public static readonly Color Dark = FromHex("#0F172A");

    // ---------- Brushes used by code-behind ----------
    public static readonly IBrush TextMutedBrush = new SolidColorBrush(TextMuted);
    public static readonly IBrush TextPrimaryBrush = new SolidColorBrush(TextPrimary);
    public static readonly IBrush DangerBrush = new SolidColorBrush(Danger);
    public static readonly IBrush WarningTextBrush = new SolidColorBrush(FromHex("#B45309"));
}
