using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public class Ping : Message {
    public override MessageKind Kind => MessageKind.Ping;

    public static Task<Message> Deserialize(NetworkStream stream)
    {
        return Task.FromResult<Message>(new Ping());
    }

    public override void SerializeIntoInner(BinaryWriter writer)
    {
        // empty message
    }
}

public class Pong : Message {
    public override MessageKind Kind => MessageKind.Pong;

    public static Task<Message> Deserialize(NetworkStream stream)
    {
        return Task.FromResult<Message>(new Pong());
    }

    public override void SerializeIntoInner(BinaryWriter writer)
    {
        // empty message
    }
}
