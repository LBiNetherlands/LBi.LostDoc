using System.IO;
using LBi.LostDoc.Core.Templating;

namespace LBi.LostDoc.ConsoleApplication
{
    public class DirectoryFileProvider : IFileProvider
    {
        // private string _basePath;
        #region IFileProvider Members

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public Stream OpenFile(string path)
        {
            // return File.Open(Path.Combine(this._basePath, path), FileMode.Open);
            return File.Open(path, FileMode.Open);
        }

        public Stream CreateFile(string path)
        {
            // return File.Create(Path.Combine(this._basePath, path));
            return File.Create(path);
        }

        #endregion
    }
}