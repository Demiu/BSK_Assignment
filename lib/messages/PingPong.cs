using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public class Ping : Message {
    public override MessageKind Kind => MessageKind.Ping;

    public static Task<Message> Deserialize(Socket socket)
    {
        return Task.FromResult<Message>(new Ping());
    }

    protected override void SerializeIntoInner(BinaryWriter writer)
    {
        // empty message
    }
}

public class Pong : Message {
    public override MessageKind Kind => MessageKind.Pong;

    public static Task<Message> Deserialize(Socket socket)
    {
        return Task.FromResult<Message>(new Pong());
    }

    protected override void SerializeIntoInner(BinaryWriter writer)
    {
        // empty message
    }
}
