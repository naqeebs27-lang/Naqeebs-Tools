using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SmartTools.Services;

/// <summary>Where the exchange rates come from.</summary>
public enum RateSource
{
    /// <summary>State Bank of Pakistan daily interbank (M2M revaluation) rates, crossed with world rates for other currencies.</summary>
    SbpInterbank = 0,

    /// <summary>World market rates from an open data feed (any currency, any date since 1999).</summary>
    WorldMarket = 1
}

/// <summary>All rates for one day, stored as "units of currency per 1 USD" so any pair can be crossed.</summary>
public sealed class RateTable
{
    public RateSource Source;
    public DateTime RequestedDate;
    public DateTime RateDate;
    public string SourceLabel = "";

    /// <summary>Extra information for the user (date adjusted, offline cache, fallback ...). Empty when nothing to say.</summary>
    public string Note = "";
    public bool NoteIsWarning;

    public readonly Dictionary<string, double> PerUsd = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Currencies whose rate comes straight from SBP (the others are crossed through USD).</summary>
    public readonly HashSet<string> SbpPublished = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool TryGetRate(string from, string to, out double rate, out string error)
    {
        rate = 0;
        error = null;

        if (!PerUsd.TryGetValue(from, out double f) || f <= 0)
        {
            error = from + " is not available from this rate source on " + RateDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + ". Try the other rate source or another date.";
            return false;
        }

        if (!PerUsd.TryGetValue(to, out double t) || t <= 0)
        {
            error = to + " is not available from this rate source on " + RateDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + ". Try the other rate source or another date.";
            return false;
        }

        rate = t / f;
        return true;
    }
}

/// <summary>ISO 4217 currencies shown in the drop-downs.</summary>
public static class CurrencyCatalog
{
    private static readonly string[] Raw =
    {
        "AED|United Arab Emirates Dirham", "AFN|Afghan Afghani", "ALL|Albanian Lek", "AMD|Armenian Dram",
        "ANG|Netherlands Antillean Guilder", "AOA|Angolan Kwanza", "ARS|Argentine Peso", "AUD|Australian Dollar",
        "AWG|Aruban Florin", "AZN|Azerbaijani Manat", "BAM|Bosnia-Herzegovina Convertible Mark", "BBD|Barbadian Dollar",
        "BDT|Bangladeshi Taka", "BGN|Bulgarian Lev", "BHD|Bahraini Dinar", "BIF|Burundian Franc",
        "BMD|Bermudian Dollar", "BND|Brunei Dollar", "BOB|Bolivian Boliviano", "BRL|Brazilian Real",
        "BSD|Bahamian Dollar", "BTN|Bhutanese Ngultrum", "BWP|Botswana Pula", "BYN|Belarusian Ruble",
        "BZD|Belize Dollar", "CAD|Canadian Dollar", "CDF|Congolese Franc", "CHF|Swiss Franc",
        "CLP|Chilean Peso", "CNY|Chinese Yuan", "COP|Colombian Peso", "CRC|Costa Rican Colon",
        "CUP|Cuban Peso", "CVE|Cape Verdean Escudo", "CZK|Czech Koruna", "DJF|Djiboutian Franc",
        "DKK|Danish Krone", "DOP|Dominican Peso", "DZD|Algerian Dinar", "EGP|Egyptian Pound",
        "ERN|Eritrean Nakfa", "ETB|Ethiopian Birr", "EUR|Euro", "FJD|Fijian Dollar",
        "FKP|Falkland Islands Pound", "GBP|British Pound Sterling", "GEL|Georgian Lari", "GHS|Ghanaian Cedi",
        "GIP|Gibraltar Pound", "GMD|Gambian Dalasi", "GNF|Guinean Franc", "GTQ|Guatemalan Quetzal",
        "GYD|Guyanese Dollar", "HKD|Hong Kong Dollar", "HNL|Honduran Lempira", "HTG|Haitian Gourde",
        "HUF|Hungarian Forint", "IDR|Indonesian Rupiah", "ILS|Israeli New Shekel", "INR|Indian Rupee",
        "IQD|Iraqi Dinar", "IRR|Iranian Rial", "ISK|Icelandic Krona", "JMD|Jamaican Dollar",
        "JOD|Jordanian Dinar", "JPY|Japanese Yen", "KES|Kenyan Shilling", "KGS|Kyrgyzstani Som",
        "KHR|Cambodian Riel", "KMF|Comorian Franc", "KPW|North Korean Won", "KRW|South Korean Won",
        "KWD|Kuwaiti Dinar", "KYD|Cayman Islands Dollar", "KZT|Kazakhstani Tenge", "LAK|Lao Kip",
        "LBP|Lebanese Pound", "LKR|Sri Lankan Rupee", "LRD|Liberian Dollar", "LSL|Lesotho Loti",
        "LYD|Libyan Dinar", "MAD|Moroccan Dirham", "MDL|Moldovan Leu", "MGA|Malagasy Ariary",
        "MKD|Macedonian Denar", "MMK|Myanmar Kyat", "MNT|Mongolian Tugrik", "MOP|Macanese Pataca",
        "MRU|Mauritanian Ouguiya", "MUR|Mauritian Rupee", "MVR|Maldivian Rufiyaa", "MWK|Malawian Kwacha",
        "MXN|Mexican Peso", "MYR|Malaysian Ringgit", "MZN|Mozambican Metical", "NAD|Namibian Dollar",
        "NGN|Nigerian Naira", "NIO|Nicaraguan Cordoba", "NOK|Norwegian Krone", "NPR|Nepalese Rupee",
        "NZD|New Zealand Dollar", "OMR|Omani Rial", "PAB|Panamanian Balboa", "PEN|Peruvian Sol",
        "PGK|Papua New Guinean Kina", "PHP|Philippine Peso", "PKR|Pakistani Rupee", "PLN|Polish Zloty",
        "PYG|Paraguayan Guarani", "QAR|Qatari Riyal", "RON|Romanian Leu", "RSD|Serbian Dinar",
        "RUB|Russian Ruble", "RWF|Rwandan Franc", "SAR|Saudi Riyal", "SBD|Solomon Islands Dollar",
        "SCR|Seychellois Rupee", "SDG|Sudanese Pound", "SEK|Swedish Krona", "SGD|Singapore Dollar",
        "SHP|Saint Helena Pound", "SLE|Sierra Leonean Leone", "SOS|Somali Shilling", "SRD|Surinamese Dollar",
        "SSP|South Sudanese Pound", "STN|Sao Tome and Principe Dobra", "SVC|Salvadoran Colon", "SYP|Syrian Pound",
        "SZL|Swazi Lilangeni", "THB|Thai Baht", "TJS|Tajikistani Somoni", "TMT|Turkmenistani Manat",
        "TND|Tunisian Dinar", "TOP|Tongan Paanga", "TRY|Turkish Lira", "TTD|Trinidad and Tobago Dollar",
        "TWD|New Taiwan Dollar", "TZS|Tanzanian Shilling", "UAH|Ukrainian Hryvnia", "UGX|Ugandan Shilling",
        "USD|United States Dollar", "UYU|Uruguayan Peso", "UZS|Uzbekistani Som", "VES|Venezuelan Bolivar",
        "VND|Vietnamese Dong", "VUV|Vanuatu Vatu", "WST|Samoan Tala", "XAF|Central African CFA Franc",
        "XCD|East Caribbean Dollar", "XCG|Caribbean Guilder", "XOF|West African CFA Franc", "XPF|CFP Franc",
        "YER|Yemeni Rial", "ZAR|South African Rand", "ZMW|Zambian Kwacha", "ZWG|Zimbabwe Gold"
    };

    /// <summary>Code + name, sorted by code.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> All { get; } = Build();

    private static List<KeyValuePair<string, string>> Build()
    {
        var list = new List<KeyValuePair<string, string>>();
        foreach (string line in Raw)
        {
            int bar = line.IndexOf('|');
            list.Add(new KeyValuePair<string, string>(line.Substring(0, bar), line.Substring(bar + 1)));
        }
        list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        return list;
    }
}

/// <summary>
/// Downloads and caches exchange rates.
///  * SBP source: State Bank of Pakistan "Exchange Rates for Mark to Market Revaluation" PDF of the day
///    (USD is the weighted average of closing interbank rates). Other currencies that SBP does not publish
///    are crossed through USD using the world feed.
///  * World source: open currency data (fawazahmed0 mirror), falling back to the ECB feed (Frankfurter).
/// Everything fetched is saved in %LOCALAPPDATA%\NaqeebsSmartTools\fx so past dates load instantly and offline.
/// </summary>
public static class RateService
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly HttpClient Http = CreateClient();
    private static readonly ConcurrentDictionary<string, CacheEntry> Memory = new ConcurrentDictionary<string, CacheEntry>();
    private static readonly TimeSpan TodayLifetime = TimeSpan.FromMinutes(30);

    private sealed class CacheEntry
    {
        public DateTime Created;
        public Task<RateTable> Work;
    }

    public sealed class SbpDto
    {
        public string RateDate { get; set; }
        public Dictionary<string, double> PkrPer { get; set; }

        [JsonIgnore]
        public bool Stale { get; set; }
    }

    public sealed class WorldDto
    {
        public string RateDate { get; set; }
        public string Provider { get; set; }
        public Dictionary<string, double> PerUsd { get; set; }

        [JsonIgnore]
        public bool Stale { get; set; }
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) SmartToolsSuite/1.0");
        return client;
    }

    // ------------------------------------------------------------------ public API

    public static Task<RateTable> GetAsync(RateSource source, DateTime date)
    {
        date = date.Date;
        string key = KeyOf(source, date);

        if (Memory.TryGetValue(key, out CacheEntry existing))
        {
            bool failed = existing.Work.IsCompleted && (existing.Work.IsFaulted || existing.Work.IsCanceled);
            bool fresh = date < DateTime.Today || DateTime.UtcNow - existing.Created < TodayLifetime;
            if (!failed && fresh)
            {
                return existing.Work;
            }
        }

        var entry = new CacheEntry { Created = DateTime.UtcNow, Work = LoadAsync(source, date) };
        Memory[key] = entry;
        return entry.Work;
    }

    public static async Task<(RateTable Table, string XmlPath)> SaveManualSbpRatesAsync(
        DateTime requested, double usdPkr, double jpyPkr, double gbpPkr, double eurPkr)
    {
        var rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = usdPkr,
            ["JPY"] = jpyPkr,
            ["GBP"] = gbpPkr,
            ["EUR"] = eurPkr
        };

        foreach (var rate in rates)
        {
            if (rate.Value <= 0 || double.IsNaN(rate.Value) || double.IsInfinity(rate.Value))
            {
                throw new ArgumentOutOfRangeException(rate.Key, "Enter a positive rate for " + rate.Key + ".");
            }
        }

        requested = requested.Date;
        WriteCache("sbp", requested, new SbpDto
        {
            RateDate = requested.ToString("yyyy-MM-dd", Inv),
            PkrPer = rates
        });
        Memory.TryRemove(KeyOf(RateSource.SbpInterbank, requested), out _);

        RateTable table = await BuildSbpAsync(requested);
        string xmlPath = WriteImportedXml(requested, table, rates);
        return (table, xmlPath);
    }

    /// <summary>Forget everything saved for this source/date so the next GetAsync downloads again.</summary>
    public static void Invalidate(RateSource source, DateTime date)
    {
        date = date.Date;
        Memory.TryRemove(KeyOf(source, date), out _);
        DeleteCache("sbp", date);
        DeleteCache("world", date);
    }

    private static string KeyOf(RateSource source, DateTime date)
    {
        return source.ToString() + "|" + date.ToString("yyyyMMdd", Inv);
    }

    private static async Task<RateTable> LoadAsync(RateSource source, DateTime date)
    {
        if (source == RateSource.WorldMarket)
        {
            return await BuildWorldAsync(date);
        }
        return await BuildSbpAsync(date);
    }

    // ------------------------------------------------------------------ table builders

    private static async Task<RateTable> BuildWorldAsync(DateTime date)
    {
        WorldDto world = await FetchWorldAsync(date);

        var table = new RateTable
        {
            Source = RateSource.WorldMarket,
            RequestedDate = date,
            RateDate = ParseDate(world.RateDate, date),
            SourceLabel = world.Provider
        };

        foreach (var kv in world.PerUsd)
        {
            table.PerUsd[kv.Key] = kv.Value;
        }
        table.PerUsd["USD"] = 1.0;

        if (world.Stale)
        {
            table.Note = "No internet connection - showing rates saved earlier on this PC.";
            table.NoteIsWarning = true;
        }
        else if (table.RateDate.Date != date.Date)
        {
            table.Note = "No rates were published for the selected day, so the closest earlier rates (" + table.RateDate.ToString("dd MMM yyyy", Inv) + ") are shown.";
        }

        return table;
    }

    private static async Task<RateTable> BuildSbpAsync(DateTime date)
    {
        SbpDto sbp = null;
        string sbpProblem = null;
        try
        {
            sbp = await FetchSbpAsync(date);
        }
        catch (Exception ex)
        {
            sbpProblem = ex.Message;
        }

        DateTime effective = sbp != null ? ParseDate(sbp.RateDate, date) : date;

        WorldDto world = null;
        string worldProblem = null;
        try
        {
            world = await FetchWorldAsync(effective);
        }
        catch (Exception ex)
        {
            worldProblem = ex.Message;
        }

        if (sbp == null && world == null)
        {
            throw new InvalidOperationException(sbpProblem + " " + worldProblem);
        }

        var table = new RateTable
        {
            Source = RateSource.SbpInterbank,
            RequestedDate = date,
            RateDate = effective
        };

        if (world != null)
        {
            foreach (var kv in world.PerUsd)
            {
                table.PerUsd[kv.Key] = kv.Value;
            }
            table.RateDate = ParseDate(world.RateDate, effective);
        }
        table.PerUsd["USD"] = 1.0;

        if (sbp != null)
        {
            table.RateDate = effective;
            table.SourceLabel = "SBP interbank (M2M revaluation)";

            double usdPkr = sbp.PkrPer["USD"];
            table.PerUsd["PKR"] = usdPkr;
            table.SbpPublished.Add("USD");
            table.SbpPublished.Add("PKR");

            foreach (var kv in sbp.PkrPer)
            {
                if (string.Equals(kv.Key, "USD", StringComparison.OrdinalIgnoreCase) || kv.Value <= 0)
                {
                    continue;
                }

                double perUsd = usdPkr / kv.Value;

                // Safety net: if a value read from the PDF is far away from the world market, do not trust it.
                if (world != null && world.PerUsd.TryGetValue(kv.Key.ToUpperInvariant(), out double market) && market > 0)
                {
                    double deviation = Math.Abs(perUsd / market - 1.0);
                    if (deviation > 0.15)
                    {
                        continue;
                    }
                }

                table.PerUsd[kv.Key] = perUsd;
                table.SbpPublished.Add(kv.Key);
            }

            if (sbp.Stale || (world != null && world.Stale))
            {
                table.Note = "No internet connection - showing rates saved earlier on this PC.";
                table.NoteIsWarning = true;
            }
            else if (world == null)
            {
                table.Note = "Only the currencies published by SBP are available because the world cross rates could not be loaded (" + worldProblem + ").";
                table.NoteIsWarning = true;
            }
            else if (effective.Date != date.Date)
            {
                table.Note = "SBP publishes on banking days only, so the latest SBP rates on or before the selected day (" + effective.ToString("dd MMM yyyy", Inv) + ") are used.";
            }
        }
        else
        {
            table.SourceLabel = world.Provider + " (SBP not available)";
            table.Note = "SBP rates could not be loaded (" + sbpProblem + ") - world market rates are shown instead.";
            table.NoteIsWarning = true;
        }

        return table;
    }

    private static DateTime ParseDate(string text, DateTime fallback)
    {
        if (!string.IsNullOrEmpty(text) &&
            DateTime.TryParseExact(text, "yyyy-MM-dd", Inv, DateTimeStyles.None, out DateTime d))
        {
            return d;
        }
        return fallback;
    }

    // ------------------------------------------------------------------ SBP (PDF)

    private static IEnumerable<string> SbpUrls(DateTime d)
    {
        // SBP publishes Conversion Rates documents using several filename layouts.
        string conversionRates = "https://www.sbp.org.pk/assets/document/conversion-rates-"
                                + d.ToString("dd-MMMM-yyyy", Inv).ToLowerInvariant() + ".pdf";
        string conversionRatesNew = "https://www.sbp.org.pk/assets/document/Conversion_Rates_-_"
                                   + d.ToString("dd-MMMM-yyyy", Inv) + ".pdf";
        string current = "https://www.sbp.org.pk/assets/document/mark-to-market-revaluation-exchange-rate-"
                         + d.ToString("dd-MMMM-yyyy", Inv).ToLowerInvariant() + ".pdf";
        string legacy = "https://www.sbp.org.pk/ecodata/rates/m2m/"
                        + d.ToString("yyyy", Inv) + "/" + d.ToString("MMM", Inv) + "/" + d.ToString("dd-MMM-yy", Inv) + ".pdf";

        if (d >= new DateTime(2026, 6, 1))
        {
            yield return conversionRatesNew;
            yield return conversionRates;
            yield return current;
            yield return legacy;
        }
        else
        {
            yield return legacy;
            yield return current;
        }
    }

    private static async Task<SbpDto> FetchSbpAsync(DateTime requested)
    {
        SbpDto cached = ReadCache<SbpDto>("sbp", requested, false);
        if (IsValid(cached))
        {
            return cached;
        }

        int lastStatus = 0;
        try
        {
            // Walk back over weekends / bank holidays until a published day is found.
            for (int back = 0; back <= 14; back++)
            {
                DateTime d = requested.AddDays(-back);
                if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                // A request for today may need yesterday's already-cached business-day rate
                // when today's SBP document has not been published or cannot be reached.
                if (d != requested)
                {
                    SbpDto cachedDay = ReadCache<SbpDto>("sbp", d, false);
                    if (IsValid(cachedDay))
                    {
                        return cachedDay;
                    }
                }

                foreach (string url in SbpUrls(d))
                {
                    var download = await DownloadPdfAsync(url);
                    lastStatus = download.Status;
                    if (download.Data == null)
                    {
                        continue;
                    }

                    byte[] bytes = download.Data;
                    Dictionary<string, double> rates = await Task.Run(() => ParseSbpPdf(bytes));
                    if (rates == null)
                    {
                        continue;
                    }

                    var dto = new SbpDto { RateDate = d.ToString("yyyy-MM-dd", Inv), PkrPer = rates };
                    WriteCache("sbp", requested, dto);
                    return dto;
                }
            }
        }
        catch (Exception)
        {
            SbpDto stale = ReadCache<SbpDto>("sbp", requested, true);
            if (IsValid(stale))
            {
                stale.Stale = true;
                return stale;
            }
            throw;
        }

        SbpDto old = ReadCache<SbpDto>("sbp", requested, true);
        if (IsValid(old))
        {
            old.Stale = true;
            return old;
        }

        throw new InvalidOperationException("No SBP rate document was found for " + requested.ToString("dd MMM yyyy", Inv)
            + " or the days before it" + (lastStatus > 0 ? " (last HTTP status " + lastStatus + ")" : "") + ".");
    }

    private static bool IsValid(SbpDto dto)
    {
        return dto != null && dto.PkrPer != null && dto.PkrPer.ContainsKey("USD");
    }

    private static string WriteImportedXml(DateTime requested, RateTable table, Dictionary<string, double> sbpRates)
    {
        string directory = Path.GetDirectoryName(CacheFile("sbp", requested));
        string path = Path.Combine(directory, "currency-rates-" + requested.ToString("yyyyMMdd", Inv) + ".xml");
        var root = new XElement("currencyRateData",
            new XAttribute("source", "Imported SBP conversion-rate PDF"),
            new XAttribute("requestedDate", requested.ToString("yyyy-MM-dd", Inv)),
            new XAttribute("rateDate", table.RateDate.ToString("yyyy-MM-dd", Inv)),
            new XAttribute("base", "USD"),
            new XAttribute("generatedUtc", DateTime.UtcNow.ToString("O", Inv)));

        var currencies = new XElement("currencies");
        foreach (var currency in CurrencyCatalog.All.OrderBy(c => c.Key))
        {
            var element = new XElement("currency", new XAttribute("code", currency.Key));
            if (sbpRates.TryGetValue(currency.Key, out double pkrPerUnit) && pkrPerUnit > 0)
            {
                element.Add(new XAttribute("sbpPkrPerUnit", pkrPerUnit.ToString("G17", Inv)));
            }
            if (table.PerUsd.TryGetValue(currency.Key, out double unitsPerUsd) && unitsPerUsd > 0)
            {
                element.Add(new XAttribute("unitsPerUsd", unitsPerUsd.ToString("G17", Inv)));
            }
            currencies.Add(element);
        }

        root.Add(currencies);
        new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).Save(path);
        return path;
    }

    private static async Task<(byte[] Data, int Status)> DownloadPdfAsync(string url)
    {
        using var response = await Http.GetAsync(url);
        int status = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            // 5xx / 429 = the server has a problem (report it); other 4xx = that file simply does not exist.
            if (status >= 500 || status == 429)
            {
                throw new HttpRequestException("SBP server answered " + status + " " + response.ReasonPhrase);
            }
            return (null, status);
        }

        byte[] data = await response.Content.ReadAsByteArrayAsync();
        bool isPdf = data.Length > 100 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46; // "%PDF"
        return (isPdf ? data : null, status);
    }

    /// <summary>
    /// Reads SBP's rate table and returns PKR per 1 unit of each currency,
    /// or null when the document does not look like the SBP rate sheet.
    /// Words are regrouped into visual rows by position, so it does not matter in which order the PDF stores them.
    /// </summary>
    private static Dictionary<string, double> ParseSbpPdf(byte[] bytes)
    {
        var rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        using (PdfDocument pdf = PdfDocument.Open(bytes))
        {
            foreach (var page in pdf.GetPages())
            {
                List<Word> words = page.GetWords().ToList();
                words.Sort((a, b) => b.BoundingBox.Bottom.CompareTo(a.BoundingBox.Bottom)); // top of page first

                var lines = new List<List<Word>>();
                double lineY = 0;
                foreach (Word w in words)
                {
                    double y = w.BoundingBox.Bottom;
                    if (lines.Count == 0 || Math.Abs(y - lineY) > 3.0)
                    {
                        lines.Add(new List<Word>());
                        lineY = y;
                    }
                    lines[lines.Count - 1].Add(w);
                }

                string pendingCode = null;
                foreach (List<Word> line in lines)
                {
                    line.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
                    var tokens = new List<string>();
                    foreach (Word w in line)
                    {
                        string text = w.Text.Trim();
                        if (text.Length > 0)
                        {
                            tokens.Add(text);
                        }
                    }
                    if (tokens.Count == 0)
                    {
                        continue;
                    }

                    if (TryNamedCurrencyRow(tokens, out string namedCode, out double namedRate))
                    {
                        if (!rates.ContainsKey(namedCode))
                        {
                            rates[namedCode] = namedRate;
                        }
                        continue;
                    }

                    string code = null;
                    int start = 0;
                    if (IsCurrencyCode(tokens[0]))
                    {
                        code = tokens[0];
                        start = 1;
                    }

                    if (code != null && tokens.Count == 1)
                    {
                        pendingCode = code; // code and numbers sit on slightly different baselines
                        continue;
                    }

                    var numbers = new List<double>();
                    bool allNumeric = tokens.Count > start;
                    for (int i = start; i < tokens.Count; i++)
                    {
                        if (TryRate(tokens[i], out double value))
                        {
                            numbers.Add(value);
                        }
                        else
                        {
                            allNumeric = false;
                            break;
                        }
                    }

                    if (!allNumeric || numbers.Count < 3)
                    {
                        continue;
                    }

                    if (code == null)
                    {
                        code = pendingCode ?? (rates.Count == 0 ? "USD" : null); // first row of the sheet is always USD
                    }
                    pendingCode = null;

                    if (code != null && numbers[0] > 0 && !rates.ContainsKey(code))
                    {
                        rates[code] = numbers[0]; // READY column
                    }
                }
            }
        }

        // Sanity check: USD/PKR must exist and be believable, otherwise this was not the rate sheet.
        if (!rates.TryGetValue("USD", out double usd) || usd < 20 || usd > 5000)
        {
            return null;
        }

        return rates;
    }

    private static bool IsCurrencyCode(string s)
    {
        if (s.Length != 3)
        {
            return false;
        }
        foreach (char c in s)
        {
            if (c < 'A' || c > 'Z')
            {
                return false;
            }
        }
        return true;
    }

    private static bool TryNamedCurrencyRow(List<string> tokens, out string code, out double rate)
    {
        code = null;
        rate = 0;

        var nameTokens = new List<string>();
        int numericCount = 0;
        foreach (string token in tokens)
        {
            if (TryRate(token, out double value))
            {
                numericCount++;
                rate = value;
            }
            else
            {
                nameTokens.Add(token);
            }
        }

        if (numericCount != 1 || rate <= 0)
        {
            return false;
        }

        string name = string.Join(" ", nameTokens)
            .Replace(".", "", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

        code = name switch
        {
            "U S DOLLAR" => "USD",
            "JAPANESE YEN" => "JPY",
            "POUND STERLING" => "GBP",
            "EURO" => "EUR",
            _ => null
        };

        return code != null;
    }

    private static bool TryRate(string s, out double value)
    {
        value = 0;
        if (s.IndexOf('.') < 0)
        {
            return false;
        }
        if (!double.TryParse(s.Replace(",", ""), NumberStyles.Float, Inv, out value))
        {
            return false;
        }
        return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }

    // ------------------------------------------------------------------ world rates (JSON)

    private static async Task<WorldDto> FetchWorldAsync(DateTime date)
    {
        WorldDto cached = ReadCache<WorldDto>("world", date, false);
        if (IsValid(cached))
        {
            return cached;
        }

        string tag = date >= DateTime.Today ? "latest" : date.ToString("yyyy-MM-dd", Inv);
        Exception last = null;

        // 1) Open currency data - 200+ currencies, daily history from March 2024 (two mirrors).
        if (date >= new DateTime(2024, 3, 3))
        {
            string[] urls =
            {
                "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@" + tag + "/v1/currencies/usd.json",
                "https://" + tag + ".currency-api.pages.dev/v1/currencies/usd.json"
            };

            foreach (string url in urls)
            {
                try
                {
                    string json = await Http.GetStringAsync(url);
                    WorldDto dto = ParseOpenData(json);
                    if (IsValid(dto))
                    {
                        WriteCache("world", date, dto);
                        return dto;
                    }
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }
        }

        // 2) ECB reference rates through Frankfurter - about 30 major currencies, history back to 1999.
        try
        {
            string json = await Http.GetStringAsync("https://api.frankfurter.dev/v1/" + tag + "?base=USD");
            WorldDto dto = ParseFrankfurter(json);
            if (IsValid(dto))
            {
                WriteCache("world", date, dto);
                return dto;
            }
        }
        catch (Exception ex)
        {
            last = ex;
        }

        WorldDto stale = ReadCache<WorldDto>("world", date, true);
        if (IsValid(stale))
        {
            stale.Stale = true;
            return stale;
        }

        throw new InvalidOperationException("World market rates could not be loaded for " + date.ToString("dd MMM yyyy", Inv)
            + (last != null ? " (" + last.Message + ")" : "") + ".");
    }

    private static bool IsValid(WorldDto dto)
    {
        return dto != null && dto.PerUsd != null && dto.PerUsd.Count > 5;
    }

    private static WorldDto ParseOpenData(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        if (!root.TryGetProperty("usd", out JsonElement usd) || usd.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var map = new Dictionary<string, double>();
        foreach (JsonProperty p in usd.EnumerateObject())
        {
            if (p.Value.ValueKind != JsonValueKind.Number)
            {
                continue;
            }
            double v = p.Value.GetDouble();
            if (v > 0 && !double.IsInfinity(v))
            {
                map[p.Name.ToUpperInvariant()] = v;
            }
        }
        map["USD"] = 1.0;

        string date = null;
        if (root.TryGetProperty("date", out JsonElement de) && de.ValueKind == JsonValueKind.String)
        {
            date = de.GetString();
        }

        return new WorldDto { RateDate = date, Provider = "World market (open currency data)", PerUsd = map };
    }

    private static WorldDto ParseFrankfurter(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        if (!root.TryGetProperty("rates", out JsonElement rates) || rates.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var map = new Dictionary<string, double>();
        foreach (JsonProperty p in rates.EnumerateObject())
        {
            if (p.Value.ValueKind != JsonValueKind.Number)
            {
                continue;
            }
            double v = p.Value.GetDouble();
            if (v > 0 && !double.IsInfinity(v))
            {
                map[p.Name.ToUpperInvariant()] = v;
            }
        }
        map["USD"] = 1.0;

        string date = null;
        if (root.TryGetProperty("date", out JsonElement de) && de.ValueKind == JsonValueKind.String)
        {
            date = de.GetString();
        }

        return new WorldDto { RateDate = date, Provider = "World market (ECB reference rates)", PerUsd = map };
    }

    // ------------------------------------------------------------------ disk cache

    private static string CacheFile(string name, DateTime date)
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NaqeebsSmartTools", "fx");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, name + "-" + date.ToString("yyyyMMdd", Inv) + ".json");
    }

    private static T ReadCache<T>(string name, DateTime date, bool allowStale) where T : class
    {
        try
        {
            string path = CacheFile(name, date);
            if (!File.Exists(path))
            {
                return null;
            }

            // Past days never change; today's rates are reused for 30 minutes (or any age when offline).
            if (!allowStale && date >= DateTime.Today && DateTime.Now - File.GetLastWriteTime(path) > TodayLifetime)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    private static void WriteCache<T>(string name, DateTime date, T value)
    {
        try
        {
            File.WriteAllText(CacheFile(name, date), JsonSerializer.Serialize(value));
        }
        catch
        {
            // caching is optional
        }
    }

    private static void DeleteCache(string name, DateTime date)
    {
        try
        {
            string path = CacheFile(name, date);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignore
        }
    }
}
