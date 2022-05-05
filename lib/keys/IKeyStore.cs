namespace KeyStore;

public interface IKeyStore
{
    void SaveEncryptedKeys(string publicKey, string privateKey, string hashedPassword);
    (string PublicKey, string PrivateKey) GetEncryptedKeys(string hashedPassword);
    bool KeysExists();
    void DeleteKeys();
}