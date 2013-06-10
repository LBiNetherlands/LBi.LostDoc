/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LBi.LostDoc.Templating.FileProviders
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
        
        private string ConvertPath(string path)
        {
            return this._ns + path.Replace('\\', '.').Replace('/', '.');
        }

        #region IReadOnlyFileProvider Members

        public bool FileExists(string path)
        {
            return this._asm.GetManifestResourceInfo(this.ConvertPath(path)) != null;
        }

        public Stream OpenFile(string path, FileMode mode)
        {
            if (mode != FileMode.Open)
                throw new ArgumentOutOfRangeException("mode", "Only FileMode.Open is supported.");

            var ret = this._asm.GetManifestResourceStream(this.ConvertPath(path.TrimStart('/')));
            if (ret == null)
                throw new FileNotFoundException(string.Format("Resource not found: {0} (Was: {1})", this.ConvertPath(path), path), path);
            return ret;
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return this._asm.GetManifestResourceNames().Where(n => this.ConvertPath(n).StartsWith(path + '.'));
        }

        #endregion

        public override string ToString()
        {
            return string.Format("[{0}]{1}", this._asm.GetName().Name, this._ns);
        }
    }
}
