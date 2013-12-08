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

namespace LBi.LostDoc
{
    public interface IAssetExplorer
    {
        /// <summary>
        /// Returns the assemblies referenced by <paramref name="assemblyAsset"/>.
        /// </summary>
        /// <param name="assemblyAsset">Must be of type <see cref="AssetType.Assembly"/>.</param>
        /// <param name="filters"><see cref="IFilterContext"/> used to determine which assets to return. Can be <value>null</value>.</param>
        /// <returns>The referenced assemblies as <see cref="Asset"/>.</returns>
        IEnumerable<Asset> GetReferences(Asset assemblyAsset, IFilterContext filters);

        IEnumerable<Asset> Discover(Asset root, IFilterContext filters);
    }
}