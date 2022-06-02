using Lib.Defines;

namespace Lib.Messages;

public class TransferRequest : Message
{
    string path;

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