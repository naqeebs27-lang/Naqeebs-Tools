# Naqeebs Multi Services - Smart Tools  (C# / .NET 10 / Windows Forms)

A desktop version of your HTML/CSS/JS tools. One window, a sidebar, nine tools:

| Tool | Notes |
|---|---|
| PDF to Image | Windows built-in PDF engine, 2x PNG per page, drag & drop, save one/all |
| Code 39 Barcode | Own encoder, module width, DPI 96/150/300, PNG/JPEG |
| PDF417 Barcode | ZXing.Net, DPI, rotation, PNG/JPEG/GIF |
| QR Code | URL / Text / WiFi / Email / vCard, error-correction H, PNG |
| Age Calculator | Years/months/days + weeks, days, hours, minutes, seconds |
| Salaried Tax | Tax years 2015-2027 (slabs copied 1:1 from your JS) |
| Business Tax | Tax years 2023-2027, AOP cap and 10% surcharge |
| Loan EMI | Sliders, presets, currencies, donut chart, year-wise schedule, report (PDF / print / copy / CSV) |
| Discount | Live savings and final price |

## Build (Visual Studio 2026 / .NET 10 SDK)
1. Open `SmartTools.sln`
2. Wait for NuGet restore (only package: `ZXing.Net`) - internet needed once
3. Press F5

Command line: `dotnet run --project SmartTools`  
Standalone EXE: run `publish.bat` -> `publish\SmartToolsSuite.exe`

## Notes
* Windows 10 (1903+) / Windows 11 only (uses Windows.Data.Pdf).
* "Save PDF" of the loan report uses the built-in *Microsoft Print to PDF* printer; if missing, Print Preview opens.
* The UI is built in code (no .Designer files) so the whole look lives in `SmartTools/UI`.
* Tax figures are estimates copied from the original tool - verify against current FBR rules.

## Branding
Logo: `SmartTools/Assets/logo.png` (sidebar + printed loan report). Icon: `SmartTools/app.ico` (exe + window). Replace these files to rebrand.
