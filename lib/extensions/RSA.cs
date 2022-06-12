using System.Security.Cryptography;
using System.Text;

public static class RSAExtension 
{
    // Exports the public key
    public static string ExportPublicKeyPem(this RSA rsa) {
        var buffer = new StringBuilder();
        buffer.AppendLine("-----BEGIN RSA PUBLIC KEY-----");
        buffer.AppendLine(Convert.ToBase64String(
            rsa.ExportRSAPublicKey(),
            Base64FormattingOptions.InsertLineBreaks));
        buffer.AppendLine("-----END RSA PUBLIC KEY-----");
        return buffer.ToString();
    }
}