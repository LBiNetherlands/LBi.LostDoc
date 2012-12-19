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

using System.IO;

namespace LBi.LostDoc.Core.Templating
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