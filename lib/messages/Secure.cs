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
    public byte[] encryptedKeySignature;
    public byte[] signingPublicKey;

    public override MessageKind Kind => MessageKind.SecureAccept;

    // TODO having RSA here is dangerous (could leak a privkey from SecurityAgent/AsymmetricContainer)
    public SecureAccept(byte[] encryptingKey, RSA signer, byte[] aesKey) {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(encryptingKey, out var len);
        Debug.Assert(len == encryptingKey.Length);

        encryptedKey = rsa.Encrypt(aesKey, Constants.RSA_PADDING_TYPE);
        encryptedKeySignature = signer.SignData(encryptedKey, Constants.SIGNING_HASH_ALGO, Constants.SIGNING_PADDING_TYPE);
        signingPublicKey = signer.ExportRSAPublicKey();
    }

    protected SecureAccept(byte[] encryptedKey, byte[] encryptedKeySignature, byte[] signingPublicKey) {
        this.encryptedKey = encryptedKey;
        this.encryptedKeySignature = encryptedKeySignature;
        this.signingPublicKey = signingPublicKey;
    }

    public bool CheckSignature() {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(signingPublicKey, out var len);
        Debug.Assert(len == signingPublicKey.Length);

        return rsa.VerifyData(encryptedKey, encryptedKeySignature, Constants.SIGNING_HASH_ALGO, Constants.SIGNING_PADDING_TYPE);
    }

    public static async Task<Message> Deserialize(Stream stream)
    {
        var encryptedKey = await stream.ReadNetIntPrefixedByteArrayAsync(new()); // TODO token
        var encryptedKeySignature = await stream.ReadNetIntPrefixedByteArrayAsync(new());
        var signingPublicKey = await stream.ReadNetIntPrefixedByteArrayAsync(new());

        return new SecureAccept(encryptedKey, encryptedKeySignature, signingPublicKey);
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        stream.WriteNetIntPrefixedByteArray(encryptedKey);
        stream.WriteNetIntPrefixedByteArray(encryptedKeySignature);
        stream.WriteNetIntPrefixedByteArray(signingPublicKey);
    }
}

public class SecureFinalize : Message {
    public override MessageKind Kind => MessageKind.SecureFinalize;

    public static Task<Message> Deserialize(Stream stream)
    {
        return Task.FromResult<Message>(new SecureFinalize());
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        // empty message
    }
}

public class SecureReject : Message
{
    public SecureRejectReasonKind Reason { get; protected set;}
    
    public override MessageKind Kind => MessageKind.SecureReject;

    public static SecureReject WrongState = new SecureReject(SecureRejectReasonKind.WrongState);
    public static SecureReject NotInWhitelist = new SecureReject(SecureRejectReasonKind.NotInWhitelist);
    public static SecureReject InvalidSignature = new SecureReject(SecureRejectReasonKind.InvalidSignature);

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
