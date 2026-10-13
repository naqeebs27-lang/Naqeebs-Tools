using System.Globalization;

namespace SmartTools.Services;

/// <summary>Number formatting / parsing helpers (always invariant so results never depend on Windows regional settings).</summary>
public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Rounded whole number with thousands separators, e.g. 1,234,567</summary>
    public static string Int(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return "0";
        double r = Math.Round(v, MidpointRounding.AwayFromZero) + 0.0; // + 0.0 turns -0 into 0
        return r.ToString("N0", Inv);
    }

    public static string Money(double v, string symbol) => symbol + " " + Int(v);

    public static string Fixed2(double v) => v.ToString("0.00", Inv);

    public static bool TryParse(string s, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        string clean = s.Trim().Replace(",", "").Replace(" ", "");
        if (!double.TryParse(clean, NumberStyles.Float, Inv, out value)) return false;
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public static double ParseOr(string s, double fallback = 0)
    {
        return TryParse(s, out double v) ? v : fallback;
    }

    /// <summary>"1.5 Crores", "25 Lakhs", "50k" style label used by the loan calculator.</summary>
    public static string CroresLakhs(double num, string symbol)
    {
        if (num >= 10000000)
        {
            double cr = Math.Round(num / 10000000, 2, MidpointRounding.AwayFromZero);
            return symbol + " " + cr.ToString("0.##", Inv) + " Crore" + (cr > 1 ? "s" : "");
        }
        if (num >= 100000)
        {
            double lk = Math.Round(num / 100000, 2, MidpointRounding.AwayFromZero);
            return symbol + " " + lk.ToString("0.##", Inv) + " Lakh" + (lk > 1 ? "s" : "");
        }
        if (num >= 1000)
        {
            return symbol + " " + Math.Round(num / 1000, 0, MidpointRounding.AwayFromZero).ToString("0", Inv) + "k";
        }
        return symbol + " " + Int(num);
    }
}
