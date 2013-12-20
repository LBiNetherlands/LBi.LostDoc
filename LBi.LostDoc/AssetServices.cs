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
            ExceptionDispatchInfo exception = null;

            ConcurrentQueue<Asset> queue = new ConcurrentQueue<Asset>();
            using (BlockingCollection<Asset> blocking = new BlockingCollection<Asset>(queue))
            {
                Action<object> func =
                    a =>
                    {
                        try
                        {
                            Discover(assetExplorer, (Asset)a, blocking, filter);
                        }
                        catch (Exception ex)
                        {
                            exception = ExceptionDispatchInfo.Capture(ex);
                        }
                        blocking.CompleteAdding();
                    };


                using (Task task = Task.Factory.StartNew(func, root))
                {
                    foreach (var asset in blocking.GetConsumingEnumerable())
                        yield return asset;

                    task.Wait();

                    if (exception != null)
                        exception.Throw();
                }
            }
        }

        private static void Discover(IAssetExplorer assetExplorer, Asset asset, BlockingCollection<Asset> collection, IFilterContext filter)
        {
            if (filter != null && filter.IsFiltered(asset))
                return;

            collection.Add(asset);

            IEnumerable<Asset> childAssets = assetExplorer.GetChildren(asset);

            foreach (Asset childAsset in childAssets)
                Discover(assetExplorer, childAsset, collection, filter);
        }
    }
}