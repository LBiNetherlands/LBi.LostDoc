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

namespace LBi.LostDoc.Templating
{
    public class DependencyProvider : IDependencyProvider
    {
        private readonly Dictionary<Uri, List<Tuple<int, Task<WorkUnitResult>>>> _tasks;
        private readonly CancellationToken _cancellationToken;

        public DependencyProvider(CancellationToken cancellationToken)
        {
            this._tasks = new Dictionary<Uri, List<Tuple<int, Task<WorkUnitResult>>>>();
            this._cancellationToken = cancellationToken;
        }


        public void Add(Uri uri, int order, Task<WorkUnitResult> task)
        {
            List<Tuple<int, Task<WorkUnitResult>>> versionList;
            if (!this._tasks.TryGetValue(uri, out versionList))
                this._tasks.Add(uri, versionList = new List<Tuple<int, Task<WorkUnitResult>>>());

            versionList.Add(Tuple.Create(order, task));
        }

        public bool TryGetDependency(Uri uri, int order, out Stream stream)
        {
            stream = null;
            List<Tuple<int, Task<WorkUnitResult>>> versionList;
            if (this._tasks.TryGetValue(uri, out versionList))
            {
                foreach (Tuple<int, Task<WorkUnitResult>> tuple in versionList)
                {
                    if (tuple.Item1 < order)
                    {
                        Task<WorkUnitResult> task = tuple.Item2;
                        if (!task.IsCompleted)
                        {
                            if (task.Status == TaskStatus.Created)
                                task.RunSynchronously();
                            else
                                task.Wait(this._cancellationToken);
                        }

                        stream = task.Result.GetStream();
                    }
                }
            }

            return stream != null;
        }

        public bool IsFinal(Uri uri, int order)
        {
            List<Tuple<int, Task<WorkUnitResult>>> versionList;
            if (this._tasks.TryGetValue(uri, out versionList))
                return order >= versionList[versionList.Count - 1].Item1;

            throw new KeyNotFoundException(uri.ToString());
        }
    }
}