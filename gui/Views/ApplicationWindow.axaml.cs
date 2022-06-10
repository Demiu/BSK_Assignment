using System.ComponentModel;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using gui.ViewModels;

namespace gui.Views;

public partial class ApplicationWindow : Window
{
    private string message;
    private string filename;
    private int fileNum;
    private bool start = true;
    
    public ApplicationWindow() {
        InitializeComponent();
        DataContext = new ApplicationWindowViewModel();
        fileNum = 0;
    }

    protected void Txt_KeyUp(object sender, KeyEventArgs e)
    {
        message = MessagesText.Text;
        MessageButton.IsEnabled = message.Length > 0;
    }

    protected void Click_SendMessage(object sender, RoutedEventArgs args) {
        SendText.Text += message + "\n";
        LogsText.Text += message + "\n";
    }

    protected void strNodeText_DoubleTapped(object sender, RoutedEventArgs e) {
        
        if (start) {
            
            TextBlock textBlock = (sender as TextBlock);
            filename = textBlock.Text;

            fileNum++;
            if (fileNum > 9) {
                fileNum = 1;
            }
            
            start = false;
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;

            worker.RunWorkerAsync();
        }
    }
    
    void worker_DoWork(object sender, DoWorkEventArgs e)
    {
        //TODO: calculating % of progressbor to change and adding sending file
        for(int i = 0; i < 101; i++)
        {
            (sender as BackgroundWorker).ReportProgress(i);
            Thread.Sleep(100);
        }
    }
    
    void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        switch (fileNum)
        {
            case 1:
                ProgressBar1.Value = e.ProgressPercentage;
                File1Text.Text = filename;
                break;
            case 2:
                ProgressBar2.Value = e.ProgressPercentage;
                File2Text.Text = filename;
                break;
            case 3:
                ProgressBar3.Value = e.ProgressPercentage;
                File3Text.Text = filename;
                break;
            case 4:
                ProgressBar4.Value = e.ProgressPercentage;
                File4Text.Text = filename;
                break;
            case 5:
                ProgressBar5.Value = e.ProgressPercentage;
                File5Text.Text = filename;
                break;
            case 6:
                ProgressBar6.Value = e.ProgressPercentage;
                File6Text.Text = filename;
                break;
            case 7:
                ProgressBar7.Value = e.ProgressPercentage;
                File7Text.Text = filename;
                break;
            case 8:
                ProgressBar8.Value = e.ProgressPercentage;
                File8Text.Text = filename;
                break;
            case 9:
                ProgressBar9.Value = e.ProgressPercentage;
                File9Text.Text = filename;
                break;
            default:
                fileNum = 0;
                break;
        }
        
        if (e.ProgressPercentage >= 99) {
            start = true;
        }
    }
    

}