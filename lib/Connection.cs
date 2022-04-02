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
            null => throw new UnexpectedEnumValueException<MessageKind,byte>(bkind),
        };
    }

    protected async Task HandleMessage(Message msg) {
        // TODO print an error about unhandled type
        return;
    }

    protected async Task HandleMessage(Ping msg) {
        return;
    }
}
