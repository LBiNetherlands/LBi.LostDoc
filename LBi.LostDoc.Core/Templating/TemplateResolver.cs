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
using System.Linq;

namespace LBi.LostDoc.Core.Templating
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

        //protected internal string ResolvePath(string nameOrPath)
        //{
        //    string ret ;
        //    IReadOnlyFileProvider fileProvider;
        //    const string definitionFileName = "template.xml";
            
        //    if (this.FileExists(System.IO.Path.Combine(nameOrPath, definitionFileName), out fileProvider))
        //        ret = System.IO.Path.Combine(nameOrPath, definitionFileName);
        //    else
        //        ret = null;

        //    return ret;
        //}

        // template will have to be able to load inherited template files from its' fileprovider!

        private bool FileExists(string path, out IReadOnlyFileProvider fileProvider)
        {
            bool ret = false;

            foreach (IReadOnlyFileProvider provider in this.Providers)
            {
                if (provider.FileExists(path))
                {
                    fileProvider = provider;
                    ret = true;
                    break;
                }
             }

            if (!ret)
                fileProvider = null;

            return ret;
        }

        public Template Resolve(string name)
        {
            Template ret;
            ret = new Template(this);
            return ret;
        }
    }
}