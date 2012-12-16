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
