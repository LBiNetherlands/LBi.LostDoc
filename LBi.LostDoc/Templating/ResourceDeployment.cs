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
using System.IO;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Templating
{
    public class ResourceDeployment : UnitOfWork
    {
        public ResourceDeployment(Uri input, Uri output, int ordinal, IResourceTransform[] transforms)
            : base(output, ordinal)
        {
            this.Input = input;
            this.Transforms = transforms;
        }

        public IResourceTransform[] Transforms { get; protected set; }

        public Uri Input { get; protected set; }

        public override void Execute(ITemplatingContext context, Stream outputStream)
        {
            // copy resources to output dir
            TraceSources.TemplateSource.TraceInformation("Copying resource: {0} => {1}",
                                                         this.Input,
                                                         this.Output);

            var inputFileRef = context.StorageResolver.Resolve(this.Input);

            Stream streamSrc = inputFileRef.GetStream(FileMode.Open);
            Stream outStream = streamSrc;
            for (int i = 0; i < this.Transforms.Length; i++)
            {
                TraceSources.TemplateSource.TraceInformation("Applying '{0}' to resource: {1}",
                                                             this.Transforms[i].GetType().Name,
                                                             this.Input);
                Stream oldStream = outStream;
                outStream = this.Transforms[i].Transform(outStream);
                oldStream.Dispose();
            }
            outStream.CopyTo(outputStream);
            outputStream.Close();
            outStream.Dispose();
        }
    }
}
