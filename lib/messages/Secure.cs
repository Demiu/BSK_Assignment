using System.Net;
using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public class SecureRequest : Message
{
    byte[] publicKey;

    public override MessageKind Kind => Defines.MessageKind.SecureRequest;

    public SecureRequest(byte[] publicKey) {
        this.publicKey = publicKey;
    }

    public static async Task<Message> Deserialize(Socket socket)
    {
        byte[] lenArray = new byte[4];
        Int32 len = IPAddress.NetworkToHostOrder(
            await socket.ReceiveAsync(lenArray, SocketFlags.None));

        byte[] publicKey = new byte[len];
        await socket.ReceiveAsync(publicKey, SocketFlags.None);

        return new SecureRequest(publicKey);
    }

    protected override void SerializeIntoInner(BinaryWriter writer)
    {
        Int32 len = publicKey.Length;
        writer.Write(IPAddress.HostToNetworkOrder(len));
        writer.Write(publicKey);
    }
}