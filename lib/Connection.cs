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
            HandleMessage((dynamic) await message); // TODO replace dynamic dispatch with single
        }
    }

    public void SendPing() {
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(() => SendMessage(new Ping()));
    }

    public void AttemptSecuringConnection() {
        var token = cancelTokenSource.Token;
        // TODO early return securityAgent.CanStartSecuring()
        Util.TaskRunSafe(async () => {
            if (securityAgent.CanStartSecuring() && await securityAgent.StartSecuring(token)) {
                var pub = securityAgent.GetPubRsaKey();
                await SendMessage(new SecureRequest(pub));
            }
        });
    }

    protected async Task<Message> ReceiveMessage(byte bkind) {
        var kind = MessageKindMethods.FromByte(bkind);
        var stream = client.GetStream();
        //Console.WriteLine($"Received: {kind}"); // TODO add to logging
        return kind switch
        {
            MessageKind.Ping => await Ping.Deserialize(stream),
            MessageKind.Pong => await Pong.Deserialize(stream),
            MessageKind.SecureRequest => await SecureRequest.Deserialize(stream),
            MessageKind.SecureAccept => await SecureAccept.Deserialize(stream),
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
            if (!securityAgent.CanAcceptSecuring()) {
                await Console.Out.WriteLineAsync("Rejected: Can't start securing");
                return;
            }
            if (!await securityAgent.AcceptSecuring(msg.publicKey, token)) {
                await Task.WhenAll(
                    Console.Out.WriteLineAsync("Rejected: null return from AcceptSecuring"),
                    SendMessage(new SecureReject())
                );
                return;
            }
            await Task.WhenAll(
                Console.Out.WriteLineAsync("Accepted"),
                SendMessage(new SecureAccept(msg.publicKey, securityAgent.GetAesKey()))
            );
        });
    }

    protected void HandleMessage(SecureAccept msg) {
        Console.WriteLine("Received SecureAccept");
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(async () => {
            if (!securityAgent.CanFinishSecuring()) {
                await Console.Out.WriteLineAsync("Cannot finish securing");
                return;
            }
            var finishOk = await securityAgent.FinishSecuring(msg.encryptedKey, token);
            Debug.Assert(finishOk);
            await Console.Out.WriteLineAsync(
                finishOk 
                ? "Finished securing" 
                : "Failed to finish securing: SecurityAgent rejection");
        });
    }
}
