using System.Runtime.InteropServices;

namespace KeyStore;

public class DirectoryKeyStore : IKeyStore
{
    private readonly string separator;
    private readonly string publicKeyDir;
    private readonly string privateKeyDir;
    private string publicKeyPath => $"{publicKeyDir}{separator}public";
    private string privateKeyPath => $"{privateKeyDir}{separator}private";


    public DirectoryKeyStore()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            separator = "/";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            separator = "\\";
        }
        
        publicKeyDir = @$"{Environment.CurrentDirectory}{separator}keys{separator}pub";
        privateKeyDir = @$"{Environment.CurrentDirectory}{separator}keys{separator}priv";

        Directory.CreateDirectory(publicKeyDir);
        Directory.CreateDirectory(privateKeyDir);
    }

    public void DeleteKeys()
    {
        if (File.Exists(publicKeyPath))
        {
            File.Delete(publicKeyPath);
        }
        if (File.Exists(privateKeyPath))
        {
            File.Delete(privateKeyPath);
        }
    }

    public void SaveEncryptedKeys(string publicKey, string privateKey, string hashedPassword)
    {
        File.WriteAllText(publicKeyPath, publicKey);
        File.WriteAllText(privateKeyPath, privateKey);
    }

    public (string PublicKey, string PrivateKey) GetEncryptedKeys(string hashedPassword)
    {
        var publicKey = string.Empty;
        var privateKey = string.Empty;
        if (File.Exists(publicKeyPath))
        {
            publicKey = File.ReadAllText(publicKeyPath);
        }

        if (File.Exists(privateKeyPath))
        {
            privateKey = File.ReadAllText(privateKeyPath);
        }

        return (publicKey, privateKey);
    }

    public bool KeysExists()
    {
        return File.Exists(publicKeyPath) && File.Exists(privateKeyPath);
    }
    
}