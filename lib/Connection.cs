using System.Net.Sockets;

namespace Lib;

public class Connection {
    TcpClient client;
    CancellationTokenSource cancelTokenSource;

    public Connection(TcpClient client, CancellationToken cancellationToken) {
        this.client = client;
        this.cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public async Task CommunicationLoop() {
        var token = cancelTokenSource.Token;
        while (!token.IsCancellationRequested) {
            byte msgKind = 0; 
            var receivedCount = await client.Client.ReceiveAsync(new[]{msgKind}, SocketFlags.None, token);
            if (receivedCount > 0) {
                // todo retrieve packet, handle packet
            }
        }
    }

    protected async Task ReceivePacket(byte kind) {
        // retrieve packet, handle packet
    }
}
