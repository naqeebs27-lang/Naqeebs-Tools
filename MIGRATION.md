# WinForms -> Avalonia migration notes

## Status: not compiled yet
This port was written without a .NET SDK available, so it has **not been built or run**. It passed static
checks (XAML well-formed, braces balanced, every `x:Name` used in code exists in XAML, unchanged files
byte-identical) and the GIF/PNG/JPEG helper algorithms were verified separately against PIL. Expect to fix
a few compile errors on the first `dotnet build`, most likely in Avalonia/SkiaSharp API details. Packages are
pinned to Avalonia 11.2.5; if that version is unavailable, use the latest 11.x for all four Avalonia packages.

## Business logic - unchanged (byte-identical)
`Services/AgeMath.cs`, `Fmt.cs`, `LoanMath.cs`, `TaxMath.cs`, `CurrencyRates.cs`

## File mapping
| WinForms | Avalonia |
|---|---|
| `Program.cs` (`Application.Run`) | `Program.cs` (`AppBuilder`) + `App.axaml(.cs)` |
| `MainForm.cs` | `MainWindow.axaml(.cs)` |
| `Tools/*Tool.cs` (UserControl, code-only UI) | `Tools/*Tool.axaml` + `.axaml.cs` (same class names, same handlers/logic) |
| `LoanReportForm` | `Tools/LoanReportWindow.axaml(.cs)` |
| `PrintPreviewDialog` | `Tools/ReportPreviewWindow.axaml(.cs)` |
| `UI/Theme.cs` | `App.axaml` resources (+ `UI/Theme.cs` for code-side colours) |
| `UI/CardPanel` | `Border.card` style |
| `UI/ModernButton` | `Button` + classes `btn primary/success/secondary/dark` |
| `UI/ModernTextBox` | `ModernTextBox : TextBox` (Prefix/Suffix via Inner content) |
| `UI/ModernComboBox` | `ModernComboBox : ComboBox` |
| `UI/ModernSlider`, `DonutChart` | custom-drawn Avalonia controls, same API |
| `UI/NavButton` | `NavButton : Button` |
| `UI/ScheduleGrid` (DataGridView) | `ScheduleGrid` (Avalonia `DataGrid`) |
| `UI/ToolPage`, `Ui` (TableLayoutPanel helpers) | `ToolPage` template in App.axaml; XAML `Grid`/`StackPanel` |
| `MessageBox`, Open/Save/Folder dialogs | `Dialogs` / `Files` helpers (Avalonia StorageProvider) |

## Replaced because they are Windows-only
| Was | Now | Visible difference |
|---|---|---|
| `System.Drawing.Bitmap`/GDI+ in `Code39`, `MatrixRenderer`, `ImageExport` | SkiaSharp (`RasterImage`), own GIF encoder, DPI written into PNG/JPEG | Code 39 caption uses Consolas on Windows, DejaVu/Liberation Mono on Linux |
| `Windows.Data.Pdf` (`PdfPageRenderer`) | PDFium via `Docnet.Core` | Same output size (2x). Page rendering is PDFium's, not Windows'. |
| `PrintDocument` / `PrintPreviewDialog` / "Microsoft Print to PDF" | Skia report drawn to PDF + preview window | "Save PDF" writes the PDF directly (no printer needed). "Print" opens the preview; "Open in PDF viewer / Print" hands the PDF to the system viewer. |
| `ReportPrintHelper`, age report, loan report layouts | Same layouts re-drawn with Skia (`ReportRendering.cs`, `Tools/LoanReport.cs`) | Fonts fall back from Segoe UI to Inter/Noto/DejaVu on Linux |

## Small behaviour differences
* Date pickers: the WinForms check boxes are replaced by an empty `DatePicker` (date of birth) and an explicit
  check box (currency comparison date). Dates are clamped to 1999-01-04 .. today in code.
* Message boxes are async dialogs; handlers `await` them or discard with `_ =` where the method just returns.
* Pixel/DPI scaling helpers (`Theme.S`) are gone - Avalonia is resolution independent.
* Windows `.csproj`: `net10.0-windows10.0.19041.0` + `UseWindowsForms` -> `net8.0` + Avalonia 11.
  Packages kept: ZXing.Net, UglyToad.PdfPig. Added: Avalonia (+Desktop, Fluent theme, DataGrid), Docnet.Core.
* Fonts: UI asks for "Segoe UI, Inter, Noto Sans, DejaVu Sans, ..." so each OS uses its best match.
