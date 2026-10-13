using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SmartTools.Services;
using SmartTools.UI;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools.Tools;

public partial class PdfToImageTool : ToolPage
{
    private readonly List<byte[]> _pages = new List<byte[]>();
    private PdfRenderDocument _doc;
    private string _pdfName = "document";
    private bool _busy;

    public PdfToImageTool()
    {
        InitializeComponent();

        ChooseButton.Click += async (s, e) => await ChooseAsync();
        SaveAllButton.Click += async (s, e) => await SaveAllAsync();

        // drag & drop a PDF anywhere on the page
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        SizeChanged += (s, e) => GalleryScroll.Height = Math.Max(340, e.NewSize.Height - 270);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        var file = e.Data.GetFiles()?.OfType<IStorageFile>().FirstOrDefault();
        if (file != null && file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            await LoadPdfAsync(file);
        }
    }

    private async Task ChooseAsync()
    {
        IStorageFile file = await Files.PickPdfAsync(this);
        if (file != null)
        {
            await LoadPdfAsync(file);
        }
    }

    private async Task LoadPdfAsync(IStorageFile file)
    {
        if (_busy) return;
        _busy = true;
        ChooseButton.IsEnabled = false;
        SaveAllButton.IsEnabled = false;

        try
        {
            ClearGallery();
            _pdfName = Path.GetFileNameWithoutExtension(file.Name);
            StatusText.Text = "Processing... please wait.";

            byte[] bytes = await Files.ReadAsync(file);
            _doc = await PdfPageRenderer.OpenAsync(bytes);
            uint count = _doc.PageCount;

            for (uint i = 0; i < count; i++)
            {
                StatusText.Text = "Rendering page " + (i + 1) + " of " + count + "...";
                byte[] png = await PdfPageRenderer.RenderPageAsync(_doc, i, 2.0);
                _pages.Add(png);
                Gallery.Children.Add(BuildPageCard((int)i + 1, png));
            }

            StatusText.Text = "Conversion complete! " + count + (count == 1 ? " page." : " pages.");
            SaveAllButton.IsEnabled = _pages.Count > 0;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not convert this PDF.";
            await Dialogs.ShowMessageAsync(this,
                "The PDF could not be opened. It may be damaged or password protected.\n\n" + ex.Message);
        }
        finally
        {
            _busy = false;
            ChooseButton.IsEnabled = true;
        }
    }

    private void ClearGallery()
    {
        foreach (Control c in Gallery.Children.ToList())
        {
            if (c is Border card && card.Child is StackPanel sp && sp.Children.Count > 0 &&
                sp.Children[0] is Border picBox && picBox.Child is Image img)
            {
                (img.Source as IDisposable)?.Dispose();
                img.Source = null;
            }
        }
        Gallery.Children.Clear();
        _pages.Clear();
        _doc?.Dispose();
        _doc = null;
    }

    private Control BuildPageCard(int number, byte[] png)
    {
        Bitmap thumb;
        using (var ms = new MemoryStream(png))
        {
            thumb = Bitmap.DecodeToWidth(ms, 420, BitmapInterpolationMode.HighQuality);
        }

        var card = new Border
        {
            Classes = { "card" },
            Width = 240,
            Height = 400,
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 14, 14)
        };

        var picBox = new Border
        {
            Background = new SolidColorBrush(ThemePalette.Background),
            Height = 280,
            Child = new Image { Source = thumb, Stretch = Stretch.Uniform }
        };
        var label = new TextBlock { Text = "Page " + number, Classes = { "bodybold" } };
        var save = new Button
        {
            Content = "Save PNG",
            Height = 38,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Classes = { "btn", "secondary" }
        };
        save.Click += async (s, e) => await SavePageAsync(number);

        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(picBox);
        stack.Children.Add(label);
        stack.Children.Add(save);
        card.Child = stack;
        return card;
    }

    private async Task SavePageAsync(int number)
    {
        try
        {
            await Files.SaveBytesAsync(this, "Save page as PNG", _pdfName + "-page-" + number + ".png",
                "png", "PNG image", _pages[number - 1]);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(this, "Could not save the file:\n" + ex.Message);
        }
    }

    private async Task SaveAllAsync()
    {
        if (_pages.Count == 0) return;

        IStorageFolder folder = await Files.PickFolderAsync(this, "Choose a folder for the PNG images");
        if (folder == null) return;

        try
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                string name = _pdfName + "-page-" + (i + 1) + ".png";
                IStorageFile target = await folder.CreateFileAsync(name);
                if (target != null)
                {
                    await Files.WriteAsync(target, _pages[i]);
                }
            }
            StatusText.Text = "Saved " + _pages.Count + " image(s) to " + (folder.TryGetLocalPath() ?? folder.Name);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(this, "Could not save the files:\n" + ex.Message);
        }
    }
}
