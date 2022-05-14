public static class StreamExtension 
{
    // Make sure stream will have that data, or the task will never finish
    public static async Task<byte[]> ReadExactlyAsync(this System.IO.Stream stream, int count, CancellationToken token) {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count) {
            int read = await stream.ReadAsync(buffer, offset, count, token);
            if (read == 0) {
                throw new System.IO.EndOfStreamException();
            }
            offset += read;
        }
        return buffer;
    }
}