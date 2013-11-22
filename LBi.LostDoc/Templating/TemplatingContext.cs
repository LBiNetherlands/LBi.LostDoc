/*
 * Copyright 2012 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Runtime.Caching;
using System.Xml.Linq;
using System.Xml.XPath;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class TemplatingContext : ITemplatingContext
    {
        public TemplatingContext(ObjectCache cache, CompositionContainer container, IFileProvider outputFileProvider, TemplateData data, IEnumerable<IAssetUriResolver> resolvers, IFileProvider templateFileProvider)
        {
            this.Container = container;
            this.OutputFileProvider = outputFileProvider;
            this.TemplateData = data;
            this.AssetUriResolvers = resolvers.ToArray();
            this.TemplateFileProvider = templateFileProvider;
            this.Cache = cache;

            XPathDocument xpathDoc;
            using (var reader = data.Document.CreateReader(ReaderOptions.OmitDuplicateNamespaces))
                xpathDoc = new XPathDocument(reader);
            this.Document = xpathDoc.CreateNavigator();
            this.DocumentIndex = new XPathNavigatorIndex(this.Document.Clone());
        }

        #region ITemplatingContext Members

        public string BasePath { get; protected set; }

        public TemplateData TemplateData { get; protected set; }
        public XPathNavigatorIndex DocumentIndex { get; protected set; }
        public XPathNavigator Document { get; protected set; }

        public IAssetUriResolver[] AssetUriResolvers { get; protected set; }

        public IFileProvider TemplateFileProvider { get; protected set; }
        public IFileProvider OutputFileProvider { get; private set; }

        #endregion

        public ObjectCache Cache { get; private set; }

        public CompositionContainer Container { get; private set; }
    }
}
