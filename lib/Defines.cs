namespace Lib.Defines;

enum MessageKind: byte {
    Ping = 0,
    Pong = 1,
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