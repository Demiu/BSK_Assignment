using System.Security.Cryptography;
using System.Text;
using KeyStore.package;

namespace KeyStore;

public class Encryptor : IEncryptor
{
    private readonly IKeyStore _keyStore;
    private List<byte> _sessionKey;
    private byte[] _publicKey;
    private List<byte> _privateKey;
    
    private List<Guid> _packageIds = new List<Guid>();
    private List<Package> _packages = new List<Package>();
    
    private List<byte> _receiverPublicKey;

    public Encryptor(IKeyStore keyStore)
    {
        _keyStore = keyStore;
    }

    public (string hashedPassword, byte[] hashedPasswordBytes) HashPassword(string password)
    {
        var sha = SHA256.Create();
        var hashedPasswordBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hashedPassword = Encoding.UTF8.GetString(hashedPasswordBytes);

        return (hashedPassword, hashedPasswordBytes);
    }
    
    public (string PublicKey, string PrivateKey) GetKeys(string password)
    {
        var (hashedPassword, hashedPasswordBytes) = HashPassword(password);

        if (!_keyStore.KeysExists())
        {
            using var rsa = RSA.Create(512);
            var privateKey = rsa.ExportRSAPrivateKey();
            var privateKeyText = Convert.ToBase64String(privateKey);
            var publicKey = rsa.ExportRSAPublicKey();
            var publicKeyText = Convert.ToBase64String(publicKey);

            var encryptedPrivateCombinedKey = EncryptDataToBase64(privateKeyText, hashedPasswordBytes);
            var encryptedPublicCombinedKey = EncryptDataToBase64(publicKeyText, hashedPasswordBytes);

            _keyStore.SaveEncryptedKeys(encryptedPublicCombinedKey, encryptedPrivateCombinedKey, hashedPassword);

            _publicKey = publicKey;
            _privateKey = privateKey.ToList();

            return (publicKeyText, privateKeyText);
        }
        
        var (publicKeyBase64, privateKeyBase64) = _keyStore.GetEncryptedKeys(hashedPassword);
        string decryptedPrivateKey;
        string decryptedPublicKey;
        try
        {
            decryptedPrivateKey = DecryptDataFromBase64(privateKeyBase64, hashedPasswordBytes);
            decryptedPublicKey = DecryptDataFromBase64(publicKeyBase64, hashedPasswordBytes);
            _publicKey = Convert.FromBase64String(decryptedPublicKey);
            _privateKey = Convert.FromBase64String(decryptedPrivateKey).ToList();
        }
        catch (CryptographicException e)
        {
            throw new Exception($"You entered the wrong password!");
        }

        return (decryptedPublicKey, decryptedPrivateKey);
    }

    public string EncryptDataToBase64(string text, byte[] symmetricEncryptionKey, CipherMode mode = CipherMode.CBC, int blockSize = 128)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Mode = mode;
        aesAlg.BlockSize = blockSize;
        aesAlg.Padding = PaddingMode.PKCS7;
        using var encryptor = aesAlg.CreateEncryptor(symmetricEncryptionKey, aesAlg.IV);
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(text);
        }

        var iv = aesAlg.IV;

        var decryptedContent = msEncrypt.ToArray();

        var result = new byte[iv.Length + decryptedContent.Length];

        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

        return Convert.ToBase64String(result);
    }

    public string DecryptDataFromBase64(string combinedKey, byte[] symmetricEncryptionKey, CipherMode mode = CipherMode.CBC, int blockSize = 128)
    {
        var fullCipher = Convert.FromBase64String(combinedKey);

        var iv = new byte[16];
        var cipher = new byte[fullCipher.Length - 16];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, fullCipher.Length - iv.Length);

        using var aesAlg = Aes.Create();
        aesAlg.Mode = mode;
        aesAlg.BlockSize = blockSize;
        aesAlg.Padding = PaddingMode.PKCS7;
        using var decryptor = aesAlg.CreateDecryptor(symmetricEncryptionKey, iv);
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        var result = srDecrypt.ReadToEnd();

        return result;
    }

    public byte[] GenerateSessionKey()
    {
        var bytes = new byte[32];
        new Random((int) Environment.TickCount64).NextBytes(bytes);
        return bytes;
    }
    
    public void SetSessionKey(List<byte> key)
    {
        _sessionKey = key;
    }

    public void SetReceiverPublicKey(string key)
    {
        _receiverPublicKey = Convert.FromBase64String(key).ToList();
    }

    public string GetSessionKeyEncryptedWithReceiverPublicKey()
    {
        var encryptedKeyBytes = EncryptWithRsa(_sessionKey, _receiverPublicKey);
        return Convert.ToBase64String(encryptedKeyBytes);
    }
    
    public byte[] DecryptSessionKeyWithRsa(string data)
    {
        var key = Convert.FromBase64String(data).ToList();
        var decryptedSessionKey = DecryptWithRsa(key, _privateKey);
        return decryptedSessionKey;
    }

    public byte[] EncryptWithRsa(List<byte> data, List<byte> publicKey)
    {
        using var rsa = RSA.Create(512);
        rsa.ImportRSAPublicKey(publicKey.ToArray(), out _);
        return rsa.Encrypt(data.ToArray(), RSAEncryptionPadding.Pkcs1);
    }
    
    public byte[] DecryptWithRsa(List<byte> data, List<byte> privateKey)
    {
        using var rsa = RSA.Create(512);
        rsa.ImportRSAPrivateKey(privateKey.ToArray(), out _);
        return rsa.Decrypt(data.ToArray(), RSAEncryptionPadding.Pkcs1);
    }
    
    
}