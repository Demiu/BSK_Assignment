using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public abstract class Message {
    public abstract MessageKind Kind{get;}

    public byte[] Serialized() {
        using (MemoryStream m = new MemoryStream()) {
            using (BinaryWriter w = new BinaryWriter(m)) {
                SerializeInto(w);
            }
            return m.ToArray();
        }
    }

    public void SerializeInto(BinaryWriter writer) {
        writer.Write((byte)Kind);
        SerializeIntoInner(writer);
    }

    protected abstract void SerializeIntoInner(BinaryWriter writer);
}
