using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace SmartTools.UI;

/// <summary>Message box + file dialog helpers (Avalonia has no built-in MessageBox / SaveFileDialog).</summary>
public static class Dialogs
{
    public static Window WindowOf(Visual visual)
    {
        if (visual is Window w) return w;
        return visual == null ? null : TopLevel.GetTopLevel(visual) as Window;
    }

    /// <summary>Modal information box with an OK button.</summary>
    public static async Task ShowMessageAsync(Visual owner, string message, string title = "Smart Tools Suite")
    {
        Window ownerWindow = WindowOf(owner);

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 460
        };
        var ok = new Button
        {
            Content = "OK",
            MinWidth = 96,
            HorizontalAlignment = HorizontalAlignment.Right,
            Classes = { "btn", "primary" }
        };

        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 18 };
        panel.Children.Add(text);
        panel.Children.Add(ok);

        var win = new Window
        {
            Title = title,
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = ownerWindow != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
        };
        ok.Click += (s, e) => win.Close();

        if (ownerWindow != null)
        {
            await win.ShowDialog(ownerWindow);
        }
        else
        {
            win.Show();
        }
    }
}

/// <summary>Open / save helpers on top of Avalonia's storage provider (works on Windows and Linux).</summary>
public static class Files
{
    public static async Task<bool> SaveBytesAsync(Visual owner, string title, string suggestedName,
        string extension, string typeLabel, byte[] data)
    {
        var top = TopLevel.GetTopLevel(owner);
        if (top == null) return false;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(typeLabel) { Patterns = new[] { "*." + extension } }
            }
        });
        if (file == null) return false;

        await WriteAsync(file, data);
        return true;
    }

    public static async Task WriteAsync(IStorageFile file, byte[] data)
    {
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(data, 0, data.Length);
        try { stream.SetLength(data.Length); } catch { /* stream not seekable - nothing to trim */ }
        await stream.FlushAsync();
    }

    public static async Task<byte[]> ReadAsync(IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    /// <summary>Shows an Open dialog for a PDF. Returns null when cancelled.</summary>
    public static async Task<IStorageFile> PickPdfAsync(Visual owner)
    {
        var top = TopLevel.GetTopLevel(owner);
        if (top == null) return null;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a PDF",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PDF files") { Patterns = new[] { "*.pdf" } }
            }
        });
        return files.Count > 0 ? files[0] : null;
    }

    /// <summary>Shows a folder chooser. Returns null when cancelled.</summary>
    public static async Task<IStorageFolder> PickFolderAsync(Visual owner, string title)
    {
        var top = TopLevel.GetTopLevel(owner);
        if (top == null) return null;

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });
        return folders.Count > 0 ? folders[0] : null;
    }
}
