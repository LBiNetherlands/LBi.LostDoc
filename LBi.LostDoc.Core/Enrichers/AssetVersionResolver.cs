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
using System.Linq;
using System.Reflection;

namespace LBi.LostDoc.Core.Enrichers
{
    internal class AssetVersionResolver
    {
        private IProcessingContext _context;

        public AssetVersionResolver(IProcessingContext context)
        {
            this._context = context;
        }

        public string getVersionedId(string assetId)
        {
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            if (aid.Type == AssetType.Assembly)
            {
                Assembly asm = (Assembly)this._context.AssetResolver.Resolve(aid);
                aid = AssetIdentifier.FromAssembly(asm);
            }
            else if (aid.Type == AssetType.Namespace)
            {
                string ns = aid.AssetId.Substring(aid.TypeMarker.Length + 1);
                Version[] groups =
                    this._context.AssetResolver.Context.SelectMany(a => a.GetTypes())
                        .Where(t => ns.Equals(t.Namespace, StringComparison.Ordinal))
                        .Select(t => t.Assembly)
                        .Distinct()
                        .GroupBy(a => a.GetName().Version, (v, g) => v).ToArray();

                if (groups.Length > 1)
                    throw new AmbiguousMatchException();

                aid = AssetIdentifier.FromNamespace(ns, groups[0]);
            }
            else
            {
                object obj = this._context.AssetResolver.Resolve(aid);
                aid = AssetIdentifier.FromMemberInfo((MemberInfo)obj);
            }

            this._context.AddReference(aid);

            return aid.ToString();
        }
    }
}
