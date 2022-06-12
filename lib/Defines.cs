using System.Net;
using System.Security.Cryptography;

namespace Lib.Defines;

public class Constants {
    public static readonly string DEFAULT_SHARE_DIR = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    public static readonly string DEFAULT_DOWNLOAD_DIR = Directory.GetCurrentDirectory();
    public const UInt16 DEFAULT_PORT = 32004;
    public static readonly IPAddress DEFAULT_ADDRESS = IPAddress.Any;
    public const Int32 RSA_KEY_SIZE = 1024;
    public static readonly RSAEncryptionPadding RSA_PADDING_TYPE = RSAEncryptionPadding.OaepSHA1;
    public static readonly HashAlgorithmName SIGNING_HASH_ALGO = HashAlgorithmName.SHA256;
    public static readonly RSASignaturePadding SIGNING_PADDING_TYPE = RSASignaturePadding.Pkcs1;
    public const string DEFAULT_PATH = "/";
    public const Int32 FILE_TRANSFER_CHUNK_SIZE = 1024;
}

public enum MessageKind: byte {
    Ping = 0,
    Pong = 1,
    SecureRequest = 2,
    SecureAccept = 3,
    SecureFinalize = 4,
    SecureReject = 5,
    SecuredMessageCBC = 6,
    SecuredMessageECB = 7,
    DirectoryRequest = 8,
    AnnounceDirectoryEntry = 9,
    TransferRequest = 10,
    AnnounceTransfer = 11,
    TransferChunk = 12,
}

public enum EncryptionMode {
    ECB,
    CBC
}

public enum SecureRejectReasonKind: byte {
    WrongState = 0,
    NotInWhitelist = 1,
    InvalidSignature = 2,
}

public enum FileSystemKind: byte {
    File = 0,
    Directory = 1,
}

static class MessageKindMethods {
    public static MessageKind? FromByte(byte b) => 
        MessageKind.IsDefined(typeof(MessageKind), b) ? (MessageKind)b : null;
}

public class UnexpectedEnumValueException<E, V> : Exception {
    public UnexpectedEnumValueException(V value) 
    : base("Value " + value + " is not defined for " + typeof(E).Name + " enum.")
    { }
}
