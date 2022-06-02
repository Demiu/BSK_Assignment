using System.Net;
using System.Security.Cryptography;
using Lib.Defines;

namespace Lib.Messages;

public class SecuredMessage : Message
{
    byte[] iv;
    byte[] content;

    public override MessageKind Kind => MessageKind.SecuredMessage;

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

    public void FeedNestedTo(Action<Stream> to, byte[] aesKey) {
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = iv;

        using var memStream = new MemoryStream(content);
        using (var cryptoStream = new CryptoStream(memStream, aes.CreateDecryptor(), CryptoStreamMode.Read)) {
            to(cryptoStream);
        }
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(iv);
        stream.WriteNetIntPrefixedByteArray(content);
    }
}