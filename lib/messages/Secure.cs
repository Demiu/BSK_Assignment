using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
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

        var publicKey = await stream.ReadNetIntPrefixedByteArrayAsync(token);

        return new SecureRequest(publicKey);
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(publicKey);
    }
}

public class SecureAccept : Message
{
    public byte[] encryptedKey;

    public override MessageKind Kind => Defines.MessageKind.SecureAccept;

    public SecureAccept(byte[] pubRsaKey, byte[] aesKey) {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(pubRsaKey, out var len);
        Debug.Assert(len == pubRsaKey.Length);

        encryptedKey = rsa.Encrypt(aesKey, Defines.Constants.RSA_PADDING_TYPE);
    }

    protected SecureAccept(byte[] encryptedKey) {
        this.encryptedKey = encryptedKey;
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        // TODO add token param
        var src = new CancellationTokenSource();
        var token = src.Token;

        var encryptedKey = await stream.ReadNetIntPrefixedByteArrayAsync(token);

        return new SecureAccept(encryptedKey);
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(encryptedKey);
    }
}

public class SecureReject : Message
{
    public override MessageKind Kind => Defines.MessageKind.SecureReject;

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        throw new NotImplementedException(); // TODO
    }
}
