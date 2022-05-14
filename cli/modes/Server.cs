using System.Net;

namespace Cli.Modes;

class ServerMode : Mode
{
    protected override string prompt => "server>";
    protected override string helpText => 
        "\tping - query sending a ping\n" +
        baseHelpText;
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    private Lib.Server server;
    private Dictionary<string, Action<ArraySegment<string>>> functionsVal;

    public ServerMode(Lib.Server server) {
        this.server = server;
        functionsVal = new() {
            {"ping", (_) => this.SendPing()},
            //{"secure", (_) => this.SecureConnection()} // TODO
        };
    }

    private void SendPing() {
        server.PingAll();
    }

    /*private void SecureConnection() {
        server.
    }*/
}