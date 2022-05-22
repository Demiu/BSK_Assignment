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

        var lenArray = await stream.ReadExactlyAsync(4, token);
        Int32 len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenArray));

        var encryptedKey = await stream.ReadExactlyAsync(len, token);

        return new SecureAccept(encryptedKey);
    }

    protected override void SerializeIntoInner(BinaryWriter writer)
    {
        Int32 keyLen = encryptedKey.Length;
        writer.Write(IPAddress.HostToNetworkOrder(keyLen));
        writer.Write(encryptedKey);
    }
}

public class SecureReject : Message
{
    public override MessageKind Kind => Defines.MessageKind.SecureReject;

    protected override void SerializeIntoInner(BinaryWriter writer)
    {
        throw new NotImplementedException(); // TODO
    }
}
