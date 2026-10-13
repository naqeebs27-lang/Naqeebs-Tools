# Naqeebs Multi Services - Smart Tools  (C# / .NET 8 / Avalonia UI)

A desktop version of your HTML/CSS/JS tools. One window, a sidebar, ten tools. Runs on **Windows and Linux**.

| Tool | Notes |
|---|---|
| PDF to Image | PDFium engine, 2x PNG per page, drag & drop, save one/all |
| Code 39 Barcode | Own encoder, module width, DPI 96/150/300, PNG/JPEG |
| PDF417 Barcode | ZXing.Net, DPI, rotation, PNG/JPEG/GIF |
| QR Code | URL / Text / WiFi / Email / vCard, error-correction H, PNG |
| Age Calculator | Years/months/days + weeks, days, hours, minutes, seconds |
| Salaried Tax | Tax years 2015-2027 (slabs copied 1:1 from your JS) |
| Business Tax | Tax years 2023-2027, AOP cap and 10% surcharge |
| Loan EMI | Sliders, presets, currencies, donut chart, year-wise schedule, report (PDF / preview / copy / CSV) |
| Discount | Live savings and final price |
| Currency Calculator | SBP interbank / world market rates, any date, compare two dates |

## Build (.NET 8 SDK)
```
dotnet run --project SmartTools
```
Standalone build:
* Windows: `publish.bat`  ->  `publish\SmartToolsSuite.exe`
* Linux:   `./publish.sh` ->  `publish/SmartToolsSuite`

Visual Studio 2022+, Rider or VS Code all work: open `SmartTools.sln`.

## Linux notes
* Needs a desktop session (X11 or Wayland/XWayland) and `libfontconfig1` (preinstalled on most distros;
  Debian/Ubuntu minimal installs: `sudo apt install libfontconfig1`).
* The PDF engine (PDFium) and graphics (SkiaSharp) ship as native libraries inside the NuGet packages: linux-x64 is supported.
* "Print / Preview" shows an on-screen preview and can save a PDF or open it in your PDF viewer (`xdg-open`) to print.

## Notes
* Tax figures are estimates copied from the original tool - verify against current FBR rules.
* UI is XAML (`*.axaml`) + code-behind; shared look lives in `SmartTools/App.axaml`.
* Business logic (`Services/AgeMath, LoanMath, TaxMath, Fmt, CurrencyRates`) is unchanged from the WinForms version.

## Branding
Logo: `SmartTools/Assets/logo.png` (sidebar + loan report). Window icon: `SmartTools/Assets/icon.png`. Exe icon: `SmartTools/app.ico`.
