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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LBi.LostDoc.Templating
{
    public class TemplateResolver
    {
        public TemplateResolver(params IReadOnlyFileProvider[] fileProviders)
            : this(fileProviders.AsEnumerable())
        {
        }

        public TemplateResolver(IEnumerable<IReadOnlyFileProvider> fileProviders)
        {
            this.Providers = fileProviders.ToArray();
        }

        protected IReadOnlyFileProvider[] Providers { get; set; }


        public bool Resolve(string name, out IReadOnlyFileProvider fileProvider, out string path)
        {
            path = Path.Combine(name, Template.TemplateDefinitionFileName);
            return this.FileExists(path, out fileProvider);
        }

        private bool FileExists(string path, out IReadOnlyFileProvider fileProvider)
        {
            bool ret = false;
            fileProvider = null;

            foreach (IReadOnlyFileProvider provider in this.Providers)
            {
                if (provider.FileExists(path))
                {
                    fileProvider = provider;
                    ret = true;
                    break;
                }
             }
            return ret;
        }

        public override string ToString()
        {
            StringBuilder ret = new StringBuilder();
            foreach (IReadOnlyFileProvider provider in this.Providers)
            {
                ret.Append("Provider: ").AppendLine(provider.GetType().Name);
                ret.AppendLine(provider.ToString());
            }
            return ret.ToString();
        }
    }
}