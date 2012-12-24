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
        private readonly IProcessingContext _context;
        private readonly Assembly _assemblyHint;

        public AssetVersionResolver(IProcessingContext context, Assembly assemblyHint)
        {
            this._context = context;
            this._assemblyHint = assemblyHint;
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

                var version = this.GetNamespaceVersion(this._assemblyHint, ns);

                if (version == null)
                    throw new Exception("Version not found for asset: " + assetId);

                aid = AssetIdentifier.FromNamespace(ns, version);
            }
            else
            {
                object obj = this._context.AssetResolver.Resolve(aid);
                if (aid.Type == AssetType.Unknown)
                {
                    MethodInfo[] arr = obj as MethodInfo[];
                    arr.
                    if (arr != null)
                    {
                        // TODO this isn't very nice but it should do the trick for now
                        // FIX this guy needs more context in order to correctly resolve an asset without version
                        // maybe the originating assembly

                        var dummyAid = AssetIdentifier.FromMemberInfo(arr[0]);
                        aid = new AssetIdentifier(aid.AssetId, dummyAid.Version);
                    }
                    else
                        throw new NotSupportedException("Unknow AssetIdentifier marker: " + aid.TypeMarker);
                }
                else
                    aid = AssetIdentifier.FromMemberInfo((MemberInfo)obj);
            }

            this._context.AddReference(aid);

            return aid.ToString();
        }

        private Version GetNamespaceVersion(Assembly assembly, string ns)
        {
            // FIX this could throw a TypeLoadException
            var types = assembly.GetTypes();
            Version version = null;
            for (int typeNum = 0; typeNum < types.Length; typeNum++)
            {
                if (ns.Equals(types[typeNum].Namespace) ||
                    (types[typeNum].Namespace != null &&
                     types[typeNum].Namespace.StartsWith(ns) &&
                     types[typeNum].Namespace[ns.Length] == '.'))
                {
                    version = this._assemblyHint.GetName().Version;
                    break;
                }
            }

            if (version == null)
            {
                AssemblyName[] referencedAssemblies = this._assemblyHint.GetReferencedAssemblies();

                for (int refNum = 0; refNum < referencedAssemblies.Length; refNum++)
                {
                    Assembly asm = this._context.AssetResolver.Context.Single(a => a.GetName() == referencedAssemblies[refNum]);
                    version = this.GetNamespaceVersion(asm, ns);
                    if (version != null)
                        break;
                }
            }
            return version;
        }
    }
}
