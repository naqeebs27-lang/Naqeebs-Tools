using SkiaSharp;

namespace SmartTools.Services;

/// <summary>
/// Resolves a typeface from a list of preferred families. "Segoe UI" and "Consolas" only exist on Windows,
/// so Linux falls through to DejaVu / Liberation / whatever the default font is.
/// </summary>
internal static class SkFonts
{
    public static readonly string[] Sans = { "Segoe UI", "Inter", "Noto Sans", "DejaVu Sans", "Liberation Sans", "Arial", "Helvetica" };
    public static readonly string[] Mono = { "Consolas", "DejaVu Sans Mono", "Liberation Mono", "Menlo", "Courier New" };

    private static readonly Dictionary<string, SKTypeface> Cache = new Dictionary<string, SKTypeface>();

    public static SKTypeface Resolve(string[] families, bool bold)
    {
        string key = string.Join("|", families) + (bold ? "#bold" : "#regular");
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out SKTypeface cached)) return cached;

            SKFontStyle style = bold ? SKFontStyle.Bold : SKFontStyle.Normal;
            SKTypeface found = null;
            foreach (string name in families)
            {
                SKTypeface candidate = SKTypeface.FromFamilyName(name, style);
                if (candidate != null && string.Equals(candidate.FamilyName, name, StringComparison.OrdinalIgnoreCase))
                {
                    found = candidate;
                    break;
                }
                candidate?.Dispose();
            }

            if (found == null)
            {
                try { found = SKTypeface.FromFamilyName(null, style); } catch { found = null; }
            }
            found ??= SKTypeface.Default;

            Cache[key] = found;
            return found;
        }
    }
}
