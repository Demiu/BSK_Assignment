namespace KeyStore.package;

public class Package
{
    // TODO:  GUID?
    public int Number { get; set; }
    public List<byte> Data { get; set; }
    public List<byte> Iv { get; set; }
}