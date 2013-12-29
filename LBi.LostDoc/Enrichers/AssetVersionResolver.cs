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

using System;
using System.Collections.Generic;
using System.Linq;

namespace LBi.LostDoc.Enrichers
{
    internal class AssetVersionResolver
    {
        private readonly Asset _assembly;
        private readonly Dictionary<string, Asset[]>[] _accessibleAssets;
        private IProcessingContext _context;

        public AssetVersionResolver(IProcessingContext context, Asset assembly)
        {
            this._context = context;
            this._assembly = assembly;
            List<List<Asset>> phases = new List<List<Asset>>();

            HashSet<Asset> processedAssemblies = new HashSet<Asset>();

            FindAccessibleAssets(processedAssemblies, phases, context.AssetExplorer, assembly, 0);


            this._accessibleAssets = phases.Select(phase =>
                                                   phase.GroupBy(ai => ai.Id.AssetId, StringComparer.Ordinal)
                                                        .ToDictionary(g => g.Key,
                                                                      g => g.ToArray(),
                                                                      StringComparer.Ordinal))
                                           .ToArray();
        }

        private static void FindAccessibleAssets(HashSet<Asset> processedAssemblies, List<List<Asset>> assets, IAssetExplorer assetExplorer, Asset assembly, int depth)
        {
            List<Asset> currentPhase;

            if (assets.Count <= depth)
                assets.Add(currentPhase = new List<Asset>());
            else
                currentPhase = assets[depth];

            currentPhase.AddRange(assetExplorer.Discover(assembly));

            foreach (var reference in assetExplorer.GetReferences(assembly, null))
            {
                if (processedAssemblies.Add(reference))
                {
                    FindAccessibleAssets(processedAssemblies, assets, assetExplorer, reference, depth + 1);
                }
            }
        }


        public string getVersionedId(string assetId)
        {
            Asset asset = null;
            for (int depth = 0; depth < this._accessibleAssets.Length; depth++)
            {
                Asset[] matches;
                if (!this._accessibleAssets[depth].TryGetValue(assetId, out matches))
                    continue;

                if (matches.Length > 1)
                {
                    IGrouping<Version, Asset>[] groups = matches.GroupBy(ai => ai.Id.Version).ToArray();
                    if (groups.Length > 1)
                    {
                        // TODO output WARNING/INFO
                    }
                    asset = groups.OrderByDescending(g => g.Count()).First().First();
                }
                if (matches.Length > 0)
                    asset = matches[0];
            }

            if (asset != null)
            {
                this._context.AddReference(asset);
                assetId = asset.Id.ToString();
            }

            return assetId;
        }
    }
}
