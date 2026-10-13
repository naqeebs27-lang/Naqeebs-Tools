using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

/// <summary>Modal preview of a report (replacement for PrintPreviewDialog): save as PDF or open it to print.</summary>
public partial class ReportPreviewWindow : Window
{
    private readonly ReportDocument _doc;
    private readonly DispatcherTimer _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3500) };

    public ReportPreviewWindow()
    {
        InitializeComponent();
    }

    public ReportPreviewWindow(ReportDocument doc) : this()
    {
        _doc = doc;
        Title = doc.Title + " - preview";

        _toastTimer.Tick += (s, e) => { ToastText.Text = ""; _toastTimer.Stop(); };
        SavePdfButton.Click += async (s, e) => await SavePdfAsync();
        OpenButton.Click += async (s, e) => await OpenAsync();
        CloseButton.Click += (s, e) => Close();

        BuildPages();
    }

    public static async Task ShowAsync(Visual owner, ReportDocument doc)
    {
        var window = new ReportPreviewWindow(doc);
        Window parent = Dialogs.WindowOf(owner);
        if (parent != null)
        {
            await window.ShowDialog(parent);
        }
        else
        {
            window.Show();
        }
    }

    private void BuildPages()
    {
        foreach (var bmp in ReportExport.RenderPages(_doc, 1.5f))
        {
            var raster = new RasterImage(bmp);
            var image = new Image
            {
                Source = raster.ToAvalonia(),
                Width = 700,
                Stretch = Stretch.Uniform
            };
            raster.Dispose();

            PagesPanel.Children.Add(new Border
            {
                Background = Brushes.White,
                BoxShadow = BoxShadows.Parse("0 2 8 0 #33000000"),
                Child = image
            });
        }
    }

    private void Toast(string message)
    {
        ToastText.Text = message;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private async Task SavePdfAsync()
    {
        try
        {
            byte[] pdf = ReportExport.ToPdfBytes(_doc);
            bool saved = await Files.SaveBytesAsync(this, "Save report as PDF", _doc.FileName + ".pdf",
                "pdf", "PDF document", pdf);
            if (saved) Toast("PDF saved successfully!");
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(this, "PDF could not be created:\n" + ex.Message);
        }
    }

    private async Task OpenAsync()
    {
        try
        {
            ReportExport.OpenInViewer(_doc);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(this, "The PDF could not be opened:\n" + ex.Message);
        }
    }
}
