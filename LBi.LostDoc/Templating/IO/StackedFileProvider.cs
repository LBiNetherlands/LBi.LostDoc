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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LBi.LostDoc.Templating.IO
{
    public class StackedFileProvider : IFileProvider
    {
        private readonly IFileProvider[] _stack;

        public StackedFileProvider(params IFileProvider[] stack)
        {
            this._stack = stack;
        }

        public StackedFileProvider(IEnumerable<IFileProvider> stack)
            : this(new[] { stack.ToArray() })
        {
        }

        public bool FileExists(string path)
        {
            return this._stack.Any(fp => fp.FileExists(path));
        }

        public Stream OpenFile(string path, FileMode mode)
        {
            if (mode != FileMode.Open)
                throw new NotSupportedException("Read Only");

            IFileProvider provider = this._stack.FirstOrDefault(fp => fp.FileExists(path));
            if (provider == null)
                throw new FileNotFoundException("Resource not found: " + path, path);

            return provider.OpenFile(path, mode);
        }

        public bool SupportsDiscovery
        {
            get { return false; }
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            throw new NotSupportedException("Discovery is not supported.");
        }

        public IEnumerable<string> GetFiles(string path)
        {
            throw new NotSupportedException("Discovery is not supported.");
        }
    }
}