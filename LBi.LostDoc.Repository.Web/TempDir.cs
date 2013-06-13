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

namespace LBi.LostDoc.Repository.Web
{
    /// <summary>
    /// A temporary dir wrapper
    /// </summary>
    public class TempDir : IDisposable
    {
        private readonly DirectoryInfo _dir;

        public TempDir(string basePath)
        {
            string path = System.IO.Path.Combine(basePath, Guid.NewGuid().ToString());
            if (Directory.Exists(path))
                throw new ArgumentException("Directoy already exists: " + path);

            this._dir = Directory.CreateDirectory(path);
        }

        public string Path
        {
            get { return this._dir.FullName; }
        }

        public void Dispose()
        {
            this._dir.Delete(true);
        }
    }
}
