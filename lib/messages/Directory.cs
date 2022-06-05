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
    public string entryPath;
    public FileSystemType fileSystemType;
    
    public override MessageKind Kind => MessageKind.AnnounceDirectoryEntry;
    
    // entryPath should be already checked for existence
    public AnnounceDirectoryEntry(string basePath, string entryFullPath)
    {
        this.entryPath = Path.GetRelativePath(basePath, entryFullPath);
        if (!this.entryPath.StartsWith('/')) { // TODO replace '/' with a constant
            this.entryPath = $"/{this.entryPath}";
        }
        this.fileSystemType = File.Exists(entryFullPath) ? FileSystemType.File : FileSystemType.Directory;
    }
    
    protected AnnounceDirectoryEntry(string entryPath, FileSystemType fileSystemType)
    {
        this.entryPath = entryPath;
        this.fileSystemType = fileSystemType;
    }
    
    public static async Task<Message> Deserialize(Stream stream)
    {
        // TODO add token param
        var src = new CancellationTokenSource();
        var token = src.Token;
        
        string fileFolder = await stream.ReadNetStringAsync(token);
        byte type = (await stream.ReadExactlyAsync(1, token))[0];
        
        return new AnnounceDirectoryEntry(fileFolder, (FileSystemType) type);
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetString(entryPath);
        stream.Write(new byte[]{ (byte)fileSystemType });
    }
}