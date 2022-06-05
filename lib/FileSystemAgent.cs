using Lib.Messages;

namespace Lib;

public class FileSystemAgent {
    string shareDir;
    string downloadDir;

    public FileSystemAgent() {
        this.shareDir = Defines.Constants.DEFAULT_SHARE_DIR;
        this.downloadDir = Defines.Constants.DEFAULT_DOWNLOAD_DIR;
    }

    public FileSystemAgent(string shareDir, string downloadDir) {
        this.shareDir = shareDir;
        this.downloadDir = downloadDir;
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
            return;
        }
        var requestedPath = Path.Join(shareDir, path);
        if (Directory.Exists(requestedPath)) {
            foreach (var entry in Directory.GetFileSystemEntries(requestedPath)) {
                await messageConsumer(new AnnounceDirectoryEntry(shareDir, entry));
            }
        }
    }

    public async Task TransferFile(string path) {
        if (!shareDir.PathContainsSubPath(path)) {
            // TODO error out, file not in share path
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
        } else {
            // TODO send an error
            Console.WriteLine($"Couldn't find file {requestedPath}");
        }
    }
}