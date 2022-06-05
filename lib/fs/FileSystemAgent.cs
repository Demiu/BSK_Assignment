using System.Collections.Concurrent;
using Lib.Messages;

namespace Lib.Fs;

public class FileSystemAgent {
    string shareDir;
    string downloadDir;
    Dictionary<string, Transfer> ongoingTransfers;

    public FileSystemAgent() 
    : this(Defines.Constants.DEFAULT_SHARE_DIR, Defines.Constants.DEFAULT_DOWNLOAD_DIR) 
    { }

    public FileSystemAgent(string shareDir, string downloadDir) {
        this.shareDir = shareDir;
        this.downloadDir = downloadDir;
        this.ongoingTransfers = new();
    }

    public FileSystemAgent DefaultWithShareDir(string shareDir) {
        var a = new FileSystemAgent();
        a.shareDir = shareDir;
        return a;
    }

    public FileSystemAgent DefaultWithDownloadDir(string downloadDir) {
        var a = new FileSystemAgent();
        a.downloadDir = downloadDir;
        return a;
    }

    public async Task AnnounceDirectoryContents(string path, Func<Message, Task> messageConsumer) {
        path = path.TrimStart('/'); // TODO replace '/' with a constant
        if (!shareDir.PathContainsSubPath(path)) {
            // TODO error out, dir not in share path
            Console.WriteLine("Error: path not a subpath of shareDir in AnnounceDirectoryContents");
            return;
        }
        var requestedPath = Path.Join(shareDir, path);
        if (Directory.Exists(requestedPath)) {
            foreach (var entry in Directory.GetFileSystemEntries(requestedPath)) {
                await messageConsumer(new AnnounceDirectoryEntry(shareDir, entry));
            }
        }
    }

    public async Task TransferPath(string path, Func<Message, Task> messageConsumer) {
        path = path.TrimStart('/'); // TODO replace '/' with a constant
        if (!shareDir.PathContainsSubPath(path)) {
            // TODO error out, file not in share path
            Console.WriteLine("Error: path not a subpath of shareDir in TransferPath");
            return;
        }
        var requestedPath = Path.Join(shareDir, path);
        if (Directory.Exists(requestedPath)) {
            // TODO send all files in the directory
            foreach (var file in Directory.EnumerateFiles(requestedPath, "*", SearchOption.AllDirectories))
            {
                Console.WriteLine($"file in requested dir: {file}");
            }
        } else if (File.Exists(requestedPath)) {
            // TODO send single file
            Console.WriteLine($"requested file: {requestedPath}");
            await TransferFile(requestedPath, messageConsumer);
        } else {
            // TODO send an error
            Console.WriteLine($"Couldn't find file {requestedPath}");
        }
    }

    public void NewIncomingTransfer(string path, Int64 size) {
        path = path.TrimStart('/'); // TODO replace '/' with a constant
        if (!downloadDir.PathContainsSubPath(path)) {
            // TODO error out, file not in download path
            Console.WriteLine("Error: path not a subpath of downloadDir in TransferPath");
            return;
        }
        var localPath = Path.Join(downloadDir, path);
        ongoingTransfers.Add(path, new Transfer(localPath, size));
        Util.TaskRunSafe(async () => {
            await ongoingTransfers[path].WriteLoop();
            // TODO remove transfer from ongoingTransfers THIS WILL MAKE IT CONCURRENT
        });
    }

    protected async Task TransferFile(string path, Func<Message, Task> messageConsumer) {
        // TODO token
        var relativePath = Path.GetRelativePath(shareDir, path);
        if (!relativePath.StartsWith('/')) { // TODO replace '/' with a constant
            relativePath = $"/{relativePath}";
        }

        // Create FileStream before announcing transfer, in case it fails to open
        using var file = File.Open(path, FileMode.Open, FileAccess.Read);
        Int64 totalSize = file.Length;
        Int64 sentSize = 0;

        await messageConsumer(new AnnounceTransfer(relativePath, totalSize));

        // Prefeth the first chunk, so we can read the file and send data at the same time
        var toRead = (Int32)Math.Min(Defines.Constants.FILE_TRANSFER_CHUNK_SIZE, totalSize - sentSize);
        var chunk = await file.ReadExactlyAsync(toRead, new()); // TODO token

        while(sentSize != totalSize) {
            byte[]? new_chunk = null;
            toRead = (Int32)Math.Min(Defines.Constants.FILE_TRANSFER_CHUNK_SIZE, totalSize - sentSize - chunk.Length);
            var read = async () => { new_chunk = await file.ReadExactlyAsync(toRead, new()); };

            await Task.WhenAll(
                messageConsumer(new TransferChunk(relativePath, chunk)),
                read()
            );
            sentSize += chunk.Length;
            chunk = new_chunk!;
        }
    }
}