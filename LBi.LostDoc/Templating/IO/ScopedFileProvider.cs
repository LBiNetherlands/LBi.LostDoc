/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LBi.LostDoc.Templating.IO
{
    public class ScopedFileProvider : IFileProvider
    {
        private static readonly char[] DirectorySeperatorChars = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public ScopedFileProvider(IFileProvider fileProvider, string basePath)
        {
            this.FileProvider = fileProvider;
            this.BasePath = basePath;
        }

        public IFileProvider FileProvider { get; protected set; }

        public string BasePath { get; protected set; }

        public bool FileExists(string path)
        {
            return this.FileProvider.FileExists(Path.Combine(this.BasePath, path));
        }

        public Stream OpenFile(string path, FileMode mode)
        {
            return this.FileProvider.OpenFile(Path.Combine(this.BasePath, path), mode);
        }

        public bool SupportsDiscovery
        {
            get { return this.FileProvider.SupportsDiscovery; }
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            string fullPath = Path.Combine(this.BasePath, path);
            return this.FileProvider
                       .GetDirectories(fullPath)
                       .Select(p => p.Substring(fullPath.Length).TrimStart(DirectorySeperatorChars));
        }

        public IEnumerable<string> GetFiles(string path)
        {
            string fullPath = Path.Combine(this.BasePath, path);
            return this.FileProvider
                       .GetFiles(fullPath)
                       .Select(p => p.Substring(fullPath.Length).TrimStart(DirectorySeperatorChars));
        }
    }
}