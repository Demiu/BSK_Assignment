namespace KeyStore.files;

public interface IFileManager
{
    void SaveFile(byte[] data, string fileName);
    byte[] GetFile(string fileName);
    void DeleteFile(string fileName);
    void DeleteAllFiles();
}