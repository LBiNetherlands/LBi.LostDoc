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

using System.Collections.Generic;
using System.IO;

namespace LBi.LostDoc.Templating.IO
{
    public class NullFileProvider : IFileProvider
    {
        static NullFileProvider()
        {
            Instance = new NullFileProvider();
        }

        public static NullFileProvider Instance { get; private set; }

        public bool FileExists(string path)
        {
            return false;
        }

        public Stream OpenFile(string path, FileMode mode)
        {
            return new MemoryStream();
        }

        public bool SupportsDiscovery { get { return false; } }

        public IEnumerable<string> GetDirectories(string path)
        {
            throw new System.NotSupportedException();
        }

        public IEnumerable<string> GetFiles(string path)
        {
            throw new System.NotSupportedException();
        }
    }
}