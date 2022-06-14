namespace Lib.Fs;

public class TransferHandle {
    Transfer handle;

    public double Progress => (double)handle.CurrentSize / handle.TotalSize;
    public string Name => handle.FilePath;

    internal TransferHandle(Transfer handle) {
        this.handle = handle;
    }
}
