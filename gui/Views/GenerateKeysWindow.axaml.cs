using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace gui.Views;

public partial class GenerateKeysWindow : Window
{
    public GenerateKeysWindow() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}