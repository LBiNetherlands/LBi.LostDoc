/*
 * Copyright 2012-2013 DigitasLBi Netherlands B.V.
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

using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Runtime.Caching;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc
{
    public class FilterContext : IFilterContext
    {
        private IAssetFilter[] _filters;

        public FilterContext(ObjectCache cache, CompositionContainer container, FilterState state, params IAssetFilter[] filters)
        {
            this.Container = container;
            this.Cache = cache;
            this.State = state;
            this._filters = filters;
        }

        #region IFilterContext Members

        public FilterState State { get; private set; }
        
        public bool IsFiltered(Asset asset)
        {
            bool filtered = false;
            foreach (IAssetFilter filter in this._filters)
            {
                if (filter.Filter(this, asset))
                {
                    filtered = true;
                    TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose,
                                                            0,
                                                            "{0} - Filtered by {1}",
                                                            asset.Id.AssetId,
                                                            filter);

                    break;
                }
            }
            return filtered;
        }

        #endregion

        public ObjectCache Cache { get; private set; }

        public CompositionContainer Container { get; private set; }
    }
}
