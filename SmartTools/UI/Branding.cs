using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace SmartTools.UI;

/// <summary>Loads the NMS logo from the Avalonia assets (Assets/logo.png).</summary>
public static class Branding
{
    public const string CompanyName = "Naqeebs Multi Services";
    private const string LogoUri = "avares://SmartToolsSuite/Assets/logo.png";

    private static Bitmap _logo;
    private static SKBitmap _logoSk;

    /// <summary>Logo for on-screen use.</summary>
    public static Bitmap Logo
    {
        get
        {
            if (_logo == null)
            {
                try
                {
                    using var s = AssetLoader.Open(new Uri(LogoUri));
                    _logo = new Bitmap(s);
                }
                catch
                {
                    // logo is optional
                }
            }
            return _logo;
        }
    }

    /// <summary>Logo for the printed / PDF loan report (Skia drawing).</summary>
    public static SKBitmap LogoSk
    {
        get
        {
            if (_logoSk == null)
            {
                try
                {
                    using var s = AssetLoader.Open(new Uri(LogoUri));
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    _logoSk = SKBitmap.Decode(ms.ToArray());
                }
                catch
                {
                    // logo is optional
                }
            }
            return _logoSk;
        }
    }
}
