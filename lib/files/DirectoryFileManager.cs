using System.Runtime.InteropServices;

namespace KeyStore.files;

public class DirectoryFileManager : IFileManager
{
    private readonly string separator;
    private readonly string fileDir;
    private string filePath;

    public DirectoryFileManager()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            separator = "/";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            separator = "\\";
        }
        
        fileDir = @$"{Environment.CurrentDirectory}{separator}files";
        Directory.CreateDirectory(fileDir);
    }

    public void SaveFile(byte[] data, string fileName)
    {
        filePath = $"{fileDir}{separator}{fileName}";
        File.WriteAllBytes(filePath, data);
    }

    public byte[] GetFile(string fileName)
    {
        filePath = $"{fileDir}{separator}{fileName}";
        return File.ReadAllBytes(filePath);
    }

    public void DeleteFile(string fileName)
    {
        filePath = $"{fileDir}{separator}{fileName}";
        if (File.Exists(filePath))
        {
            File.Delete(filePath);     
        }
    }

    public void DeleteAllFiles()
    {
        var files = Directory.GetFiles(fileDir);
        foreach (var file in files)
        {
            File.Delete(file);
        }
    }
    
}