/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
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
using LBi.LostDoc.Templating.IO;

namespace LBi.LostDoc.Templating
{
    public class FileReference
    {
        public FileReference(int order, IFileProvider fileProvider, string path)
        {
            this.Order = order;
            this.FileProvider = fileProvider;
            this.Path = path;
        }

        public string Path { get; protected set; }

        public IFileProvider FileProvider { get; protected set; }

        public int Order { get; protected set; }

        public virtual Stream GetStream(FileMode fileMode = FileMode.Open)
        {
            return this.FileProvider.OpenFile(this.Path, fileMode);
        }
    }
}