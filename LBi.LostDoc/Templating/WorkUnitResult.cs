/*
 * Copyright 2012-2014 DigitasLBi Netherlands B.V.
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

namespace LBi.LostDoc.Templating
{
    public class WorkUnitResult
    {
        public WorkUnitResult(IFileProvider fileProvder, UnitOfWork unitOfWork, long duration)
        {
            this.FileProvider = fileProvder;
            this.WorkUnit = unitOfWork;
            this.Duration = duration;
        }

        public string Path { get { return this.WorkUnit.Path; } }

        public IFileProvider FileProvider { get; protected set; }

        public UnitOfWork WorkUnit { get; protected set; }

        /// <summary>
        ///   Micro seconds.
        /// </summary>
        public long Duration { get; protected set; }

        public virtual Stream GetStream()
        {
            return this.FileProvider.OpenFile(this.Path, FileMode.Open);
        }
    }
}
