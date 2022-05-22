using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public class Ping : Message 
{
    public override MessageKind Kind => MessageKind.Ping;

    public static Task<Message> Deserialize(Stream stream)
    {
        return Task.FromResult<Message>(new Ping());
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        // empty message
    }
}

public class Pong : Message 
{
    public override MessageKind Kind => MessageKind.Pong;

    public static Task<Message> Deserialize(Stream stream)
    {
        return Task.FromResult<Message>(new Pong());
    }

    protected override void SerializeIntoInner(System.IO.Stream stream)
    {
        // empty message
    }
}
