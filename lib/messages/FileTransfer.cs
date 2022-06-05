using Lib.Defines;

namespace Lib.Messages;

public class TransferRequest : Message
{
    public string path;

    public override MessageKind Kind => MessageKind.TransferRequest;

    public TransferRequest(string path) {
        this.path = path;
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        var path = await stream.ReadNetStringAsync(new()); // TODO cancelaltionToken
        return new TransferRequest(path);
    }

    protected override void SerializeIntoInner(Stream stream)
    {
        stream.WriteNetString(path);
    }
}

public class AnnounceTransfer : Message {
    public string path;
    public Int64 size;

    public override MessageKind Kind => MessageKind.AnnounceTransfer;

    public AnnounceTransfer(string basePath, string entryFullPath) {
        this.path = Path.GetRelativePath(basePath, entryFullPath);
        if (!this.path.StartsWith('/')) { // TODO replace '/' with a constant
            this.path = $"/{this.path}";
        }
        this.size = new FileInfo(entryFullPath).Length;
    }

    protected AnnounceTransfer(string path, Int64 size) {
        this.path = path;
        this.size = size;
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        var path = await stream.ReadNetStringAsync(new()); // TODO cancelaltionToken
        var size = await stream.ReadNetInt64Async(new());
        return new AnnounceTransfer(path, size);
    }

    protected override void SerializeIntoInner(Stream stream)
    {
        stream.WriteNetString(path);
        stream.WriteNetInt64(size);
    }
}
