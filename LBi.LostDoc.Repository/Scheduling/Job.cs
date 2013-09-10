/*
 * Copyright 2013 LBi Netherlands B.V.
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
using System.Threading;

namespace LBi.LostDoc.Repository.Scheduling
{
    public class Job : IJob
    {
        private readonly Action<CancellationToken> _action;

        public Job(string name, Action<CancellationToken> action)
        {
            this.Name = name;
            this._action = action;
            this.Created = DateTimeOffset.UtcNow;
        }

        public string Name { get; private set; }

        public DateTimeOffset Created { get; private set; }

        public DateTimeOffset? Started { get; private set; }

        public void Execute(CancellationToken cancellationToken)
        {
            this.Started = DateTimeOffset.UtcNow;
            this._action(cancellationToken);
        }
    }
}