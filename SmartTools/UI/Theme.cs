using System.Drawing.Drawing2D;

namespace SmartTools.UI;

/// <summary>Central place for colours, fonts and DPI helpers so the whole app looks consistent.</summary>
public static class Theme
{
    // ---------- DPI ----------
    private static float _scale;

    public static float Scale
    {
        get
        {
            if (_scale <= 0f)
            {
                using var g = Graphics.FromHwnd(IntPtr.Zero);
                _scale = g.DpiX / 96f;
                if (_scale < 1f) _scale = 1f;
            }
            return _scale;
        }
    }

    /// <summary>Scale a pixel value (designed at 96 DPI) to the current screen DPI.</summary>
    public static int S(int px) => (int)Math.Round(px * Scale);

    public static Padding P(int all) => new Padding(S(all));
    public static Padding P(int left, int top, int right, int bottom) => new Padding(S(left), S(top), S(right), S(bottom));

    public static Color FromHex(string hex) => ColorTranslator.FromHtml(hex);

    // ---------- Colours ----------
    public static readonly Color Sidebar = FromHex("#0B1220");
    public static readonly Color SidebarHover = FromHex("#162036");
    public static readonly Color SidebarActive = FromHex("#1E2B47");
    public static readonly Color SidebarText = FromHex("#94A3B8");

    public static readonly Color Background = FromHex("#F1F5F9");
    public static readonly Color Card = Color.White;
    public static readonly Color Border = FromHex("#E2E8F0");
    public static readonly Color BorderStrong = FromHex("#CBD5E1");

    public static readonly Color TextPrimary = FromHex("#0F172A");
    public static readonly Color TextMuted = FromHex("#64748B");
    public static readonly Color TextLight = FromHex("#94A3B8");

    public static readonly Color Accent = FromHex("#2563EB");
    public static readonly Color AccentDark = FromHex("#1D4ED8");
    public static readonly Color AccentSoft = FromHex("#EAF1FF");

    public static readonly Color Success = FromHex("#059669");
    public static readonly Color SuccessDark = FromHex("#047857");
    public static readonly Color SuccessSoft = FromHex("#E8F7F0");
    public static readonly Color Warning = FromHex("#F59E0B");
    public static readonly Color Danger = FromHex("#DC2626");
    public static readonly Color DangerSoft = FromHex("#FDECEC");

    public static readonly Color Neutral = FromHex("#EEF2F7");
    public static readonly Color NeutralHover = FromHex("#E2E8F0");
    public static readonly Color Disabled = FromHex("#CBD5E1");
    public static readonly Color DisabledText = FromHex("#F8FAFC");

    public static readonly Color Dark = FromHex("#0F172A");
    public static readonly Color DarkBorder = FromHex("#1E293B");

    // ---------- Fonts ----------
    public static readonly Font Body = new Font("Segoe UI", 10f, FontStyle.Regular);
    public static readonly Font BodyBold = new Font("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font Small = new Font("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font Caption = new Font("Segoe UI", 9f, FontStyle.Bold);
    public static readonly Font Heading = new Font("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font Title = new Font("Segoe UI", 21f, FontStyle.Bold);
    public static readonly Font Big = new Font("Segoe UI", 26f, FontStyle.Bold);
    public static readonly Font Large = new Font("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font Mono = new Font("Consolas", 10f, FontStyle.Regular);

    // ---------- Drawing helpers ----------
    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        if (r.Width <= 0 || r.Height <= 0)
        {
            return path;
        }

        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 1)
        {
            path.AddRectangle(r);
            return path;
        }

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Color ParentBack(Control c)
    {
        Color back = c.Parent != null ? c.Parent.BackColor : Background;
        return back.A == 0 ? Background : back;
    }

    public static Color Darken(Color c, float amount = 0.12f)
    {
        int r = (int)(c.R * (1f - amount));
        int g = (int)(c.G * (1f - amount));
        int b = (int)(c.B * (1f - amount));
        return Color.FromArgb(c.A, r, g, b);
    }
}
