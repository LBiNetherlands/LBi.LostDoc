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

using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using LBi.LostDoc.Core.Templating.XPath;

namespace LBi.LostDoc.Core.Templating
{
    public class TemplateData
    {
        public TemplateData(XDocument doc, AssetRedirectCollection assetRedirects)
        {
            this.XDocument = doc;
            XPathDocument xpathDoc;
            using (var reader = doc.CreateReader(ReaderOptions.OmitDuplicateNamespaces))
                xpathDoc = new XPathDocument(reader);
            this.Document = xpathDoc.CreateNavigator();
            this.DocumentIndex = new XPathNavigatorIndex(this.Document.Clone());
            this.AssetRedirects = assetRedirects;
            this.Arguments = new Dictionary<string, object>();
        }
        public XPathNavigatorIndex DocumentIndex { get; protected set; }
        public XPathNavigator Document { get; protected set; }
        public XDocument XDocument { get; protected set; }
        public AssetRedirectCollection AssetRedirects { get; protected set; }

        public VersionComponent? IgnoredVersionComponent { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
        public string TargetDirectory { get; set; }
        public bool OverwriteExistingFiles { get; set; }
    }
}
