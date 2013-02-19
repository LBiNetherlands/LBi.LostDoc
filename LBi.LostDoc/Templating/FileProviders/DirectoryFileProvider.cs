/*
 * Copyright 2012,2013 LBi Netherlands B.V.
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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using LBi.LostDoc.Composition;

namespace LBi.LostDoc.Templating.FileProviders
{
    [Export(ContractNames.TemplateProvider, typeof(IReadOnlyFileProvider))]
    public class DirectoryFileProvider : IReadOnlyFileProvider
    {
        public DirectoryFileProvider()
            : this(Enumerable.Empty<string>())
        {
        }

        public DirectoryFileProvider(params string[] paths)
            : this(paths.AsEnumerable())
        {
        }

        private DirectoryFileProvider(IEnumerable<string> paths)
        {
            this.SearchPaths = paths;
        }

        protected IEnumerable<string> SearchPaths { get; set; }

        protected virtual IEnumerable<string> GeneratePaths(string path)
        {
            yield return path;

            if (!Path.IsPathRooted(path))
            {
                foreach (string basePath in this.SearchPaths)
                    yield return Path.Combine(basePath, path);
            }
        }

        #region IReadOnlyFileProvider Members

        public virtual bool FileExists(string path)
        {
            return this.GeneratePaths(path)
                       .FirstOrDefault(File.Exists) != null;
        }

        public virtual Stream OpenFile(string path)
        {
            string filePath = this.GeneratePaths(path)
                                  .FirstOrDefault(File.Exists);
            if (filePath != null)
            {
                throw new FileNotFoundException("Tried the following paths: {0}",
                                                string.Join(", ", this.GeneratePaths(path)));
            }

            return File.Open(path, FileMode.Open);
        }

        #endregion

        public override string ToString()
        {
            StringBuilder ret = new StringBuilder();
            foreach (var searchPath in this.SearchPaths)
                ret.AppendLine(searchPath);
            return ret.ToString();
        }
    }
}
