using Lib;

namespace Cli.Modes;

class ClientMode : Mode
{
    protected override string prompt => "client>";
    protected override string helpText => 
        "\tping - query sending a ping\n" +
        "\tsecure - attempts to secure a connection\n" +
        "\taes - prints the aes key" + 
        baseHelpText;
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    private Connection connection;
    private Dictionary<string, Action<ArraySegment<string>>> functionsVal;

    public ClientMode(Connection connection) {
        this.connection = connection;
        functionsVal = new() {
            {"ping", (_) => this.SendPing()},
            {"secure", (_) => this.SecureConnection()},
            {"aes", (_) => this.PrintAesKey()},
        };
    }

    private void SendPing() {
        connection.SendPing();
    }

    private void SecureConnection() {
        connection.AttemptSecuringConnection();
    }

    private void PrintAesKey() {
        Console.WriteLine(Convert.ToBase64String(connection.GetAesKey()));
    }
}