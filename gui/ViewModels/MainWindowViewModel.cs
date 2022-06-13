namespace Gui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string ipString = "";
    //private static readonly string ipWatermark = "0.0.0.0";//Lib.Defines.Constants.DEFAULT_ADDRESS.ToString();
    public string IpWatermark {
        get => "lol";
        set {
            ipString = value;
        }
    }
    private string port;

    private string[] extensions = {"txt", "pdf", "png", "avi"};
    private string[] extensionsName = {"All filtered files"};
    private string whitelistFilename;
    private string publicKeyFilename;
    private string privateKeyFilename;
}
    