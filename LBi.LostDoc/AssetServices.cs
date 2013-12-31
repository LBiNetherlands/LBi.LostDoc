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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace LBi.LostDoc
{
    public static class AssetServices
    {
        public static Asset GetRoot(this IAssetExplorer assetExplorer, Asset asset)
        {
            for (;;)
            {
                Asset parent = assetExplorer.GetParent(asset);
                if (parent == null)
                    return asset;

                asset = parent;
            }
        }

        public static IEnumerable<Asset> GetAssetHierarchy(this IAssetExplorer assetExplorer, Asset asset)
        {
            do
            {
                yield return asset;
                asset = assetExplorer.GetParent(asset);
            } while (asset != null);
        }

        public static IEnumerable<Asset> Discover(this IAssetExplorer assetExplorer, Asset root)
        {
            return assetExplorer.Discover(root, null);
        }

        public static IEnumerable<Asset> Discover(this IAssetExplorer assetExplorer, Asset root, IFilterContext filter)
        {
            List<Asset> ret = new List<Asset>();
            Discover(assetExplorer, root, ret, filter);
            return ret;
        }

        private static void Discover(IAssetExplorer assetExplorer, Asset asset, List<Asset> ret, IFilterContext filter)
        {
            if (filter != null && filter.IsFiltered(asset))
                return;

            ret.Add(asset);

            IEnumerable<Asset> childAssets = assetExplorer.GetChildren(asset);

            foreach (Asset childAsset in childAssets)
                Discover(assetExplorer, childAsset, ret, filter);
        }
    }
}