using Avalonia;
using Avalonia.Controls;

namespace SmartTools.UI;

/// <summary>
/// Base class for every tool screen: title, subtitle and scrollable content.
/// The look comes from the ui|ToolPage template in App.axaml. In XAML:
/// &lt;ui:ToolPage Title="..." Subtitle="..."&gt; ...content... &lt;/ui:ToolPage&gt;
/// </summary>
public class ToolPage : ContentControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ToolPage, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<ToolPage, string>(nameof(Subtitle), string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    // Every derived page (DiscountTool, QrCodeTool, ...) must use the ToolPage template.
    protected override Type StyleKeyOverride => typeof(ToolPage);

    /// <summary>Information box (was MessageBox.Show in the WinForms version).</summary>
    protected Task Warn(string message)
    {
        return Dialogs.ShowMessageAsync(this, message);
    }
}
