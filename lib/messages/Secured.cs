using System.Net;
using System.Security.Cryptography;
using Lib.Defines;

namespace Lib.Messages;

public class SecuredMessageCBC : Message
{
    byte[] iv;
    byte[] content;

    public override MessageKind Kind => MessageKind.SecuredMessageCBC;

    public SecuredMessageCBC(Message nested, byte[] aesKey) {
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.Mode = CipherMode.CBC;

        using var memStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memStream, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
            cryptoStream.Write(nested.Serialized());
        }

        this.iv = aes.IV;
        this.content = memStream.ToArray();
    }

    protected SecuredMessageCBC(byte[] iv, byte[] content) {
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

        return new SecuredMessageCBC(iv, content);
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

public class SecuredMessageECB : Message 
{
    byte[] content;

    public override MessageKind Kind => MessageKind.SecuredMessageECB;

    public SecuredMessageECB(Message nested, byte[] aesKey) {
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.Mode = CipherMode.ECB;

        using var memStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memStream, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
            cryptoStream.Write(nested.Serialized());
        }

        this.content = memStream.ToArray();
    }

    protected SecuredMessageECB(byte[] content) {
        this.content = content;
    }

    public static async Task<Message> Deserialize(Stream stream) 
    {
        var content = await stream.ReadNetIntPrefixedByteArrayAsync(new()); // TODO token
        return new SecuredMessageECB(content);
    }

    public void FeedNestedTo(Action<Stream> to, byte[] aesKey) {
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.Mode = CipherMode.ECB;

        using var memStream = new MemoryStream(content);
        using (var cryptoStream = new CryptoStream(memStream, aes.CreateDecryptor(), CryptoStreamMode.Read)) {
            to(cryptoStream);
        }
    }

    protected override void SerializeIntoInner(Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(content);
    }
}
