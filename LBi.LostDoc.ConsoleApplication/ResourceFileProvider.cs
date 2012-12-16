using System;
using System.IO;
using System.Reflection;
using LBi.LostDoc.Core.Templating;

namespace LBi.LostDoc.ConsoleApplication
{
    public class ResourceFileProvider : IFileProvider
    {
        private Assembly _asm;
        private string _ns;

        public ResourceFileProvider(string ns) : this(ns, Assembly.GetCallingAssembly())
        {
        }

        public ResourceFileProvider(string ns, Assembly asm)
        {
            if (!string.IsNullOrEmpty(ns))
                this._ns = ns + '.';
            else
                this._ns = ns;

            this._asm = asm;
        }

        #region IFileProvider Members

        public bool FileExists(string path)
        {
            return this._asm.GetManifestResourceInfo(this.ConvertPath(path)) != null;
        }

        public Stream OpenFile(string path)
        {
            return this._asm.GetManifestResourceStream(this.ConvertPath(path));
        }

        public Stream CreateFile(string path)
        {
            throw new NotSupportedException();
        }

        #endregion

        private string ConvertPath(string path)
        {
            return this._ns + path.Replace('\\', '.').Replace('/', '.');
        }
    }
}