using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Lib.Defines;

namespace Lib.Messages;

public class DirectoryRequest : Message
{
    public string directory;

    public override MessageKind Kind => MessageKind.DirectoryRequest;

    public DirectoryRequest(string directory)
    {
        this.directory = directory;
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        var src = new CancellationTokenSource();
        var token = src.Token;
        
        var fileFolder = await stream.ReadNetStringAsync(token);

        return new DirectoryRequest(fileFolder);
    }
    
    protected override void SerializeIntoInner(Stream stream)
    {
        stream.WriteNetString(directory);
    }
}

public class AnnounceDirectoryEntry : Message
{
    public FileSystemInfo fileFolderInfo;
    public FileSystemType fileSystemType;

    public override MessageKind Kind => MessageKind.AnnounceDirectoryEntry;


    public AnnounceDirectoryEntry(FileSystemInfo fileFolderInfo)
    {
        this.fileFolderInfo = fileFolderInfo;

        fileSystemType = (fileFolderInfo.Attributes & FileAttributes.Directory) == 
                         FileAttributes.Directory ? FileSystemType.Directory : FileSystemType.File;
    }
    
    public static async Task<Message> Deserialize(Stream stream)
    {
        // TODO add token param
        var src = new CancellationTokenSource();
        var token = src.Token;
        
        string fileFolder = await stream.ReadNetStringAsync(token);

        if (File.Exists(fileFolder))
        {
            return new AnnounceDirectoryEntry(new FileInfo(fileFolder));
        }
        return new AnnounceDirectoryEntry(new DirectoryInfo(fileFolder));
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetString(fileFolderInfo.FullName);
    }
}