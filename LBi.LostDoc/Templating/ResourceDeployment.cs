/*
 * Copyright 2012-2013 DigitasLBi Netherlands B.V.
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
using System.Threading.Tasks;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Templating
{
    public class ResourceDeployment : UnitOfWork
    {
        public ResourceDeployment(IFileProvider fileProvider, string path, string destination, IResourceTransform[] transforms)
            : base(path)
        {
            this.FileProvider = fileProvider;
            this.ResourcePath = path;
            this.Destination = destination;
            this.Transforms = transforms;
        }

        public IResourceTransform[] Transforms { get; protected set; }

        public string Destination { get; protected set; }

        public IFileProvider FileProvider { get; protected set; }

        public string ResourcePath { get; protected set; }

        public override WorkUnitResult Execute(ITemplatingContext context)
        {
            Stopwatch localTimer = new Stopwatch();

            // copy resources to output dir
            string target = this.Destination ?? this.ResourcePath;

            if (this.Destination != null)
            {
                TraceSources.TemplateSource.TraceInformation("Copying resource: {0} => {1}",
                                                             this.ResourcePath,
                                                             this.Destination);
            }
            else
                TraceSources.TemplateSource.TraceInformation("Copying resource: {0}", this.ResourcePath);


            using (Stream streamSrc = this.FileProvider.OpenFile(this.ResourcePath, FileMode.Open))
            using (Stream streamDest = context.OutputFileProvider.OpenFile(target, FileMode.Create))
            {
                Stream outStream = streamSrc;
                for (int i = 0; i < this.Transforms.Length; i++)
                {
                    TraceSources.TemplateSource.TraceInformation("Applying '{0}' to resource: {1}",
                                                                 this.Transforms[i].GetType().Name,
                                                                 this.ResourcePath);
                    Stream oldStream = outStream;
                    outStream = this.Transforms[i].Transform(outStream);
                    oldStream.Dispose();
                }
                outStream.CopyTo(streamDest);
                streamDest.Close();
                outStream.Dispose();
            }

            return new WorkUnitResult(context.OutputFileProvider,
                                      this,
                                      (long)Math.Round(((double) localTimer.ElapsedTicks/Stopwatch.Frequency)*1, 000, 000));
        }
    }
}
