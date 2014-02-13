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
using System.ComponentModel.Composition.Primitives;
using System.Runtime.Caching;
using System.Xml.Linq;
using LBi.LostDoc.Templating.AssetResolvers;
using LBi.LostDoc.Templating.IO;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class TemplateContext : ITemplateContext
    {
        private readonly IFileResolver _fileResolver;

        public TemplateContext(ObjectCache cache,
                               XDocument document,
                               CustomXsltContext xsltContext,
                               IFileResolver fileResolver,
                               ComposablePartCatalog catalog,
                               IFileProvider templateFileProvider,
                               StorageResolver storage,
                               IDependencyProvider dependencyProvider)
        {
            this.Cache = cache;
            this.XsltContext = xsltContext;
            this.Document = document;
            this._fileResolver = fileResolver;
            this.DependencyProvider = dependencyProvider;
            this.TemplateFileProvider = templateFileProvider;
            this.Catalog = catalog;
            this.Storage = storage;
        }

        public ObjectCache Cache { get; private set; }

        public ComposablePartCatalog Catalog { get; private set; }

        public XDocument Document { get; private set; }

        public IDependencyProvider DependencyProvider { get; private set; }

        public StorageResolver Storage { get; private set; }

        public IFileProvider TemplateFileProvider { get; private set; }

        public CustomXsltContext XsltContext { get; private set; }

        public void RegisterAssetUri(AssetIdentifier assetId, Uri uri)
        {
            this._fileResolver.Add(assetId.AssetId, assetId.Version, uri);
        }


    }
}