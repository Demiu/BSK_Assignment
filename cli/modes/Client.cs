using Lib;

namespace Cli.Modes;

class ClientMode : Mode
{
    protected override string prompt => $"client {currentPath} >";
    protected override string helpText => 
        "\tping [sec] - query sending a ping\n" +
        "\t             sec - encrypt the ping\n" +
        "\tsecure - attempts to secure a connection\n" +
        "\taes - prints the aes key\n" + 
        "\tls - prints the content of \n" + 
        "\tcd - change directory \n" + 
        "\taes - prints the aes key \n" + 
        "\tget path - request the file on server under path, verbatim \n" + 
        "\tencmode [mode] - get/set preferred outgoing encryption mode\n" +
        baseHelpText;
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    Connection connection;
    CancellationTokenSource ctSource;
    Dictionary<string, Action<ArraySegment<string>>> functionsVal;
    string currentPath = Lib.Defines.Constants.BASE_NET_PATH;

    public ClientMode(Connection connection, CancellationTokenSource source) {
        this.connection = connection;
        this.ctSource = source;
        this.functionsVal = new() {
            {"ping", (o) => this.SendPing(o)},
            {"secure", (_) => this.SecureConnection()},
            {"aes", (_) => this.PrintAesKey()},
            {"ls", (o) => this.ListFiles(o)},
            {"cd", (o) => this.ChangeDirectory(o)},
            {"get", (o) => this.FetchFile(o)},
            {"encmode", (o) => this.SetEncryptionMode(o)},
        };
    }

    protected override void OnExit()
    {
        ctSource.Cancel();
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

    private void ListFiles(ArraySegment<string> opts) {
        if (opts.Count == 0) {
            connection.GetFileDirectory(currentPath);
            return;
        }
        var path = String.Join(" ", opts);
        connection.GetFileDirectory(Path.Combine(currentPath, path));
    }
    
    private void ChangeDirectory(ArraySegment<string> opts) {
        // TODO improve parsing (eg. multiple ..s)
        if (opts.Count == 0) {
            Console.WriteLine("Invalid number of arguments!");
            return;
        }
        var path = String.Join(" ", opts);
        if (path == "..") {
            var segments = currentPath.Split(Path.DirectorySeparatorChar);
            currentPath = String.Join(Path.DirectorySeparatorChar, segments[1..]);
        } else if (path.StartsWith('/')) { // TODO replace '/' with a constant
            currentPath = path;
        } else {
            currentPath = Path.Join(currentPath, path);
        }
    }

    private void PrintAesKey() {
        Console.WriteLine(Convert.ToBase64String(connection.GetAesKey()));
    }

    private void FetchFile(ArraySegment<string> opts) {
        if (opts.Count == 0) {
            Console.WriteLine("You need to specify a path");
            return;
        }
        var path = String.Join(" ", opts);
        connection.RequestFile(Path.Combine(currentPath, path));
    }

    private void SetEncryptionMode(ArraySegment<string> opts) {
        if (opts.Count == 0) {
            Console.WriteLine($"Preferred encryption mode is {connection.GetPreferredEncryptionMode()}");
            return;
        } else if (opts.Count != 1) {
            Console.WriteLine("Invalid number of arguments!");
            return;
        }
        var newModeStr = opts[0];
        if (newModeStr == "cbc") {
            Console.WriteLine("Setting preferred mode to cbc");
            connection.ChangePreferredEncryptionMode(Lib.Defines.EncryptionMode.CBC);
        } else if (newModeStr == "ecb") {
            Console.WriteLine("Setting preferred mode to ecb");
            connection.ChangePreferredEncryptionMode(Lib.Defines.EncryptionMode.ECB);
        } else {
            Console.WriteLine("Unknown encryption mode, recognized modes: ecb, cbc");
        }
    }
}