using Avalonia.Controls;

namespace SmartTools.UI;

/// <summary>Rounded drop-down selector (read-only list). Look comes from ComboBox.modern in App.axaml.</summary>
public class ModernComboBox : ComboBox
{
    protected override Type StyleKeyOverride => typeof(ComboBox);

    public event EventHandler SelectedIndexChanged;

    public ModernComboBox()
    {
        Classes.Add("modern");
        SelectionChanged += (s, e) => SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetItems(params string[] items)
    {
        ItemsSource = items;
        SelectedIndex = items.Length > 0 ? 0 : -1;
    }

    /// <summary>The selected text ("" when nothing is selected).</summary>
    public string SelectedText => SelectedItem as string ?? "";
}
