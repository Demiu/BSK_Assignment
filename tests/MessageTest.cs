using System.IO;
using System.Threading;
using System.Text;
using Xunit;

namespace tests;

public class MessageTest {

    [Fact]
    public void SerializeDeserializeSecureRequest() {
        // Content to be send over
        var content = "this is not a valid public key";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        // Create and serialize the message
        var messagePre = new Lib.Messages.SecureRequest(contentBytes);
        var serialized = messagePre.Serialized();
        // Deserialize the message
        Lib.Messages.SecureRequest? messagePost;
        using(var stream = new MemoryStream(serialized)) {
            // pop the type
            var kind = stream.ReadExactlyAsync(1, CancellationToken.None).Result;
            Assert.Equal<byte>(kind[0], (byte)Lib.Defines.MessageKind.SecureRequest);

            messagePost = Lib.Messages.SecureRequest.Deserialize(stream).Result as Lib.Messages.SecureRequest;
            Assert.NotNull(messagePost);
        }
        // Check the contents
        var contentBytesPost = messagePost!.publicKey;
        Assert.Equal(contentBytes, contentBytesPost);
        var contentPost = Encoding.UTF8.GetString(messagePost.publicKey);
        Assert.Equal(content, contentPost);
    }
}