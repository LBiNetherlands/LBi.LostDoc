using System.IO;

namespace LBi.LostDoc.Core.Templating
{
    public interface IFileProvider
    {
        bool FileExists(string path);
        Stream OpenFile(string path);
        Stream CreateFile(string path);
    }
}