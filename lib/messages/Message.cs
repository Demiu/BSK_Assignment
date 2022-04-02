using System.Net.Sockets;
using Lib.Defines;

namespace Lib.Messages;

public abstract class Message {
    public byte[] Serialize() {
        using (MemoryStream m = new MemoryStream()) {
            using (BinaryWriter w = new BinaryWriter(m)) {
                SerializeInto(w);
            }
            return m.ToArray();
        }
    }

    public abstract void SerializeInto(BinaryWriter writer);
}
