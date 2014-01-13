/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition.Primitives;
using System.Runtime.Caching;
using System.Xml.Linq;
using System.Xml.Xsl;
using LBi.LostDoc.Templating.AssetResolvers;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class TemplateContext : ITemplateContext
    {
        private readonly IUniqueUriFactory _uniqueUriFactory;
        private readonly FileResolver _fileResolver;

        public TemplateContext(ObjectCache cache,
                               XDocument document,
                               CustomXsltContext xsltContext,
                               IUniqueUriFactory uniqueUriFactory,
                               FileResolver fileResolver,
                               ComposablePartCatalog catalog)
        {
            this.Cache = cache;
            this.XsltContext = xsltContext;
            this.Document = document;
            this._uniqueUriFactory = uniqueUriFactory;
            this._fileResolver = fileResolver;
            this.Catalog = catalog;
        }

        public ObjectCache Cache { get; private set; }

        public ComposablePartCatalog Catalog { get; private set; }

        public XDocument Document { get; private set; }

        public CustomXsltContext XsltContext { get; private set; }
        
        public void EnsureUniqueUri(ref Uri uri)
        {
            this._uniqueUriFactory.EnsureUnique(ref uri);
        }

        public void RegisterAssetUri(AssetIdentifier assetId, Uri uri)
        {
            this._fileResolver.Add(assetId.AssetId, assetId.Version, uri);
        }

        
    }

    public interface ITemplateContext : IContextBase
    {
        CustomXsltContext XsltContext { get; }

        XDocument Document { get; }

        void EnsureUniqueUri(ref Uri uri);

        void RegisterAssetUri(AssetIdentifier assetId, Uri uri);
    }

    public interface ITemplateDirective<out TUnitOfWork> where TUnitOfWork : UnitOfWork
    {
        IEnumerable<TUnitOfWork> DiscoverWork(ITemplateContext context);
    }
}