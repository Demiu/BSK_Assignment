using Lib;

namespace Cli.Modes;

class ClientMode : Mode
{
    protected override string prompt => "client>";
    protected override string helpText => 
        "\tping - query sending a ping\n" +
        "\tsecure - attempts to secure a connection\n" +
        baseHelpText;
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    private Connection connection;
    private Dictionary<string, Action<ArraySegment<string>>> functionsVal;

    public ClientMode(Connection connection) {
        this.connection = connection;
        functionsVal = new() {
            {"ping", (_) => this.SendPing()},
            {"secure", (_) => this.SecureConnection()}
        };
    }

    private void SendPing() {
        connection.SendPing();
    }

    private void SecureConnection() {
        connection.AttemptSecuringConnection();
    }
}