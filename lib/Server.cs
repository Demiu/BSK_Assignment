using System.Net;
using System.Net.Sockets;

namespace Lib;

public class Server : IDisposable
{
    TcpListener listener;
    CancellationTokenSource cancelTokenSource;

    public Server(IPEndPoint endPoint) {
        listener = new TcpListener(endPoint);
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
        try {
            var token = cancelTokenSource.Token;
            listener.Start();
            while (!token.IsCancellationRequested) {
                var client = await listener.AcceptTcpClientAsync(cancelTokenSource.Token);
                await HandleClient(client);
            }
        } finally {
            listener.Stop();
        }
    }

    private async Task HandleClient(TcpClient client) {
        // TODO
    }
}
