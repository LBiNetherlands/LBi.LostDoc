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
using System.Reflection;
using System.Text.RegularExpressions;
using LBi.LostDoc.Templating;

namespace LBi.LostDoc.Enrichers
{
    internal class AssetVersionResolver
    {
        private readonly IAssetExplorer _assetExplorer;
        private readonly Asset _assembly;
        private readonly AssetIdentifier[][] _accessibleAssets;

        public AssetVersionResolver(IAssetExplorer assetExplorer, Asset assembly)
        {
            this._assetExplorer = assetExplorer;
            this._assembly = assembly;
            List<List<AssetIdentifier>> phases = new List<List<AssetIdentifier>>();

            FindAccessibleAssets(phases, assetExplorer, assembly, 0);

            this._accessibleAssets = phases.Select(l => l.ToArray()).ToArray();
        }

        private static void FindAccessibleAssets(List<List<AssetIdentifier>> assets, IAssetExplorer assetExplorer, Asset assembly, int depth)
        {
            List<AssetIdentifier> currentPhase;

            if (assets.Count <= depth)
                assets.Add(currentPhase = new List<AssetIdentifier>());
            else
                currentPhase = assets[depth];

            currentPhase.AddRange(assetExplorer.Discover(assembly).Select(a => a.Id));

            foreach (var reference in assetExplorer.GetReferences(assembly, null))
                FindAccessibleAssets(assets, assetExplorer, reference, depth + 1);
        }


        public string getVersionedId(string assetId)
        {
            for (int depth = 0; depth < this._accessibleAssets.Length; depth++)
            {
                var matches = this._accessibleAssets[depth].Where(a => a.AssetId.Equals(assetId, StringComparison.Ordinal)).ToArray();

                if (matches.Length > 1)
                {
                    IGrouping<Version, AssetIdentifier>[] groups = matches.GroupBy(ai => ai.Version).ToArray();
                    if (groups.Length > 1)
                    {
                        // TODO output WARNING/INFO
                    }
                    return groups.OrderByDescending(g => g.Count()).First().First();
                }
                if (matches.Length > 0)
                    return matches[0].ToString();
            }

            return assetId;
        }
    }
}
