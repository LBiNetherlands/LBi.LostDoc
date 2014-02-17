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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LBi.LostDoc.Templating.IO;

namespace LBi.LostDoc.Templating
{
    public class DependencyProvider : IDependencyProvider
    {
        private readonly Dictionary<Uri, OrdinalResolver<FileReference>> _tasks;
        private readonly CancellationToken _cancellationToken;
        private readonly StorageResolver _storageResolver;

        public DependencyProvider(StorageResolver storageResolver, CancellationToken cancellationToken)
        {
            this._storageResolver = storageResolver;
            this._tasks = new Dictionary<Uri, OrdinalResolver<FileReference>>();
            this._cancellationToken = cancellationToken;
        }


        public void Add(Uri uri, int ordinal, Task<WorkUnitResult> task)
        {
            OrdinalResolver<FileReference> resolver;
            if (!this._tasks.TryGetValue(uri, out resolver))
                this._tasks.Add(uri, resolver = new OrdinalResolver<FileReference>(this.CreateFallbackEvaluator(uri)));

            resolver.Add(ordinal, this.CreateTaskEvaluator(task));
        }



        public Stream GetDependency(Uri uri, int ordinal)
        {
            Stream ret = null;
            OrdinalResolver<FileReference> resolver;
            if (this._tasks.TryGetValue(uri, out resolver))
            {
                FileReference fileRef = resolver.Resolve(ordinal).Value;
                ret = fileRef.GetStream(FileMode.Open);
            }
            else
                ret = this.GetFallback(uri).GetStream(FileMode.Open);

            return ret;
        }

        public bool IsFinal(Uri uri, int ordinal)
        {
            OrdinalResolver<FileReference> resolver;
            if (this._tasks.TryGetValue(uri, out resolver))
                return resolver.IsFinal(ordinal);

            throw new KeyNotFoundException(uri.ToString());
        }

        private Lazy<FileReference> CreateTaskEvaluator(Task<WorkUnitResult> task)
        {
            return new Lazy<FileReference>(
                () =>
                {
                    if (!task.IsCompleted)
                    {
                        if (task.Status == TaskStatus.Created)
                            task.RunSynchronously();
                        else
                            task.Wait(this._cancellationToken);
                    }

                    return task.Result.FileReference;
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private Lazy<FileReference> CreateFallbackEvaluator(Uri uri)
        {
            return new Lazy<FileReference>(() => this.GetFallback(uri), LazyThreadSafetyMode.None);
        }

        private FileReference GetFallback(Uri uri)
        {
            FileReference fileRef = this._storageResolver.Resolve(uri);

            if (!fileRef.Exists)
                throw new FileNotFoundException(string.Format("File not found: {0} ({1})", uri, fileRef.Path),
                                                fileRef.Path);

            return fileRef;
        }
    }
}