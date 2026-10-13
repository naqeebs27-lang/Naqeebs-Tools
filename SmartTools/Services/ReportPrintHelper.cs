using Avalonia;
using SmartTools.Tools;

namespace SmartTools.Services;

/// <summary>
/// "Print / Preview" for the simple reports. WinForms used PrintPreviewDialog; Avalonia has no print
/// subsystem, so the report is drawn with Skia into a preview window that can save a PDF or open it
/// in the system PDF viewer for printing (works the same on Windows and Linux).
/// </summary>
public static class ReportPrintHelper
{
    public static Task ShowPreviewAsync(Visual owner, string title, IReadOnlyList<string> lines)
    {
        return ReportPreviewWindow.ShowAsync(owner, new TextReportDocument(title, lines));
    }
}
