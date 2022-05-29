using System.IO;
using System;
using Xunit;

namespace tests;

public class StreamExtensionTest {
    [Fact]
    public void StringWriteEncode() {
        var input = "testκόσμε";
        var expected = new byte[] { 0x74, 0x65, 0x73, 0x74, 0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5 };

        using var memStream = new MemoryStream();
        memStream.WriteString(input);
        // Skip the first 4 bytes (length)
        var output = memStream.ToArray()[new Index(4)..];

        Assert.Equal(output, expected);
    }

    [Fact]
    public void StringWriteRead() {
        var input = "testκόσμε";

        using var memStream = new MemoryStream();
        memStream.WriteString(input);
        memStream.Seek(0, SeekOrigin.Begin);
        var output = memStream.ReadStringAsync(new()).Result;

        Assert.Equal(output, input);
    }
}