using System.Security.Cryptography;
using KeyStore.package;

namespace KeyStore;

public interface IEncryptor
{
    (string PublicKey, string PrivateKey) GetKeys(string password);
    byte[] GenerateSessionKey();
    void SetSessionKey(List<byte> key);
    void SetReceiverPublicKey(string key);
    string GetSessionKeyEncryptedWithReceiverPublicKey();
    byte[] DecryptSessionKeyWithRsa(string data);
    

}