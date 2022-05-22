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
        using (var cryptoStream = new CryptoStream(memStream, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
            cryptoStream.Write(nested.Serialized());
        }

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

        var iv = await stream.ReadNetIntPrefixedByteArrayAsync(token);
        var content = await stream.ReadNetIntPrefixedByteArrayAsync(token);

        return new SecuredMessage(iv, content);
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(iv);
        stream.WriteNetIntPrefixedByteArray(content);
    }
}