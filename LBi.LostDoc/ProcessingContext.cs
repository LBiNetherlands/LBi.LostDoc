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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Xml.Linq;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Reflection;

namespace LBi.LostDoc
{
    public class ProcessingContext : IProcessingContext
    {
        private readonly IAssetFilter[] _filters;
        private readonly HashSet<Asset> _references;

        public ProcessingContext(ObjectCache cache, CompositionContainer container, IEnumerable<IAssetFilter> filters, IAssemblyLoader assemblyLoader, XElement element, HashSet<Asset> references, int phase, IAssetExplorer assetExplorer)
        {
            this._filters = filters.ToArray();
            this.Element = element;
            this._references = references;
            AssetExplorer = assetExplorer;
            Container = container;
            this.Phase = phase;
            this.Cache = cache;
            this.AssemblyLoader = assemblyLoader;
        }

        #region IProcessingContext Members

        public IAssetExplorer AssetExplorer { get; private set; }

        public IAssemblyLoader AssemblyLoader { get; private set; }

        public ObjectCache Cache { get; private set; }

        public CompositionContainer Container { get; private set; }

        public XElement Element { get; private set; }

        public IEnumerable<Asset> References
        {
            get { return this._references; }
        }

        public bool AddReference(Asset asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            //if (string.IsNullOrEmpty(asset))
            //    throw new ArgumentException("Argument cannot be null or empty.", "asset");

            //AssetIdentifier aid = AssetIdentifier.Parse(asset);

            //Debug.Assert(aid.ToString().Equals(asset, StringComparison.Ordinal),
            //             "AssetIdentifier '{0}' failed to roundtrip.", asset);

            //if (aid.AssetId[aid.AssetId.Length - 1] == ']')
            //    aid = new AssetIdentifier(aid.AssetId.Substring(0, aid.AssetId.LastIndexOf('[')),
            //                              aid.Version);

            //object resolve = this.AssetResolver.Resolve(aid);
            //Debug.Assert(resolve != null);

            //Debug.Assert(!string.IsNullOrWhiteSpace(aid.AssetId));

            if (this._references.Add(asset))
            {
                TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose, 1, "Reference: {0}", asset.Id);
                return true;
            }

            return false;
        }

        public int Phase { get; private set; }

        public IProcessingContext Clone(XElement newElement)
        {
            return new ProcessingContext(this.Cache,
                                         this.Container,
                                         this._filters,
                                         this.AssemblyLoader,
                                         newElement,
                                         this._references,
                                         this.Phase,
                                         this.AssetExplorer);
        }

        public bool IsFiltered(Asset asset)
        {
            IFilterContext filterContext = new FilterContext(this.Cache, this.Container, FilterState.Generating);
            for (int i = 0; i < this._filters.Length; i++)
            {
                if (this._filters[i].Filter(filterContext, asset))
                {
                    TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose,
                                        0,
                                        "{0} - Filtered by {1}",
                                        asset.Id.AssetId, this._filters[i]);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
