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
        try {
            await client.ConnectAsync(destination);
        }
        catch (System.Exception e) {
            await Console.Out.WriteLineAsync($"Connection.CreateTo ConnectAsync fail: {e}");
            return null;
        }
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
            if (securityAgent.CanStartSecuring()) {
                var msg = await securityAgent.StartSecuring(token);
                if (msg != null) {
                    await SendMessage(msg);
                }
            }
        });
    }

    public void ChangePreferredEncryptionMode(EncryptionMode newPreferred) {
        securityAgent.preferredMode = newPreferred;
    }
    
    public void GetFileDirectory(string path) {
        Util.TaskRunSafe(() => SendMessage(new DirectoryRequest(path)));
    }

    public void RequestFile(string path) {
        Util.TaskRunSafe(() => SendMessage(new TransferRequest(path)));
    }

    public EncryptionMode GetPreferredEncryptionMode() => securityAgent.preferredMode;
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
            MessageKind.SecureFinalize => await SecureFinalize.Deserialize(stream),
            MessageKind.SecureReject => await SecureReject.Deserialize(stream),
            MessageKind.SecuredMessageCBC => await SecuredMessageCBC.Deserialize(stream),
            MessageKind.SecuredMessageECB => await SecuredMessageECB.Deserialize(stream),
            MessageKind.DirectoryRequest => await DirectoryRequest.Deserialize(stream),
            MessageKind.AnnounceDirectoryEntry => await AnnounceDirectoryEntry.Deserialize(stream),
            MessageKind.TransferRequest => await TransferRequest.Deserialize(stream),
            MessageKind.AnnounceTransfer => await AnnounceTransfer.Deserialize(stream),
            MessageKind.TransferChunk => await TransferChunk.Deserialize(stream),
            _ => throw new UnexpectedEnumValueException<MessageKind,byte>(bkind),
        };
    }

    protected async Task SendMessage(Message msg) {
        // TODO if secured prefer 
        if (securityAgent.IsSecured) {
            await SendMessageSecured(msg);
        } else {
            // TODO deny sending some messages if we're not secured
            await SendMessageUnsecured(msg);
        }
    }

    protected async Task SendMessageUnsecured(Message msg) {
        var token = cancelTokenSource.Token;
        var serialized = msg.Serialized();
        var sent = await client.Client.SendAsync(serialized, SocketFlags.None, token);
        Debug.Assert(sent == serialized.Length);
    }

    protected async Task SendMessageSecured(Message msg) {
        if (msg is SecuredMessageCBC || msg is SecuredMessageECB) {
            await Task.WhenAll(
                Console.Out.WriteLineAsync(
                    "Attmpted to secure send a message that's already a SecuredMessageCBC/SecuredMessageECB"),
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
            var outMsg = await securityAgent.AcceptSecuring(msg, token);

            string logResult;
            if (outMsg is SecureReject) {
                var outMsgRej = (SecureReject)outMsg;
                logResult = $"Rejected securing: {outMsgRej.Reason.ToString()}";
            } else if (outMsg is SecureAccept) {
                logResult = "Accepted securing";
            } else { 
                throw new InvalidOperationException("SecurityAgent.AcceptSecuring returned an unknown type");
            }

            await Task.WhenAll(
                Console.Out.WriteLineAsync(logResult),
                SendMessageUnsecured(outMsg)
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
            var outMsg = await securityAgent.FinishSecuring(msg, token);

            string logResult;
            if (outMsg is SecureReject) {
                var outMsgRej = (SecureReject)outMsg;
                logResult = $"Rejected securing (finalize): {outMsgRej.Reason.ToString()}";
            } else if (outMsg is SecureFinalize) {
                logResult = "Accepted securing (finalize)";
            } else { 
                throw new InvalidOperationException("SecurityAgent.FinishSecuring returned an unknown type");
            }
            
            await Task.WhenAll(
                Console.Out.WriteLineAsync(logResult),
                SendMessageUnsecured(outMsg)
            );
        });
    }

    protected void HandleMessage(SecureFinalize msg) {
        Console.WriteLine("Received SecureFinalize");
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(async () => {
            var finalizeOk = await securityAgent.FinalizeSecuring(msg, token);
            Debug.Assert(finalizeOk);
            await Console.Out.WriteAsync(
                finalizeOk 
                ? "Finalized securing"
                : "Failed to finalize securing"
            );
        });
    }

    protected void HandleMessage(SecureReject msg) {
        Console.WriteLine("Received SecureReject");
        var token = cancelTokenSource.Token;
        Util.TaskRunSafe(async () => {
            await securityAgent.CancelSecuring(token);
            await Console.Out.WriteLineAsync($"Failed to secure connection, reason: {msg.Reason}");
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
    
    protected void HandleMessage(SecuredMessageCBC msg) {
        Console.WriteLine("Received SecuredMessageCBC");
        var token = cancelTokenSource.Token;
        var key = securityAgent.GetAesKey();
        if (key != null) {
            Action<Stream> syncRecv = (s) => ReceiveMessageFrom(s, token).Wait();
            msg.FeedNestedTo(syncRecv, key);
        } else {
            Console.WriteLine("Received secured message, but no key was established!");
        }
    }
    
    protected void HandleMessage(SecuredMessageECB msg) {
        Console.WriteLine("Received SecuredMessageECB");
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
