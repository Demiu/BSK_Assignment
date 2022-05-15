using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Lib.Defines;
using Lib.Messages;

namespace Lib;

public class Connection {
    TcpClient client;
    CancellationTokenSource cancelTokenSource;
    Crypto.SecurityAgent securityAgent;

    public Connection(TcpClient client, CancellationToken cancellationToken) {
        this.client = client;
        this.cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this.securityAgent = new();
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
        while (!token.IsCancellationRequested) {
            var msgKind = await client.GetStream().ReadExactlyAsync(1, token);
            var message = ReceiveMessage(msgKind[0]);
            HandleMessage((dynamic) await message);
        }
    }

    public void SendPing() {
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(() => SendMessage(new Ping()));
    }

    public void AttemptSecuringConnection() {
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(async () => {
            if (await securityAgent.StartSecuring(token)) {
                var pub = await securityAgent.GetPubKey(token);
                await SendMessage(new SecureRequest(pub));
            }
        });
    }

    protected async Task<Message> ReceiveMessage(byte bkind) {
        var kind = MessageKindMethods.FromByte(bkind);
        var stream = client.GetStream();
        Console.WriteLine($"Received: {kind}");
        return kind switch
        {
            MessageKind.Ping => await Ping.Deserialize(stream),
            MessageKind.Pong => await Pong.Deserialize(stream),
            MessageKind.SecureRequest => await SecureRequest.Deserialize(stream),
            _ => throw new UnexpectedEnumValueException<MessageKind,byte>(bkind),
        };
    }

    protected async Task SendMessage(Message msg) {
        var token = cancelTokenSource.Token;
        var serialized = msg.Serialize();
        var sent = await client.Client.SendAsync(serialized, SocketFlags.None, token);
        Debug.Assert(sent == serialized.Length);
    }

    protected void HandleMessage(Message msg) {
        Console.WriteLine($"Unhandled message of kind {msg.Kind}");
    }

    protected void HandleMessage(Ping msg) {
        Console.WriteLine("Received Ping, sending Pong");
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(() => SendMessage(new Pong()));
    }   

    protected void HandleMessage(Pong msg) {
        Console.WriteLine("Received Pong");
    }

    protected void HandleMessage(SecureRequest msg) {
        Console.WriteLine("Received SecureRequest");
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(async () => {
            var keyIvTuple = await securityAgent.AcceptSecuring(msg.publicKey, token);
            if (keyIvTuple == null) {
                await Console.Out.WriteLineAsync("Rejected");
                await SendMessage(new SecureReject());
                return;
            }
            await Console.Out.WriteLineAsync("Accepted");
            var (keyEnc, ivEnc) = keyIvTuple.Value;
            await SendMessage(new SecureAccept(keyEnc, ivEnc));
        });
    }
}
