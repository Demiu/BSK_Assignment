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
}