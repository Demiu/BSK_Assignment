using System.Net;

namespace Cli.Modes;

class MainMode : Mode
{
    protected override string prompt => ">";
    protected override string helpText => throw new NotImplementedException();
    protected override Dictionary<string, Action<ArraySegment<string>>> functions => functionsVal;

    private IPAddress address;
    private UInt16 port;
    private Dictionary<string, Action<ArraySegment<string>>> functionsVal;

    public MainMode() {
        address = IPAddress.Loopback;
        port = Lib.Defines.Constants.DEFAULT_PORT;
        functionsVal = new() {
            {"host", (_) => StartServer()},
            {"join", (_) => StartClient()},
        };
    }

    private void SetIp(ArraySegment<string> opts) {
        if (opts.Count != 1) {
            Console.WriteLine("Invalid number of arguments!");
        }
        
        var ok = IPAddress.TryParse(opts[0], out IPAddress? newAddr);
        if (ok && newAddr != null) {
            address = newAddr;
            Console.WriteLine($"New IP is: {address}");
        } else {
            Console.WriteLine($"Couldn't parse and IP from {opts[0]}");
        }
    }

    private void StartServer() {
        var endpoint = new IPEndPoint(address, port);

        Console.WriteLine("Starting server...");
        var server = new Lib.Server(endpoint);
        var _ = server.ListenLoop();

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
        var _ = connection.CommunicationLoop();

        var clientMode = new ClientMode(connection);
        clientMode.Run();
    }
}