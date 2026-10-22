using SmartTools.Services;
using SmartTools.UI;
using System.Drawing.Drawing2D;

namespace SmartTools.Tools;

public class PdfToImageTool : ToolPage
{
    private readonly ModernButton _choose = ModernButton.Primary("Choose PDF file...");
    private readonly ModernButton _saveAll = ModernButton.Success("Save all pages");
    private readonly Label _status = new Label
    {
        Text = "Choose a PDF or drag one onto this window.",
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = Theme.Body,
        ForeColor = Theme.TextMuted
    };
    private readonly FlowLayoutPanel _gallery = new FlowLayoutPanel
    {
        AutoScroll = true,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        BackColor = Theme.Background,
        Padding = new Padding(12)
    };

    private readonly List<byte[]> _pages = new List<byte[]>();
    private string _pdfName = "document";
    private bool _busy;

    public PdfToImageTool() : base("PDF to Image", "Convert every page of a PDF into a sharp PNG image (2x resolution).")
    {
        _status.Height = Theme.S(42);
        _saveAll.Enabled = false;
        _gallery.Height = Theme.S(480);

        _choose.Click += async (s, e) => await ChooseAsync();
        _saveAll.Click += (s, e) => SaveAll();

        var top = new CardPanel();
        var row = Ui.Columns(Ui.Px(200), Ui.Pct(100f), Ui.Px(170));
        row.AddCell(_choose, 0, 0, 14);
        row.AddCell(_status, 1, 0, 14);
        row.AddCell(_saveAll, 2, 0, 0);
        top.AddRow(row);

        var galleryCard = new CardPanel();
        galleryCard.AddHeading("Pages");
        galleryCard.AddRow(_gallery, 6, 0);

        Stack.AddRow(top, 0, 18);
        Stack.AddRow(galleryCard);

        AllowDrop = true;
        HookDrop(this);
        HookDrop(_gallery);
        HookDrop(top);
    }

    private void HookDrop(Control c)
    {
        c.AllowDrop = true;
        c.DragEnter += (s, e) =>
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        };
        c.DragDrop += async (s, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0 &&
                files[0].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                await LoadPdfAsync(files[0]);
            }
        };
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_gallery != null)
        {
            _gallery.Height = Math.Max(Theme.S(340), ClientSize.Height - Theme.S(270));
        }
    }

    private async Task ChooseAsync()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Choose a PDF",
            Filter = "PDF files (*.pdf)|*.pdf"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await LoadPdfAsync(dlg.FileName);
        }
    }

    private async Task LoadPdfAsync(string path)
    {
        if (_busy) return;
        _busy = true;
        _choose.Enabled = false;
        _saveAll.Enabled = false;

        try
        {
            ClearGallery();
            _pdfName = Path.GetFileNameWithoutExtension(path);
            _status.Text = "Processing... please wait.";

            var doc = await PdfPageRenderer.OpenAsync(path);
            uint count = doc.PageCount;

            for (uint i = 0; i < count; i++)
            {
                _status.Text = "Rendering page " + (i + 1) + " of " + count + "...";
                byte[] png = await PdfPageRenderer.RenderPageAsync(doc, i, 2.0);
                _pages.Add(png);
                _gallery.Controls.Add(BuildPageCard((int)i + 1, png));
            }

            _status.Text = "Conversion complete! " + count + (count == 1 ? " page." : " pages.");
            _saveAll.Enabled = _pages.Count > 0;
        }
        catch (Exception ex)
        {
            _status.Text = "Could not convert this PDF.";
            MessageBox.Show(this, "The PDF could not be opened. It may be damaged or password protected.\n\n" + ex.Message,
                "Smart Tools Suite", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _busy = false;
            _choose.Enabled = true;
        }
    }

    private void ClearGallery()
    {
        foreach (Control c in _gallery.Controls.Cast<Control>().ToList())
        {
            _gallery.Controls.Remove(c);
            foreach (var pb in c.Controls.OfType<PictureBox>())
            {
                pb.Image?.Dispose();
                pb.Image = null;
            }
            c.Dispose();
        }
        _pages.Clear();
    }

    private Control BuildPageCard(int number, byte[] png)
    {
        Bitmap thumb;
        using (var ms = new MemoryStream(png))
        using (var src = new Bitmap(ms))
        {
            thumb = MakeThumbnail(src, Theme.S(420), Theme.S(560));
        }

        var card = new CardPanel
        {
            AutoSize = false,
            Size = new Size(Theme.S(240), Theme.S(400)),
            Padding = Theme.P(14),
            Margin = new Padding(0, 0, Theme.S(14), Theme.S(14))
        };

        var pic = new PictureBox
        {
            Image = thumb,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Theme.Background,
            Height = Theme.S(280)
        };
        var label = Ui.Text("Page " + number, Theme.BodyBold);
        var save = ModernButton.Secondary("Save PNG");
        save.Height = Theme.S(38);
        save.Click += (s, e) => SavePage(number);

        card.AddRow(pic, 0, 8);
        card.AddRow(label, 0, 8);
        card.AddRow(save);
        return card;
    }

    private static Bitmap MakeThumbnail(Bitmap src, int maxW, int maxH)
    {
        double ratio = Math.Min(1.0, Math.Min((double)maxW / src.Width, (double)maxH / src.Height));
        int w = Math.Max(1, (int)(src.Width * ratio));
        int h = Math.Max(1, (int)(src.Height * ratio));
        var bmp = new Bitmap(w, h);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.White);
            g.DrawImage(src, 0, 0, w, h);
        }
        return bmp;
    }

    private void SavePage(int number)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Save page as PNG",
            FileName = _pdfName + "-page-" + number + ".png",
            Filter = "PNG image (*.png)|*.png",
            DefaultExt = "png"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllBytes(dlg.FileName, _pages[number - 1]);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not save the file:\n" + ex.Message, "Smart Tools Suite",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveAll()
    {
        if (_pages.Count == 0) return;
        using var dlg = new FolderBrowserDialog { Description = "Choose a folder for the PNG images" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                string file = Path.Combine(dlg.SelectedPath, _pdfName + "-page-" + (i + 1) + ".png");
                File.WriteAllBytes(file, _pages[i]);
            }
            _status.Text = "Saved " + _pages.Count + " image(s) to " + dlg.SelectedPath;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not save the files:\n" + ex.Message, "Smart Tools Suite",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ClearGallery();
        base.Dispose(disposing);
    }
}
