/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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
using LBi.LostDoc.Composition;

namespace LBi.LostDoc.Templating.FileProviders
{
    [Export(ContractNames.TemplateProvider, typeof(IFileProvider))]
    public class DirectoryFileProvider : IFileProvider
    {
        public DirectoryFileProvider()
        {
        }

        #region IReadOnlyFileProvider Members

        public virtual bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public virtual Stream OpenFile(string path, FileMode mode)
        {
            return File.Open(path, mode);
        }

        #endregion

    }
}
