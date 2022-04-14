using System.Net;
using System.Net.Sockets;
using Lib.Defines;
using Lib.Messages;

namespace Lib;

public class Connection {
    TcpClient client;
    CancellationTokenSource cancelTokenSource;

    public Connection(TcpClient client, CancellationToken cancellationToken) {
        this.client = client;
        this.cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public async Task<Connection?> CreateTo(IPEndPoint destination, CancellationToken cancellationToken) {
        var client = new TcpClient();
        await client.ConnectAsync(destination);
        if (client.Connected) {
            return new Connection(client, cancellationToken);
        } else {
            return null;
        }
    }

    public async Task CommunicationLoop() {
        var token = cancelTokenSource.Token;
        while (!token.IsCancellationRequested) {
            byte msgKind = 0; 
            var receivedCount = await client.Client.ReceiveAsync(new[]{msgKind}, SocketFlags.None, token);
            if (receivedCount > 0) {
                var message = ReceiveMessage(msgKind);
                HandleMessage((dynamic) await message);
            }
        }
    }

    protected async Task<Message> ReceiveMessage(byte bkind) {
        var kind = MessageKindMethods.FromByte(bkind);
        return kind switch
        {
            MessageKind.Ping => await Ping.Deserialize(client.GetStream()),
            MessageKind.Pong => await Pong.Deserialize(client.GetStream()),
            _ => throw new UnexpectedEnumValueException<MessageKind,byte>(bkind),
        };
    }

    protected void HandleMessage(Message msg) {
        // TODO print an error about unhandled type
        return;
    }

    protected void HandleMessage(Ping msg) {
        var token = cancelTokenSource.Token;
        Task.Run(() => client.Client.SendAsync(new Pong().Serialize(), SocketFlags.None, token));
    }
}
