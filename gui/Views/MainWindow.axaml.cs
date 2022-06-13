using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gui.Views;

public partial class MainWindow : Window
{

    public MainWindow() {
        InitializeComponent();
    }

    /*protected void EnableButtons() {
        if (!string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(PublicKeyFile.Text) && !string.IsNullOrEmpty(PrivateKeyFile.Text)){
            HostButton.IsEnabled = true;
            JoinButton.IsEnabled = true;
        }
        else {
            HostButton.IsEnabled = false;
            JoinButton.IsEnabled = false;     
        }
    }

    protected void Txt_KeyUpIp(object sender, KeyEventArgs e) {
        ip = TextIp.Text;
        EnableButtons();
    }

    protected void Txt_KeyUpPort(object sender, KeyEventArgs e) {
        port = TextPort.Text;
        EnableButtons();
    }
    
    private void Click_ChooseWhitelistButton(object sender, RoutedEventArgs args) {
        WhitelistButton.IsEnabled = true;
    }

    private async void Click_WhitelistButton(object sender, RoutedEventArgs args) {
        var dialog = new OpenFileDialog();
        var allFileFilter = new FileDialogFilter { Name = extensionsName[0] };
        allFileFilter.Extensions = new List<string> { extensions[0], extensions[1], extensions[2],extensions[3]};
        dialog.Filters = new List<FileDialogFilter> {allFileFilter};
    
        var path = await dialog.ShowAsync(this);

        string resultPath = path[0];
        string[] foldersFileNames = resultPath.Split('/', '\\');
        whitelistFilename = foldersFileNames[^1];
    
        WhitelistDirectory.Text = $"Chosen file:\n {whitelistFilename}";
    }

    private void Click_GenerateKeys(object sender, RoutedEventArgs args) {
        var generateKeysWindow = new GenerateKeysWindow();
        generateKeysWindow.Show();
    }
    
    private async void Click_PublicKeyButton(object sender, RoutedEventArgs args) {
        var dialog = new OpenFileDialog();
        var allFileFilter = new FileDialogFilter { Name = extensionsName[0] };
        allFileFilter.Extensions = new List<string> { extensions[0], extensions[1], extensions[2],extensions[3]};
        dialog.Filters = new List<FileDialogFilter> {allFileFilter};
    
        var path = await dialog.ShowAsync(this);

        string resultPath = path[0];
        string[] foldersFileNames = resultPath.Split('/', '\\');
        publicKeyFilename = foldersFileNames[^1];
    
        PublicKeyFile.Text = $"Chosen public key:\n {publicKeyFilename}";
        EnableButtons();
    }
    
    private async void Click_PrivateKeyButton(object sender, RoutedEventArgs args) {
        var dialog = new OpenFileDialog();
        var allFileFilter = new FileDialogFilter { Name = extensionsName[0] };
        allFileFilter.Extensions = new List<string> { extensions[0], extensions[1], extensions[2],extensions[3]};
        dialog.Filters = new List<FileDialogFilter> {allFileFilter};
    
        var path = await dialog.ShowAsync(this);

        string resultPath = path[0];
        string[] foldersFileNames = resultPath.Split('/', '\\');
        privateKeyFilename = foldersFileNames[^1];
    
        PrivateKeyFile.Text = $"Chosen private key:\n {privateKeyFilename}";
        EnableButtons();
    }
    
    private void Click_Host(object sender, RoutedEventArgs args) {
        var hostedServerWindow = new HostedServerWindow();
        hostedServerWindow.Show();
        Close();
    }

    private void Click_Join(object sender, RoutedEventArgs args) {
        ip = TextIp.Text;
        port = TextPort.Text;

        var applicationWindow = new ApplicationWindow();
        applicationWindow.Show();
        Close();
    }*/
}
