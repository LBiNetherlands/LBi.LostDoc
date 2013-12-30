/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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
using System.Linq;
using System.Runtime.CompilerServices;

namespace LBi.LostDoc
{
    public class AssetExplorerCache : IAssetExplorer
    {
        private readonly IAssetExplorer _assetExplorer;
        private readonly Dictionary<Asset, Asset[]> _referenceCache;
        private readonly Dictionary<Asset, Asset> _parentCache;
        private readonly Dictionary<Asset, Asset[]> _childrenCache;

        public AssetExplorerCache(IAssetExplorer assetExplorer)
        {
            this._assetExplorer = assetExplorer;
            this._referenceCache = new Dictionary<Asset, Asset[]>();
            this._parentCache = new Dictionary<Asset, Asset>();
            this._childrenCache = new Dictionary<Asset, Asset[]>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Asset> GetReferences(Asset assemblyAsset)
        {
            Asset[] cachedValue;
            if (!this._referenceCache.TryGetValue(assemblyAsset, out cachedValue))
            {
                cachedValue = this._assetExplorer.GetReferences(assemblyAsset).ToArray();
                this._referenceCache.Add(assemblyAsset, cachedValue);
            }
            return cachedValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Asset GetParent(Asset asset)
        {
            Asset cachedValue;
            if (!this._parentCache.TryGetValue(asset, out cachedValue))
            {
                cachedValue = this._assetExplorer.GetParent(asset);
                this._parentCache.Add(asset, cachedValue);
            }
            return cachedValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Asset> GetChildren(Asset asset)
        {
            Asset[] cachedValue;
            if (!this._childrenCache.TryGetValue(asset, out cachedValue))
            {
                cachedValue = this._assetExplorer.GetChildren(asset).ToArray();
                this._childrenCache.Add(asset, cachedValue);
            }
            return cachedValue;
        }
    }
}