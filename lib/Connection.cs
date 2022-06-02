using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Lib.Defines;
using Lib.Messages;

namespace Lib;

public class Connection {
    TcpClient client;
    CancellationTokenSource cancelTokenSource;
    Crypto.SecurityAgent securityAgent;
    
    private string _publicKey;
    private string _privateKey;
    private string _currentPath = Defines.Constants.DEFAULT_PATH;

    //bool canSendFiles; // TODO

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
        var networkStream = client.GetStream();
        while (!token.IsCancellationRequested) {
            await ReceiveMessageFrom(networkStream, token);
        }
    }

    public void CreateAndSaveEncryptedKeys()
    {
        string publicKeyPath = $"./keys/public_key";
        string privateKeyPath = $"./keys/private_key";
        
        if (KeysExists(publicKeyPath, privateKeyPath))
        {
            GetKeys(publicKeyPath, privateKeyPath);
        }
        else
        {            
            int keyBits = 2048;
            var keygen = new SshKeyGenerator.SshKeyGenerator(keyBits);
            
            // extracting only the keys
            string[] pubKeyList = keygen.ToRfcPublicKey().Split(" ");
            List<String> privKeyList = new List<string>(keygen.ToPrivateKey().Split("\n"));

            privKeyList.RemoveAt(privKeyList.Count - 1);
            privKeyList.RemoveAt(privKeyList.Count - 1);
            privKeyList.RemoveAt(0);

            string priv = String.Join("\n", privKeyList);
            string privateKey = priv.Replace("\r\n", string.Empty);

            string publicKey = pubKeyList[1];
            //
            
            File.WriteAllText(publicKeyPath, publicKey);
            File.WriteAllText(privateKeyPath, privateKey);

            _publicKey = publicKey;
            _privateKey = privateKey;
        }
    }
    
    public void GetKeys(string publicKeyPath, string privateKeyPath)
    {
        if (File.Exists(publicKeyPath))
        {
            _publicKey = File.ReadAllText(publicKeyPath);
        }

        if (File.Exists(privateKeyPath))
        {
            _privateKey = File.ReadAllText(privateKeyPath);
        }
    }
    
    public bool KeysExists(string publicKeyPath, string privateKeyPath)
    {
        return File.Exists(publicKeyPath) && File.Exists(privateKeyPath);
    }

    public void GetChangedDirectory(string path)
    {
        if (path == "..")
        {
            _currentPath = Path.GetFullPath(Path.Combine(_currentPath, @"../"));
        }
        else if (Directory.Exists(_currentPath + path) && path != "..")
        {
            _currentPath = _currentPath + path;
        }
        else
        {
            Console.WriteLine("You entered the wrong path");
        }
        Console.WriteLine(_currentPath);
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
        if (Directory.Exists(_currentPath + path))
        {
            Util.TaskRunSafe(() => SendMessage(new DirectoryRequest(path)));
        }
        else
        {
            Console.WriteLine("Wrong path");
        }
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
            _ => throw new UnexpectedEnumValueException<MessageKind,byte>(bkind),
        };
    }

    protected async Task SendMessage(Message msg) {
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
                SendMessage(msg)
            );
            return;
        }
        var wrapped = securityAgent.TrySecureMessage(msg);
        if (wrapped == null) {
            await Console.Out.WriteLineAsync("Failed to secure a message");
            return;
        }
        await SendMessage(wrapped);
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

    protected void HandleMessage(DirectoryRequest msg) {
        Console.WriteLine("Received DirectoryRequest");
        Util.TaskRunSafe(async () =>
        {
            string pathFilesDirectories = _currentPath + msg.directory;
            var contentFilesDirectories = Directory.GetFileSystemEntries(pathFilesDirectories);

            foreach (var content in contentFilesDirectories)
            {
                SendMessage(File.Exists(content)
                    ? new AnnounceDirectoryEntry(new FileInfo(content))
                    : new AnnounceDirectoryEntry(new DirectoryInfo(content)));
            }
        });
    }
    
    protected void HandleMessage(AnnounceDirectoryEntry msg) {
        Console.WriteLine("Received AnnounceDirectoryEntry");
        Console.WriteLine($"{msg.fileFolderInfo} is {msg.fileSystemType} ");
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

    //protected void HandleMessage(TransferRequest msg) {

    //}

}
