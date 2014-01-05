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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LBi.LostDoc.Templating
{
    public class DependencyProvider : IDependencyProvider, IFileProvider
    {
        private readonly ConcurrentDictionary<Uri, Task<WorkUnitResult>> _tasks;
        private readonly CancellationToken _cancellationToken;

        public DependencyProvider(CancellationToken cancellationToken)
        {
            this._tasks = new ConcurrentDictionary<Uri, Task<WorkUnitResult>>();
            this._cancellationToken = cancellationToken;
        }


        public void Add(Uri uri, Task<WorkUnitResult> task)
        {
            if (!this._tasks.TryAdd(uri, task))
                throw new DuplicateNameException("Uri already added: " + uri.ToString());
        }

        public Stream GetDependency(Uri uri)
        {
            Task<WorkUnitResult> task;
            if (this._tasks.TryGetValue(uri, out task))
            {
                if (!task.IsCompleted)
                    task.Wait(this._cancellationToken);

                return task.Result.GetStream();
            }
            
            throw new KeyNotFoundException("Uri doesn't exist: " + uri.ToString());
        }

        public bool Exists(Uri uri)
        {
            return this._tasks.ContainsKey(uri);
        }

        bool IFileProvider.FileExists(string path)
        {
            return this.Exists(new Uri(path, UriKind.RelativeOrAbsolute));
        }

        Stream IFileProvider.OpenFile(string path, FileMode mode)
        {
            if (mode != FileMode.Open)
                throw new NotSupportedException();

            return this.GetDependency(new Uri(path, UriKind.RelativeOrAbsolute));
        }

        bool IFileProvider.SupportsDiscovery
        {
            get { return false; }
        }

        IEnumerable<string> IFileProvider.GetDirectories(string path)
        {
            throw new NotSupportedException();
        }

        IEnumerable<string> IFileProvider.GetFiles(string path)
        {
            throw new NotSupportedException();
        }
    }
}