using System.Net;
using System.Security.Cryptography;
using Lib.Defines;

namespace Lib.Messages;

public class SecuredMessage : Message
{
    byte[] iv;
    byte[] content;

    public override MessageKind Kind => Defines.MessageKind.SecuredMessage;

    public SecuredMessage(Message nested, byte[] aesKey) {
        using var aes = Aes.Create();
        aes.Key = aesKey;

        using var memStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(nested.Serialized());

        this.iv = aes.IV;
        this.content = memStream.ToArray();
    }

    protected SecuredMessage(byte[] iv, byte[] content) {
        this.iv = iv;
        this.content = content;
    }

    public static async Task<Message> Deserialize(Stream stream) 
    {
        // TODO add token param
        var src = new CancellationTokenSource();
        var token = src.Token;

        var ivLenArray = await stream.ReadExactlyAsync(4, token);
        Int32 ivLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ivLenArray));
        var iv = await stream.ReadExactlyAsync(ivLen, token);

        var contentLenArray = await stream.ReadExactlyAsync(4, token);
        Int32 contentLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(contentLenArray));
        var content = await stream.ReadExactlyAsync(contentLen, token);

        return new SecuredMessage(iv, content);
    }

    protected override void SerializeIntoInner(BinaryWriter writer)
    {
        Int32 ivLen = iv.Length;
        writer.Write(IPAddress.HostToNetworkOrder(ivLen));
        writer.Write(iv);

        Int32 contentLen = content.Length;
        writer.Write(IPAddress.HostToNetworkOrder(contentLen));
        writer.Write(content);
    }
}