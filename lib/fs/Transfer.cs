using System.Threading.Tasks.Dataflow;

namespace Lib.Fs;

// Represents an incoming transfer
class Transfer : IDisposable {
    // TODO: CancellationToken
    readonly Int64 totalSize;
    Int64 currentSize;
    // BufferBlock is thread-safe, needed because file transfer chunks arrive in order
    // but pushing and popping (can) happen on different threads
    BufferBlock<byte[]> chunksToWrite;
    FileStream file;

    public Transfer(string path, Int64 size) {
        this.totalSize = size;
        this.currentSize = 0;
        this.chunksToWrite = new();
        
        var directories = Path.GetDirectoryName(path);
        if (directories != null) {
            Directory.CreateDirectory(directories);
        }
        this.file = File.Open(path, FileMode.Create);
    }

    public async Task WriteLoop() {
        // TODO token
        while(currentSize != totalSize) {
            var chunk = await chunksToWrite.ReceiveAsync();
            Console.WriteLine($"Received chunk of size {chunk.Length}");
            if (currentSize + chunk.Length > totalSize) {
                Console.WriteLine($"Error: too much data for {file.Name} (expected: {totalSize}, got: {currentSize + chunk.Length})");
                return;
            }
            await file.WriteAsync(chunk);
            currentSize += chunk.Length;
        }
        file.Close();
    }

    public void QueueChunk(byte[] chunk) {
        chunksToWrite.Post(chunk); // TODO maybe SendAsync?
    }

    public void Dispose()
    {
        ((IDisposable)file).Dispose();
    }
}