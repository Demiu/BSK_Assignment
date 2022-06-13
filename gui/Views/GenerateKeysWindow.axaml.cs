using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gui.Views;

public partial class GenerateKeysWindow : Window
{
    public GenerateKeysWindow() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}