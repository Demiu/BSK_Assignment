using System.Collections.Immutable;

namespace KeyStore.package;

public static class FilePackager
{
    
    public static List<Package> DivideIntoPackages(byte[] data, int packageSize)
    {
        var packages = new List<Package>();
        var i = 0;
        while (data.Length > packages.Count * packageSize)
        {
            var package = new Package
            {
                // TODO: creating GUID?
                Number = ++i,
                Data = data.Skip(packages.Count * packageSize).Take(packageSize).ToList()
            };
            packages.Add(package);
        }

        return packages;
    }
    
    // TODO: maybe we will have to add a method to sort these packets in case something is wrong? Maybe the GUIDs?

    //

    public static byte[] UnpackData(List<Package> packages)
    {
        List<byte> unpackedData = new List<byte>();
        foreach (var package in packages)
        {
            unpackedData.AddRange(package.Data);
        }
        return unpackedData.ToArray();
    }
    
}