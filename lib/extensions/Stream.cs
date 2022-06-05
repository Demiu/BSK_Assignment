using System.Net;
using System.Text;

public static class StreamExtension 
{
    // Make sure stream will have that data, or the task will never finish
    public static async Task<byte[]> ReadExactlyAsync(this System.IO.Stream stream, int count, CancellationToken token) {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count) {
            int read = await stream.ReadAsync(buffer, offset, count, token);
            if (read == 0) {
                //Console.WriteLine("Throwing EOS"); // TODO uncomment when we add proper logging
                throw new System.IO.EndOfStreamException();
            }
            offset += read;
        }
        return buffer;
    }

    public static async Task<Int64> ReadNetInt64Async(this System.IO.Stream stream, CancellationToken token) {
        var array = await stream.ReadExactlyAsync(8, token);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(array));
    }

    public static void WriteNetInt64(this System.IO.Stream stream, Int64 value) {
        stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
    }

    // Reads a byte array that's prefixed by an Int32 (in network order) specifying the array's length
    public static async Task<byte[]> ReadNetIntPrefixedByteArrayAsync(this System.IO.Stream stream, CancellationToken token) {
        var lenArray = await stream.ReadExactlyAsync(4, token);
        Int32 len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenArray));
        return await stream.ReadExactlyAsync(len, token);
    }

    // Writes a byte array that's prefixed by an Int32 (in network order) specyfing the array's length
    public static void WriteNetIntPrefixedByteArray(this System.IO.Stream stream, byte[] array) {
        Int32 len = array.Length;
        stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len)));
        stream.Write(array);
    }

    public static async Task<string> ReadNetStringAsync(this System.IO.Stream stream, CancellationToken token) {
        return Encoding.UTF8.GetString(await stream.ReadNetIntPrefixedByteArrayAsync(token));
    }

    public static void WriteNetString(this System.IO.Stream stream, string toWrite) {
        stream.WriteNetIntPrefixedByteArray(Encoding.UTF8.GetBytes(toWrite));
    }
}