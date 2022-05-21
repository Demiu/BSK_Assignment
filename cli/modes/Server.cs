using System.Net;

namespace Cli.Modes;

class ServerMode : Mode
{
    protected override string prompt => "server>";
    protected override string helpText => 
        "\tping - query sending a ping\n" +
        "\tpeers - list all connections\n" +
        "\taes - prints the aes key" + 
        baseHelpText;
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    private Lib.Server server;
    private Dictionary<string, Action<ArraySegment<string>>> functionsVal;

    public ServerMode(Lib.Server server) {
        this.server = server;
        functionsVal = new() {
            {"ping", (_) => this.SendPing()},
            //{"secure", (_) => this.SecureConnection()} // TODO
            {"peers", (_) => this.PrintPeers()},
            {"aes", (_) => this.PrintAesKey()},
        };
    }

    private void SendPing() {
        server.PingAll();
    }
    
    // TODO
    /*private void SecureConnection() {
        server.
    }*/

    private void PrintPeers() {
        // TODO
    }

    private void PrintAesKey() {
        var keys = server.GetAllAesKeys();
        foreach(var key in keys) {
            Console.WriteLine(Convert.ToBase64String(key));
        }
    }
}