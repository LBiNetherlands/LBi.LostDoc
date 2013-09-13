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
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using dotless.Core.configuration;
using LBi.LostDoc.Extensibility;

namespace LBi.LostDoc.Templating.Transforms.Less
{
    [ExportTransform("less")]
    public class LessTransform : IResourceTransform
    {
        [ImportingConstructor]
        public LessTransform([Import(ContractNames.ResourceFileProvider)]IFileProvider fileProvider)
        {
            this.FileProvider = fileProvider;
        }

        protected IFileProvider FileProvider { get; set; }

        public Stream Transform(Stream input)
        {
            using (StreamReader reader = new StreamReader(input, true))
            {
                string lessDoc = dotless.Core.Less.Parse(reader.ReadToEnd(),
                                                         new DotlessConfiguration
                                                             {
                                                                 CacheEnabled = false,
                                                                 Debug = false,
                                                                 HandleWebCompression = false,
                                                                 MinifyOutput = false,
                                                                 LessSource = new LessFileReader(this.FileProvider)
                                                             });
                return new MemoryStream(Encoding.UTF8.GetBytes(lessDoc)) { Position = 0 };
            }
        }
    }
}
