using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gui.Views;

public partial class HostedServerWindow : Window
{
    public HostedServerWindow() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}