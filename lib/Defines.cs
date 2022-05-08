namespace Lib.Defines;

public class Constants {
    public const UInt16 DEFAULT_PORT = 32004;
    public const Int32 RSA_KEY_SIZE = 1024;
}

public enum MessageKind: byte {
    Ping = 0,
    Pong = 1,
    SecureRequest = 2,
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