/*
 * Copyright 2012-2014 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Runtime.Caching;
using System.Threading;
using LBi.LostDoc.Templating.AssetResolvers;

namespace LBi.LostDoc.Templating
{
    public class TemplateSettings
    {
        public TemplateSettings()
        {
            this.AssetRedirects = new AssetRedirectCollection();
            this.Arguments = new Dictionary<string, object>();
            this.Filter = null;
            this.UriFactory = new DefaultUniqueUriFactory();
            this.Cache = new MemoryCache("LostDoc");
            this.FileResolver = new FileResolver(caseSensitiveFs: false);
            this.Catalog = new ApplicationCatalog();
            this.UriResolvers = new List<IAssetUriResolver>();
            this.UriResolvers.Add(new MsdnResolver());
            this.CancellationToken = CancellationToken.None;
        }

        public AssetRedirectCollection AssetRedirects { get; set; }
        public Func<UnitOfWork, bool> Filter { get; set; } 
        public VersionComponent? IgnoredVersionComponent { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
        public IFileProvider OutputFileProvider { get; set; }
        public bool OverwriteExistingFiles { get; set; }
        public IUniqueUriFactory UriFactory { get; set; }
        public ObjectCache Cache { get; set; }
        public IFileResolver FileResolver { get; set; }
        public ComposablePartCatalog Catalog { get; set; }
        public List<IAssetUriResolver> UriResolvers { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
