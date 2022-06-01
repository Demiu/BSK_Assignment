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
}