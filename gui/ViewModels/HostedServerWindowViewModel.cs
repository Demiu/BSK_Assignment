using Avalonia.Threading;
using Gui.Views;

namespace Gui.ViewModels;

public class HostedServerWindowViewModel : ViewModelBase
{
    Lib.Server server;

    public HostedServerWindowViewModel(Lib.Server server) {
        this.server = server;
        server.OnNewConnection += (c) => Dispatcher.UIThread.Post(() => NewConnectionCallback(c));
    }

    public void NewConnectionCallback(Lib.Connection connection) {
        var newWindow = new ApplicationWindow {
            DataContext = new ApplicationWindowViewModel(connection),
        };
        newWindow.Show();
    }
}
