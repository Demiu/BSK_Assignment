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
        "\tls - prints the content of \n" + 
        "\tcd - change directory \n" + 
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
            {"ls", (o) => this.FileDirectory(o)},
            {"cd", (o) => this.ChangeDirectory(o)},
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
    
    private void FileDirectory(ArraySegment<string> opts) {
        if (opts.Count > 1) {
            Console.WriteLine("Invalid number of arguments!");
            return;
        }
        if (opts.Count == 0)
        {
            connection.GetFileDirectory("");
        }
        if (opts.Count == 1)
        {
            connection.GetFileDirectory(opts[0]);
        }
    }
    
    private void ChangeDirectory(ArraySegment<string> opts) {
        if (opts.Count > 1) {
            Console.WriteLine("Invalid number of arguments!");
            return;
        }
        if (opts.Count == 1)
        {
            connection.GetChangedDirectory(opts[0]);
        }
    }

    private void PrintAesKey() {
        Console.WriteLine(Convert.ToBase64String(connection.GetAesKey()));
    }
}