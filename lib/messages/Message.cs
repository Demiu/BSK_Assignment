using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public abstract class Message {
    public abstract MessageKind Kind{get;}

    public byte[] Serialized() {
        using (MemoryStream s = new MemoryStream()) {
            SerializeInto(s);
            return s.ToArray();
        }
    }

    public void SerializeInto(System.IO.Stream stream) {
        stream.Write(new byte[]{ (byte)Kind });
        SerializeIntoInner(stream);
    }

    protected abstract void SerializeIntoInner(System.IO.Stream stream);
}
