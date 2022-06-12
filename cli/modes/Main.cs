using System.Net;

namespace Cli.Modes;

class MainMode : Mode
{
    protected override string prompt => ">";
    protected override string helpText => 
        "\tip <address> - sets the address\n" +
        "\tport <port> - sets the port\n" +
        "\thost - starts the server\n" +
        "\tjoin - starts the cliant\n" +
        baseHelpText;
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    private IPAddress address;
    private UInt16 port;
    private Dictionary<string, Action<ArraySegment<string>>> functionsVal;

    public MainMode() {
        address = Lib.Defines.Constants.DEFAULT_ADDRESS;
        port = Lib.Defines.Constants.DEFAULT_PORT;
        functionsVal = new() {
            {"host", (_) => StartServer()},
            {"join", (_) => StartClient()},
            {"ip", (o) => SetIp(o)},
            {"port", (o) => SetPort(o)},
        };
    }

    protected override void OnExit()
    {
        // nothing
    }

    private void SetIp(ArraySegment<string> opts) {
        if (opts.Count != 1) {
            Console.WriteLine("Invalid number of arguments!");
            return;
        }
        
        var ok = IPAddress.TryParse(opts[0], out var newAddr);
        if (ok && newAddr != null) {
            address = newAddr;
            Console.WriteLine($"New IP is: {address}");
        } else {
            Console.WriteLine($"Couldn't parse an IP from {opts[0]}");
        }
    }

    private void SetPort(ArraySegment<string> opts) {
        if (opts.Count != 1) {
            Console.WriteLine("Invalid number of arguments!");
            return;
        }

        var ok = UInt16.TryParse(opts[0], out var newPort);
        if (ok) {
            port = newPort;
            Console.WriteLine($"New port is: {port}");
        } else {
            Console.WriteLine($"Couldn't parse a port from {opts[0]}");
        }
    }

    private void StartServer() {
        var endpoint = new IPEndPoint(address, port);

        Console.WriteLine("Starting server...");
        var server = new Lib.Server(endpoint);
        Lib.Util.TaskRunSafe(() => server.ListenLoop());

        var serverMode = new ServerMode(server);
        serverMode.Run();
    }

    private void StartClient() {
        var endpoint = new IPEndPoint(address, port);
        var ct = new CancellationTokenSource();

        Console.WriteLine("Starting client...");
        var connection = Lib.Connection.CreateTo(endpoint, ct.Token).Result;
        if (connection == null) {
            Console.WriteLine("Failed to connect to server");
            return;
        }
        Lib.Util.TaskRunSafe(() => connection.CommunicationLoop());

        var clientMode = new ClientMode(connection, ct);
        clientMode.Run();
    }
}