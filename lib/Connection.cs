using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Lib.Crypto;
using Lib.Defines;
using Lib.Fs;
using Lib.Messages;

namespace Lib;

public class Connection {
    TcpClient client;
    CancellationTokenSource cancelTokenSource;
    Crypto.SecurityAgent securityAgent;
    FileSystemAgent fsAgent;
    //bool canSendFiles; // TODO

    public Connection(TcpClient client, FileSystemAgent fsAgent, CancellationToken token)
    : this(client, fsAgent, token, new())
    { }

    public Connection(TcpClient client, FileSystemAgent fsAgent, CancellationToken token, AsymmetricContainer keyStore) {
        this.client = client;
        this.cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        this.securityAgent = new(keyStore);
        this.fsAgent = fsAgent;
    }

    public static async Task<Connection?> CreateTo(IPEndPoint destination, CancellationToken cancellationToken) {
        var client = new TcpClient();
        await client.ConnectAsync(destination);
        if (client.Connected) {
            return new Connection(client, new(), cancellationToken);
        } else {
            return null;
        }
    }

    public async Task CommunicationLoop() {
        var token = cancelTokenSource.Token;
        var networkStream = client.GetStream();
        while (!token.IsCancellationRequested) {
            await ReceiveMessageFrom(networkStream, token);
        }
    }

    public void SendPing() {
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(() => SendMessage(new Ping()));
    }

    public void SendPingSecured() {
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(() => SendMessageSecured(new Ping()));
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
    
    public void GetFileDirectory(string path) {
        Util.TaskRunSafe(() => SendMessage(new DirectoryRequest(path)));
    }

    public void RequestFile(string path) {
        Util.TaskRunSafe(() => SendMessage(new TransferRequest(path)));
    }

    public byte[]? GetAesKey() => securityAgent.GetAesKey();

    protected async Task ReceiveMessageFrom(Stream stream, CancellationToken token) {
        var message = DeserializeMessageFrom(stream, token);
        HandleMessage((dynamic) await message); // TODO replace dynamic dispatch with single
    }

    protected async Task<Message> DeserializeMessageFrom(Stream stream, CancellationToken token) {
        var bkind = (await stream.ReadExactlyAsync(1, token))[0];
        var kind = MessageKindMethods.FromByte(bkind);
        //Console.WriteLine($"Received: {kind}"); // TODO add to logging
        return kind switch
        {
            MessageKind.Ping => await Ping.Deserialize(stream),
            MessageKind.Pong => await Pong.Deserialize(stream),
            MessageKind.SecureRequest => await SecureRequest.Deserialize(stream),
            MessageKind.SecureAccept => await SecureAccept.Deserialize(stream),
            MessageKind.SecuredMessage => await SecuredMessage.Deserialize(stream),
            MessageKind.DirectoryRequest => await DirectoryRequest.Deserialize(stream),
            MessageKind.AnnounceDirectoryEntry => await AnnounceDirectoryEntry.Deserialize(stream),
            MessageKind.TransferRequest => await TransferRequest.Deserialize(stream),
            MessageKind.AnnounceTransfer => await AnnounceTransfer.Deserialize(stream),
            MessageKind.TransferChunk => await TransferChunk.Deserialize(stream),
            _ => throw new UnexpectedEnumValueException<MessageKind,byte>(bkind),
        };
    }

    protected async Task SendMessage(Message msg) {
        if (securityAgent.IsSecured) {
            await SendMessageSecured(msg);
        } else {
            // TODO deny sending some messages if we're not secured
            await SendMessageUnsecured(msg);
        }
    }

    protected async Task SendMessageUnsecured(Message msg) {
        // TODO if secured prefer 
        var token = cancelTokenSource.Token;
        var serialized = msg.Serialized();
        var sent = await client.Client.SendAsync(serialized, SocketFlags.None, token);
        Debug.Assert(sent == serialized.Length);
    }

    protected async Task SendMessageSecured(Message msg) {
        if (msg is SecuredMessage) {
            await Task.WhenAll(
                Console.Out.WriteLineAsync(
                    "Attmpted to secure send a message that's already a SecuredMessage"),
                SendMessageUnsecured(msg)
            );
            return;
        }
        var wrapped = securityAgent.TrySecureMessage(msg);
        if (wrapped == null) {
            await Console.Out.WriteLineAsync("Failed to secure a message");
            return;
        }
        await SendMessageUnsecured(wrapped);
    }

    protected void HandleMessage(Message msg) {
        Console.WriteLine($"Unhandled message of kind {msg.Kind}");
    }

    protected void HandleMessage(Ping msg) {
        Console.WriteLine("Received Ping, sending Pong");
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
                    SendMessageUnsecured(new SecureReject())
                );
                return;
            }
            await Task.WhenAll(
                Console.Out.WriteLineAsync("Accepted"),
                SendMessageUnsecured(new SecureAccept(msg.publicKey, securityAgent.GetAesKey()))
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

    protected void HandleMessage(DirectoryRequest msg) {
        Console.WriteLine("Received DirectoryRequest");
        Util.TaskRunSafe(async () => {
            await fsAgent.AnnounceDirectoryContents(msg.directory, SendMessage);
        });
    }
    
    protected void HandleMessage(AnnounceDirectoryEntry msg) {
        Console.WriteLine("Received AnnounceDirectoryEntry");
        Console.WriteLine($"{msg.entryPath} is {msg.fileSystemType} ");
    }
    
    protected void HandleMessage(SecuredMessage msg) {
        Console.WriteLine("Received SecuredMessage");
        var token = cancelTokenSource.Token;
        var key = securityAgent.GetAesKey();
        if (key != null) {
            Action<Stream> syncRecv = (s) => ReceiveMessageFrom(s, token).Wait();
            msg.FeedNestedTo(syncRecv, key);
        } else {
            Console.WriteLine("Received secured message, but no key was established!");
        }
    }

    protected void HandleMessage(TransferRequest msg) {
        Console.WriteLine("Received TransferRequest");
        Util.TaskRunSafe(async () => 
            await fsAgent.TransferPath(msg.path, SendMessage));
    }

    protected void HandleMessage(AnnounceTransfer msg) {
        Console.WriteLine("Received AnnounceTransfer");
        fsAgent.NewIncomingTransfer(msg.path, msg.size);
    }

    protected void HandleMessage(TransferChunk msg) {
        Console.WriteLine("Received TransferChunk");
        fsAgent.ReceiveTransferChunk(msg.path, msg.chunk);
    }
}
