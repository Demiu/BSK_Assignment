using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Lib.Defines;

namespace Lib.Messages;

public class DirectoryRequest : Message
{
    public string fileFolderDirectory;

    public override MessageKind Kind => Defines.MessageKind.DirectoryRequest;

    public DirectoryRequest(string fileFolderDirectory)
    {
        this.fileFolderDirectory = fileFolderDirectory;
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        var src = new CancellationTokenSource();
        var token = src.Token;
        
        var fileFolder = await stream.ReadNetIntPrefixedByteArrayAsync(token);

        return new DirectoryRequest(Encoding.ASCII.GetString(fileFolder));
    }
    
    protected override void SerializeIntoInner(Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(Encoding.ASCII.GetBytes(fileFolderDirectory));
    }
}

public class DirectoryAccept : Message
{
    public string fileFolderInfo;

    public override MessageKind Kind => Defines.MessageKind.DirectoryAccept;

    public DirectoryAccept(string fileFolderInfo) {
        this.fileFolderInfo = fileFolderInfo;
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        // TODO add token param
        var src = new CancellationTokenSource();
        var token = src.Token;

        var fileFolder = await stream.ReadNetIntPrefixedByteArrayAsync(token);

        return new DirectoryAccept(Encoding.ASCII.GetString(fileFolder));
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(Encoding.ASCII.GetBytes(fileFolderInfo));
    }
}