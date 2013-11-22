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
        private readonly HashSet<AssetIdentifier> _references;

        public ProcessingContext(ObjectCache cache, CompositionContainer container, IEnumerable<IAssetFilter> filters, IAssemblyLoader assemblyLoader, IAssetResolver assetResolver, XElement element, HashSet<AssetIdentifier> references, int phase)
        {
            this._filters = filters.ToArray();
            this.AssetResolver = assetResolver;
            this.Element = element;
            this._references = references;
            Container = container;
            this.Phase = phase;
            this.Cache = cache;
            this.AssemblyLoader = assemblyLoader;
        }

        #region IProcessingContext Members

        public IAssemblyLoader AssemblyLoader { get; private set; }

        public ObjectCache Cache { get; private set; }

        public CompositionContainer Container { get; private set; }

        public XElement Element { get; private set; }

        public IEnumerable<AssetIdentifier> References
        {
            get { return this._references; }
        }

        public bool AddReference(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
                throw new ArgumentException("Argument cannot be null or empty.", "assetId");

            AssetIdentifier aid = AssetIdentifier.Parse(assetId);

            Debug.Assert(aid.ToString().Equals(assetId, StringComparison.Ordinal),
                         "AssetIdentifier '{0}' failed to roundtrip.", assetId);

            if (aid.AssetId[aid.AssetId.Length - 1] == ']')
                aid = new AssetIdentifier(aid.AssetId.Substring(0, aid.AssetId.LastIndexOf('[')),
                                          aid.Version);

            object resolve = this.AssetResolver.Resolve(aid);
            Debug.Assert(resolve != null);

            Debug.Assert(!string.IsNullOrWhiteSpace(aid.AssetId));

            if (this._references.Add(aid))
            {
                TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose, 1, "Reference: {0}", aid);
                return true;
            }

            return false;
        }

        public IAssetResolver AssetResolver { get; protected set; }

        public int Phase { get; private set; }

        public IProcessingContext Clone(XElement newElement)
        {
            return new ProcessingContext(this.Cache,
                                         this.Container,
                                         this._filters,
                                         this.AssemblyLoader,
                                         this.AssetResolver,
                                         newElement,
                                         this._references,
                                         this.Phase);
        }

        public bool IsFiltered(AssetIdentifier assetId)
        {
            IFilterContext filterContext = new FilterContext(this.Cache, this.Container, this.AssetResolver, FilterState.Generating);
            for (int i = 0; i < this._filters.Length; i++)
            {
                if (this._filters[i].Filter(filterContext, assetId))
                {
                    TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose,
                                        0,
                                        "{0} - Filtered by {1}",
                                        assetId.AssetId, this._filters[i]);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
