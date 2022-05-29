using Lib;

namespace Cli.Modes;

class ClientMode : Mode
{
    protected override string prompt => "client>";
    protected override string helpText => 
        "\tping [sec] - query sending a ping\n" +
        "\t             sec - encrypt the ping\n" +
        "\tsecure - attempts to secure a connection\n" +
        "\taes - prints the aes key\n" + 
        "\tdir - prints the content of directory" + 
        baseHelpText;
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    private Connection connection;
    private Dictionary<string, Action<ArraySegment<string>>> functionsVal;

    public ClientMode(Connection connection) {
        this.connection = connection;
        functionsVal = new() {
            {"ping", (o) => this.SendPing(o)},
            {"secure", (_) => this.SecureConnection()},
            {"aes", (_) => this.PrintAesKey()},
            {"dir", (_) => this.PrintFileDirectory()},
        };
    }

    private void SendPing(ArraySegment<string> opts) {
        if (opts.Count > 1) {
            Console.WriteLine("Invalid number of arguments!");
            return;
        }
        if (opts.Count == 1) {
            if (opts[0] == "sec") {
                connection.SendPingSecured();
            } else {
                Console.WriteLine($"Invalid option ${opts[0]}");
            }
            return;
        }
        connection.SendPing();
    }

    private void SecureConnection() {
        connection.AttemptSecuringConnection();
    }
    
    private void PrintFileDirectory() {
        connection.AttemptGetFileDirectory();
    }

    private void PrintAesKey() {
        Console.WriteLine(Convert.ToBase64String(connection.GetAesKey()));
    }
}