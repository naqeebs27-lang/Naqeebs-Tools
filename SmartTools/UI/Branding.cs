using System.Reflection;

namespace SmartTools.UI;

/// <summary>Loads the embedded NMS logo and icon.</summary>
public static class Branding
{
    public const string CompanyName = "Naqeebs Multi Services";
    private static Image _logo;
    private static Icon _icon;

    public static Image Logo
    {
        get
        {
            if (_logo == null)
            {
                using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png");
                if (s != null)
                {
                    using var raw = Image.FromStream(s);
                    _logo = new Bitmap(raw); // detach from the stream
                }
            }
            return _logo;
        }
    }

    public static Icon AppIcon
    {
        get
        {
            if (_icon == null)
            {
                using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
                if (s != null) _icon = new Icon(s);
            }
            return _icon;
        }
    }
}
