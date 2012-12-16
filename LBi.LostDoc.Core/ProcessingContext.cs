/*
 * Copyright 2012 LBi Netherlands B.V.
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
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using LBi.LostDoc.Core.Diagnostics;

namespace LBi.LostDoc.Core
{
    public class ProcessingContext : IProcessingContext
    {
        private readonly IAssetFilter[] _filters;
        private readonly HashSet<AssetIdentifier> _references;

        public ProcessingContext(IEnumerable<IAssetFilter> filters, IAssetResolver assetResolver, XElement element,
                                 HashSet<AssetIdentifier> references, int phase)
        {
            this._filters = filters.ToArray();
            this.AssetResolver = assetResolver;
            this.Element = element;
            this._references = references;
            this.Phase = phase;
        }

        #region IProcessingContext Members

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
            return new ProcessingContext(this._filters, this.AssetResolver, newElement, this._references, this.Phase);
        }

        public bool IsFiltered(AssetIdentifier assetId)
        {
            IFilterContext filterContext = new FilterContext(this.AssetResolver);
            for (int i = 0; i < this._filters.Length; i++)
            {
                if (this._filters[i].Filter(filterContext, assetId))
                    return true;
            }

            return false;
        }

        #endregion
    }
}
