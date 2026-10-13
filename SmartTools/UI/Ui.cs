using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SmartTools.UI;

/// <summary>Small helpers for the few places where the UI is still assembled in code.</summary>
public static class Ui
{
    public static TextBlock Caption(string text)
    {
        return new TextBlock { Text = text, Classes = { "caption" } };
    }

    /// <summary>Caption label followed by an input control (top/bottom spacing in DIPs).</summary>
    public static void AddField(this Panel panel, string caption, Control input, double top = 14)
    {
        var cap = Caption(caption);
        cap.Margin = new Avalonia.Thickness(0, top, 0, 6);
        input.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Children.Add(cap);
        panel.Children.Add(input);
    }

    /// <summary>Adds or removes a style class (used for the "selected / on" states).</summary>
    public static void SetClass(this StyledElement element, string name, bool on)
    {
        if (on)
        {
            if (!element.Classes.Contains(name)) element.Classes.Add(name);
        }
        else
        {
            element.Classes.Remove(name);
        }
    }
}
