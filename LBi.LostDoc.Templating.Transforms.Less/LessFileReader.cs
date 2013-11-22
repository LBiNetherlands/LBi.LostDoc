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


using System.IO;
using dotless.Core.Input;

namespace LBi.LostDoc.Templating.Transforms.Less
{
    public class LessFileReader : IFileReader
    {
        private readonly IFileProvider _fileProvider;

        public LessFileReader(IFileProvider fileProvider)
        {
            this._fileProvider = fileProvider;
        }

        public byte[] GetBinaryFileContents(string fileName)
        {
            using (var fileStream = this._fileProvider.OpenFile(fileName, FileMode.Open))
            using (var memoryStream = new MemoryStream())
            {
                fileStream.CopyTo(memoryStream);
                fileStream.Close();
                return memoryStream.ToArray();
            }
        }

        public string GetFileContents(string fileName)
        {
            using (var fileStream = this._fileProvider.OpenFile(fileName, FileMode.Open))
            using (TextReader reader = new StreamReader(fileStream))
            {
                return reader.ReadToEnd();
            }
        }

        public bool DoesFileExist(string fileName)
        {
            return this._fileProvider.FileExists(fileName);
        }

        public bool UseCacheDependencies
        {
            get { return false; }
        }
    }
}