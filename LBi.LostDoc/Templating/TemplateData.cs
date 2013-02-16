/*
 * Copyright 2012,2013 LBi Netherlands B.V.
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
using System.Xml.Linq;

namespace LBi.LostDoc.Templating
{
    public class TemplateData
    {
        public TemplateData(XDocument doc)
        {
            this.XDocument = doc;
       
            this.AssetRedirects = new AssetRedirectCollection();
            this.Arguments = new Dictionary<string, object>();
            this.Filters = uow => true;
        }
        public XDocument XDocument { get; protected set; }
        public AssetRedirectCollection AssetRedirects { get; set; }
        public Func<UnitOfWork, bool> Filters { get; set; } 
        public VersionComponent? IgnoredVersionComponent { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
        public string TargetDirectory { get; set; }
        public bool OverwriteExistingFiles { get; set; }
    }
}
