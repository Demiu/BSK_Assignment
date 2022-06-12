using System.Security.Cryptography;

namespace Lib.Crypto;

public class AsymmetricContainer {
    List<RSA> knownPublicKeys;
    RSA ownPair;

    public AsymmetricContainer()
    : this(RSA.Create(Defines.Constants.RSA_KEY_SIZE))
    { }

    public AsymmetricContainer(RSA ownPair) {
        this.knownPublicKeys = new();
        this.ownPair = ownPair;
    }

    public static AsymmetricContainer FromPem(string pem) {
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new AsymmetricContainer(rsa);
    }

    public byte[] GetOwnPubKey() => ownPair.ExportRSAPublicKey();
    public byte[] Decrypt(byte[] data) => ownPair.Decrypt(data, Defines.Constants.RSA_PADDING_TYPE);

    public bool LoadPubFromPem(string pem) {
        var rsa = RSA.Create();
        try {
            rsa.ImportFromPem(pem);
            knownPublicKeys.Add(rsa);
            return true;
        }
        catch (System.ArgumentException) {
            // TODO log
            return false;
        }
    }

    public bool LoadPubFromPemFile(string path) {
        try {
            var pem = File.ReadAllText(path);
            return LoadPubFromPem(pem);
        }
        catch (System.Exception e) {
            Console.WriteLine($"Failed to load pem from {path}: {e}");
            return false;
        }
    }

    // Returns number of files which were loaded
    public int LoadPubsFromPemsInDirectory(string path) {
        try {
            int loaded = 0;
            foreach (var item in Directory.GetFiles(path)) {
                if (LoadPubFromPemFile(item)) {
                    loaded += 1;
                }
            }
            return loaded;
        }
        catch (System.Exception e) {
            Console.WriteLine($"Failed to load pems from {path} directory: {e}");
            return 0;
        }
    }

    public bool PubKeyKnown(byte[] pub) {
        foreach (var rsa in knownPublicKeys) {
            var knownPub = rsa.ExportRSAPublicKey();
            if (knownPub.Length != pub.Length) {
                continue;
            }
            if (knownPub.SequenceEqual(pub)) {
                return true;
            }
        }
        return false;
    }
}