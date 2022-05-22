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