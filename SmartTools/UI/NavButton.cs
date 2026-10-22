using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools.UI;

/// <summary>Sidebar navigation entry: coloured badge + title. Styled by Button.nav in App.axaml.</summary>
public class NavButton : Button
{
    private readonly Border _bar;
    private readonly Border _badgeBox;
    private readonly TextBlock _badgeText;
    private readonly TextBlock _title;
    private bool _selected;

    protected override Type StyleKeyOverride => typeof(Button);

    public NavButton()
    {
        Classes.Add("nav");

        _bar = new Border
        {
            Width = 3,
            Height = 24,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(ThemePalette.Accent),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false
        };

        _badgeText = new TextBlock
        {
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _badgeBox = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(14, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = _badgeText
        };

        _title = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 16, 0)
        };
        Grid.SetColumn(_title, 1);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(_bar);
        Grid.SetColumnSpan(_bar, 2);
        grid.Children.Add(_badgeBox);
        grid.Children.Add(_title);
        Content = grid;
    }

    public string Title
    {
        get => _title.Text ?? "";
        set => _title.Text = value;
    }

    public string Badge
    {
        get => _badgeText.Text ?? "";
        set
        {
            _badgeText.Text = value;
            _badgeText.FontSize = (value ?? "").Length > 2 ? 9 : 12;
        }
    }

    public Color BadgeColor
    {
        get => (_badgeBox.Background as ISolidColorBrush)?.Color ?? ThemePalette.Accent;
        set => _badgeBox.Background = new SolidColorBrush(value);
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            this.SetClass("selected", value);
            _bar.IsVisible = value;
        }
    }
}
