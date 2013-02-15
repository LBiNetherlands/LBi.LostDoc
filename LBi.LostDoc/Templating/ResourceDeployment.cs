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
using System.Diagnostics;
using System.IO;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Templating
{
    public class ResourceDeployment : UnitOfWork
    {
        
        public ResourceDeployment(IReadOnlyFileProvider fileProvider, string path)
        {
            this.FileProvider = fileProvider;
            this.ResourcePath = path;
        }

        public IReadOnlyFileProvider FileProvider { get; protected set; }

        public string ResourcePath { get; protected set; }

        public override WorkUnitResult Execute(ITemplatingContext context)
        {
            Stopwatch localTimer = new Stopwatch();
            string rootPath = Path.GetFullPath(context.TemplateData.TargetDirectory);
            // copy resources to output dir

            string target = Path.Combine(rootPath, this.ResourcePath);
            string targetDir = Path.GetDirectoryName(target);

            TraceSources.TemplateSource.TraceInformation("Copying resource: {0}", this.ResourcePath);

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            using (Stream streamSrc = this.FileProvider.OpenFile(this.ResourcePath))
            using (Stream streamDest = File.Create(target))
            {
                streamSrc.CopyTo(streamDest);
                streamDest.Close();
                streamSrc.Close();
            }

            return new WorkUnitResult
                       {
                           WorkUnit = this,
                           Duration = (long)Math.Round((double)(localTimer.ElapsedTicks/Stopwatch.Frequency)*1000000)
                       };
        }
    }
}
