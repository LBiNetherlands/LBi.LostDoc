using System.Collections.Generic;
using System.IO;

namespace LBi.LostDoc.Templating.IO
{
    public class VersionedFileProvider : IFileProvider
    {
        public bool FileExists(string path)
        {
            throw new System.NotImplementedException();
        }

        public Stream OpenFile(string path, FileMode mode)
        {
            throw new System.NotImplementedException();
        }

        public bool SupportsDiscovery { get; private set; }

        public IEnumerable<string> GetDirectories(string path)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<string> GetFiles(string path)
        {
            throw new System.NotImplementedException();
        }
    }
}