/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;

namespace LBi.LostDoc.Templating
{
    [Export]
    public class TemplateResolver
    {
        public TemplateResolver(params IFileProvider[] fileProviders)
            : this(fileProviders.AsEnumerable())
        {
        }

        [ImportingConstructor]
        public TemplateResolver([ImportMany]IEnumerable<IFileProvider> fileProviders)
        {
            this.Providers = fileProviders.ToArray();
        }

        protected IFileProvider[] Providers { get; set; }

        public IEnumerable<TemplateInfo> GetTemplates()
        {
            foreach (var fileProvider in this.Providers)
            {
                foreach (string directory in fileProvider.GetDirectories("."))
                {
                    string path = Path.Combine(directory, Template.TemplateDefinitionFileName);
                    if (fileProvider.FileExists(path))
                        yield return TemplateInfo.Load(this, fileProvider, directory);
                }
            }
        }

        public TemplateInfo Resolve(string name)
        {
            TemplateInfo ret;
            if (!this.TryResolve(name, out ret))
                throw new Exception("Template not found: '" + name + "'");

            return ret;
        }

        public bool TryResolve(string name, out TemplateInfo templateInfo)
        {
            string path = Path.Combine(name, Template.TemplateDefinitionFileName);
            IFileProvider fileProvider;
            bool ret = this.FileExists(path, out fileProvider);
            if (ret)
                templateInfo = TemplateInfo.Load(this, fileProvider, name);
            else
                templateInfo = null;

            return ret;
        }

        private bool FileExists(string path, out IFileProvider fileProvider)
        {
            bool ret = false;
            fileProvider = null;

            foreach (IFileProvider provider in this.Providers)
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
            foreach (IFileProvider provider in this.Providers)
            {
                ret.Append("Provider: ").AppendLine(provider.GetType().Name);
                ret.AppendLine(provider.ToString());
            }
            return ret.ToString();
        }
    }
}