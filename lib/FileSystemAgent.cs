namespace Lib;

class FileSystemAgent {
    string shareDir;
    string downloadDir;

    public FileSystemAgent() {
        shareDir = Defines.Constants.DEFAULT_SHARE_DIR;
        downloadDir = Defines.Constants.DEFAULT_DOWNLOAD_DIR;
    }
}