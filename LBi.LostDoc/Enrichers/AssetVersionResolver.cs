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
using System.Diagnostics;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Enrichers
{
    internal class AssetVersionResolver
    {
        private class AssetPrefixFilter : IAssetFilter
        {
            private readonly string _assetPrefix;

            public AssetPrefixFilter(string asset)
            {
                this._assetPrefix = asset.Substring(asset.IndexOf(':') + 1);
            }

            public bool Filter(IFilterContext context, Asset asset)
            {
                if (asset.Type == AssetType.Type)
                {
                    bool filtered = !_assetPrefix.StartsWith(asset.Id.AssetId.Substring(asset.Id.AssetId.IndexOf(':') + 1));
                    return filtered;
                }
                else if (asset.Type == AssetType.Namespace)
                {
                    
                }

                return false;
            }
        }
        private readonly Asset _assembly;
        private readonly Dictionary<string, Asset[]>[] _accessibleAssets;
        private readonly IProcessingContext _context;

        public AssetVersionResolver(IProcessingContext context, Asset assembly)
        {
            this._context = context;
            this._assembly = assembly;
        }

        public string getVersionedId(string assetId)
        {
            Asset asset = null;

            FilterContext filterContext = new FilterContext(this._context.Cache, null, FilterState.Discovery, new AssetPrefixFilter(assetId));

            foreach (Asset a in this._context.AssetExplorer.Discover(this._assembly, filterContext))
            {
                if (StringComparer.Ordinal.Equals(a.Id.AssetId, assetId))
                {
                    asset = a;
                    break;
                }
            }

            if (asset == null)
            {
                foreach (var asm in this._context.AssetExplorer.GetReferences(this._assembly))
                {
                    Debug.WriteLine(asm.ToString());

                    foreach (Asset a in this._context.AssetExplorer.Discover(asm, filterContext))
                    {
                        if (StringComparer.Ordinal.Equals(a.Id.AssetId, assetId))
                        {
                            asset = a;
                            break;
                        }
                    }
                }
            }

            if (asset != null)
            {
                TraceSources.AssetResolverSource.TraceVerbose("Resolved {0} to version: {1}", assetId, asset.Id.Version);
                this._context.AddReference(asset);
                assetId = asset.Id.ToString();
            }
            else
            {
                TraceSources.AssetResolverSource.TraceWarning("Failed to resolve {0}", assetId);
            }

            return assetId;
        }
    }
}
