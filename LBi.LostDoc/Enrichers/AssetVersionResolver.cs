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
using System.Linq;
using System.Reflection;

namespace LBi.LostDoc.Enrichers
{
    internal class AssetVersionResolver
    {
        private readonly IProcessingContext _context;
        private readonly Assembly _hintAssembly;

        public AssetVersionResolver(IProcessingContext context, Assembly hintAssembly)
        {
            this._context = context;
            this._hintAssembly = hintAssembly;
        }

        public string getVersionedId(string assetId)
        {
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            AssetIdentifier hintAsmAid = null;
            AssetIdentifier ret = null;

            if (this._hintAssembly != null)
                hintAsmAid = AssetIdentifier.FromAssembly(this._hintAssembly);

            if (aid.Type == AssetType.Assembly)
            {
                Assembly asm = (Assembly)this._context.AssetResolver.Resolve(aid, hintAsmAid);
                if (asm != null)
                    ret = AssetIdentifier.FromAssembly(asm);
            }
            else if (aid.Type == AssetType.Namespace)
            {
                string ns = aid.AssetId.Substring(aid.TypeMarker.Length + 1);

                var version = this.GetNamespaceVersion(this._hintAssembly, ns);

                if (version != null)
                    ret = AssetIdentifier.FromNamespace(ns, version);
            }
            else
            {
                object obj = this._context.AssetResolver.Resolve(aid, hintAsmAid);

                if (obj != null)
                {
                    if (aid.Type == AssetType.Unknown)
                    {
                        MethodInfo[] arr = obj as MethodInfo[];
                        if (arr != null)
                        {
                            // TODO this isn't very nice but it should do the trick for now
                            var dummyAid = AssetIdentifier.FromMemberInfo(arr[0]);
                            ret = new AssetIdentifier(aid.AssetId, dummyAid.Version);
                        }
                        else
                            throw new NotSupportedException("Unknow AssetIdentifier marker: " + aid.TypeMarker);
                    }
                    else
                        ret = AssetIdentifier.FromMemberInfo((MemberInfo)obj);
                }
            }

            if (ret != null)
                this._context.AddReference(ret);
            else
            {
                // TODO log warning/error failed to resolve asset
                ret = aid;
            }
            return ret.ToString();
        }

        private Version GetNamespaceVersion(Assembly assembly, string ns)
        {
            foreach (Assembly asm in this._context.AssemblyLoader.GetAssemblyChain(assembly))
            {
                var types = asm.GetTypes();

                for (int typeNum = 0; typeNum < types.Length; typeNum++)
                {
                    if (ns.Equals(types[typeNum].Namespace) ||
                        (types[typeNum].Namespace != null &&
                         types[typeNum].Namespace.StartsWith(ns) &&
                         types[typeNum].Namespace[ns.Length] == '.'))
                    {
                        return assembly.GetName().Version;
                    }
                }
            }

            throw new Exception("Could not find namespace '" + ns + "' in any loaded assembly");
        }
    }
}
