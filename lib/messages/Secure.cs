using System.Net;
using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public class SecureRequest : Message
{
    public byte[] publicKey;

    public override MessageKind Kind => Defines.MessageKind.SecureRequest;

    public SecureRequest(byte[] publicKey) {
        this.publicKey = publicKey;
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        // TODO add token param
        var src = new CancellationTokenSource();
        var token = src.Token;

        var lenArray = await stream.ReadExactlyAsync(4, token);
        Int32 len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenArray));

        var publicKey = await stream.ReadExactlyAsync(len, token);

        return new SecureRequest(publicKey);
    }

    protected override void SerializeIntoInner(BinaryWriter writer)
    {
        Int32 len = publicKey.Length;
        writer.Write(IPAddress.HostToNetworkOrder(len));
        writer.Write(publicKey);
    }
}
