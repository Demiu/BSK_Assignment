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

    public static async Task<Connection?> CreateTo(IPEndPoint destination, CancellationToken cancellationToken) {
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
        var msgKind = new byte[1];
        while (!token.IsCancellationRequested) {
            var receivedCount = await client.Client.ReceiveAsync(msgKind, SocketFlags.None, token);
            if (receivedCount > 0) {
                var message = ReceiveMessage(msgKind[0]);
                HandleMessage((dynamic) await message);
            }
        }
    }

    public void SendPing() {
        var token = cancelTokenSource.Token;
        Task.Run(() => client.Client.SendAsync(new Ping().Serialize(), SocketFlags.None, token));
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
        Console.Error.WriteLine($"Unhandled message of kind {msg.Kind}");
    }

    protected void HandleMessage(Ping msg) {
        Console.WriteLine("Received Ping, sending Pong");
        var token = cancelTokenSource.Token;
        Task.Run(() => client.Client.SendAsync(new Pong().Serialize(), SocketFlags.None, token));
    }

    protected void HandleMessage(Pong msg) {
        Console.WriteLine("Received Pong");
    }
}