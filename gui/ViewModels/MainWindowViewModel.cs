using ReactiveUI;

namespace Gui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    string ipString = "";
    public string IpString {
        get => ipString;
        set => this.RaiseAndSetIfChanged(ref ipString, value);
    }
    public string IpWatermark => Lib.Defines.Constants.DEFAULT_ADDRESS.ToString();

    string portString = "";
    public string PortString {
        get => portString;
        set => this.RaiseAndSetIfChanged(ref portString, value);
    }
    public string PortWatermark => Lib.Defines.Constants.DEFAULT_PORT.ToString();

    private string[] extensions = {"txt", "pdf", "png", "avi"};
    private string[] extensionsName = {"All filtered files"};
    private string whitelistFilename;
    private string publicKeyFilename;
    private string privateKeyFilename;
}
    