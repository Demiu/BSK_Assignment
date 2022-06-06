using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Lib.Defines;

namespace Lib.Messages;

public class SecureRequest : Message
{
    public byte[] publicKey;

    public override MessageKind Kind => MessageKind.SecureRequest;

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

    public override MessageKind Kind => MessageKind.SecureAccept;

    public SecureAccept(byte[] pubRsaKey, byte[] aesKey) {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(pubRsaKey, out var len);
        Debug.Assert(len == pubRsaKey.Length);

        encryptedKey = rsa.Encrypt(aesKey, Constants.RSA_PADDING_TYPE);
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
    public SecureRejectReasonKind Reason { get; protected set;}
    public override MessageKind Kind => MessageKind.SecureReject;

    public static SecureReject AlreadySecured = new SecureReject(SecureRejectReasonKind.AlreadySecured);
    public static SecureReject NotInWhitelist = new SecureReject(SecureRejectReasonKind.NotInWhitelist);

    protected SecureReject(SecureRejectReasonKind reason) {
        this.Reason = reason;
    }

    public static async Task<Message> Deserialize(Stream stream) {
        byte reason = (await stream.ReadExactlyAsync(1, new()))[0]; // TODO token
        return new SecureReject((SecureRejectReasonKind)reason);
    }

    protected override void SerializeIntoInner(System.IO.Stream stream) {
        stream.Write(new byte[]{ (byte)Reason });
    }
}
