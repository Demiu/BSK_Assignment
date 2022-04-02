using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public class Ping : Message {
    public static Task<Message> Deserialize(NetworkStream stream)
    {
        return Task.FromResult<Message>(new Ping());
    }

    public override void SerializeInto(BinaryWriter writer)
    {
        writer.Write((byte)MessageKind.Ping);
    }
}

public class Pong : Message {
    public static Task<Message> Deserialize(NetworkStream stream)
    {
        return Task.FromResult<Message>(new Pong());
    }

    public override void SerializeInto(BinaryWriter writer)
    {
        writer.Write((byte)MessageKind.Pong);
    }
}
