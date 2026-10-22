using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools.UI;

/// <summary>
/// Rounded text input with optional prefix / suffix text (e.g. "PKR" or "%").
/// Same public surface as the WinForms control; the look comes from TextBox.modern in App.axaml.
/// </summary>
public class ModernTextBox : TextBox
{
    private string _prefix = "";
    private string _suffix = "";
    private bool _multiline;

    protected override Type StyleKeyOverride => typeof(TextBox);

    public bool NumericOnly { get; set; }
    public bool AllowDecimal { get; set; } = true;

    public event EventHandler EnterPressed;

    public ModernTextBox()
    {
        Classes.Add("modern");
        UpdatePadding();
    }

    public string Prefix
    {
        get => _prefix;
        set
        {
            _prefix = value ?? "";
            InnerLeftContent = _prefix.Length > 0 ? MakeAffix(_prefix, new Thickness(12, 0, 0, 0)) : null;
            UpdatePadding();
        }
    }

    public string Suffix
    {
        get => _suffix;
        set
        {
            _suffix = value ?? "";
            InnerRightContent = _suffix.Length > 0 ? MakeAffix(_suffix, new Thickness(0, 0, 12, 0)) : null;
            UpdatePadding();
        }
    }

    public string Placeholder
    {
        get => Watermark;
        set => Watermark = value ?? "";
    }

    public bool Multiline
    {
        get => _multiline;
        set
        {
            _multiline = value;
            AcceptsReturn = value;
            TextWrapping = value ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
            VerticalContentAlignment = value ? VerticalAlignment.Top : VerticalAlignment.Center;
            this.SetClass("multiline", value);
            UpdatePadding();
        }
    }

    public bool UsePasswordChar
    {
        get => PasswordChar != '\0';
        set => PasswordChar = value ? '\u25CF' : '\0';
    }

    public void SelectEnd()
    {
        int end = (Text ?? "").Length;
        CaretIndex = end;
        SelectionStart = end;
        SelectionEnd = end;
    }

    public void FocusInput()
    {
        Focus();
    }

    private static TextBlock MakeAffix(string text, Thickness margin)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = ThemePalette.TextMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = margin
        };
    }

    private void UpdatePadding()
    {
        double v = _multiline ? 10 : 0;
        Padding = new Thickness(_prefix.Length > 0 ? 8 : 12, v, _suffix.Length > 0 ? 8 : 12, v);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (NumericOnly && !string.IsNullOrEmpty(e.Text))
        {
            foreach (char ch in e.Text)
            {
                if (char.IsControl(ch) || char.IsDigit(ch)) continue;
                if (AllowDecimal && ch == '.' && !(Text ?? "").Contains('.')) continue;
                e.Handled = true;
                return;
            }
        }
        base.OnTextInput(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !AcceptsReturn)
        {
            e.Handled = true;
            EnterPressed?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.OnKeyDown(e);
    }
}
