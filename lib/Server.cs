using System.Net;
using System.Net.Sockets;

namespace Lib;

public class Server : IDisposable
{
    TcpListener listener;
    List<Connection> connections;
    CancellationTokenSource cancelTokenSource;

    public Server(IPEndPoint endPoint) {
        listener = new TcpListener(endPoint);
        connections = new();
        cancelTokenSource = new CancellationTokenSource();
    }

    ~Server() {
        Dispose();
    }

    public void Dispose()
    {
        cancelTokenSource.Cancel();
    }

    public async Task ListenLoop() {
        Console.WriteLine("Server::ListenLoop start");
        try {
            var token = cancelTokenSource.Token;
            listener.Start();
            while (!token.IsCancellationRequested) {
                var client = await listener.AcceptTcpClientAsync(cancelTokenSource.Token);
                HandleNewClient(client);
            }
        } finally {
            listener.Stop();
        }
    }

    private void HandleNewClient(TcpClient client) {
        var connection = new Connection(client, cancelTokenSource.Token);
        connections.Add(connection);
        Task.Run(() => connection.CommunicationLoop().ContinueWith(_ => connections.Remove(connection))); // TODO handleconnectionclosed?
    }
}
