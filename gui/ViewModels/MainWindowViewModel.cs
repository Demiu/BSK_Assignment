﻿using System;
using System.Net;
using System.Threading;
using Gui.Views;
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

    bool hostJoinEnabled = true;
    public bool HostJoinEnabled {
        get => hostJoinEnabled;
        set => this.RaiseAndSetIfChanged(ref hostJoinEnabled, value);
    }

    private string[] extensions = {"txt", "pdf", "png", "avi"};
    private string[] extensionsName = {"All filtered files"};
    //private string whitelistFilename;
    //private string publicKeyFilename;
    //private string privateKeyFilename;

    public async void HostClicked(MainWindow window) {
        HostJoinEnabled = false;

        var ep = GetIPEndPoint();
        if (ep == null) {
            await Console.Out.WriteLineAsync("IP/Port not valid");
            return;
        }
        var server = new Lib.Server(ep);

        var newWindow = new HostedServerWindow {
            DataContext = new HostedServerWindowViewModel(server),
        };
        newWindow.Show();
        window.Close();

        Lib.Util.TaskRunSafe(async () => {
            try {
                await server.ListenLoop();
            }
            //catch (System.Exception) { }
            finally {
                // TODO close newWindow
            }
        });
    }

    public async void JoinClicked(MainWindow window) {
        HostJoinEnabled = false;

        var ep = GetIPEndPoint();
        if (ep == null) {
            await Console.Out.WriteLineAsync("IP/Port not valid");
            return;
        }
        var cts = new CancellationTokenSource();
        var connection = await Lib.Connection.CreateTo(ep, cts.Token);
        if (connection == null) {
            await Console.Out.WriteLineAsync("Failed to connect");
            HostJoinEnabled = true;
            return;
        }

        var newWindow = new ApplicationWindow {
            DataContext = new ApplicationWindowViewModel(connection),
        };
        newWindow.Show();
        window.Close();

        Lib.Util.TaskRunSafe(async () => {
            try {
                await connection.CommunicationLoop();
            }
            //catch (System.Exception) { }
            finally {
                // TODO close newWindow
            }
        });
    }

    protected IPAddress? GetIpAddress() {
        if (ipString == "") {
            return Lib.Defines.Constants.DEFAULT_ADDRESS;
        }
        IPAddress.TryParse(ipString, out var ip);
        return ip;
    }

    protected UInt16? GetPort() {
        if (portString == "") {
            return Lib.Defines.Constants.DEFAULT_PORT;
        }
        UInt16.TryParse(portString, out var port);
        return port;
    }

    protected IPEndPoint? GetIPEndPoint() {
        var ip = GetIpAddress();
        var port = GetPort();
        if (ip == null || port == null) {
            return null;
        }
        return new IPEndPoint((IPAddress)ip, (UInt16)port);
    }
}
    