namespace Lib.Crypto;

public class Keystore
{
    private List<string> publicKeys;
    private List<string> privateKeys;
    // TODO: a hardcoded key to check if it is in a whitelist
    string randomKey = "adsdsaasdadskjy712u7643tg7rtig";
    //

    public Keystore() {
        this.publicKeys = new List<string>();
        this.privateKeys = new List<string>();
    }

    public void SaveEncryptedKeys() {
        // TODO: i do not know where to store these keys for now, therefore a hardcoded path
        string publicKeyPath = "./keys/public_key";
        string privateKeyPath = "./keys/private_key";
        //
        if (KeysExists(publicKeyPath, privateKeyPath)) {
            LoadKeys(publicKeyPath, privateKeyPath);
        }
    }

    public void LoadKeys(string publicKeyPath, string privateKeyPath) {
        if (File.Exists(publicKeyPath)) {
            var publicKey = File.ReadAllText(publicKeyPath);
            publicKeys.Add(publicKey);
        }

        if (File.Exists(privateKeyPath)) {
            var privateKey = File.ReadAllText(privateKeyPath);
            privateKeys.Add(privateKey);
        }
    }

    public bool KeysExists(string publicKeyPath, string privateKeyPath) {
        return File.Exists(publicKeyPath) && File.Exists(privateKeyPath);
    }

    public void CheckKeyWhitelist() {
        if (publicKeys.Contains(randomKey) || privateKeys.Contains(randomKey)) {
            Console.WriteLine("The key is whitelist");
        }
    }
    
}