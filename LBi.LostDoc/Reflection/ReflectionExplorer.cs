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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LBi.LostDoc.Reflection
{
    public class ReflectionExplorer : IAssetExplorer
    {
        private readonly IAssemblyLoader _assemblyLoader;

        public ReflectionExplorer(IAssemblyLoader assemblyLoader)
        {
            this._assemblyLoader = assemblyLoader;
        }

        public IEnumerable<Asset> GetReferences(Asset assemblyAsset, IFilterContext filter)
        {
            if (assemblyAsset.Type != AssetType.Assembly)
                throw new ArgumentException("Asset must be of type Assembly", "assemblyAsset");

            Assembly assembly = assemblyAsset.GetAssembly();

            AssemblyName[] references = assembly.GetReferencedAssemblies();

            foreach (AssemblyName reference in references)
            {
                Assembly asm = this._assemblyLoader.Load(reference.ToString());

                if (asm == null)
                    throw new FileNotFoundException("Assembly not found: " + reference.ToString());

                Asset asset = ReflectionServices.GetAsset(asm);

                if (filter == null || !filter.IsFiltered(asset))
                    yield return asset;
            }
        }

        public Asset GetParent(Asset asset)
        {
            Asset ret;
            switch (asset.Type)
            {
                case AssetType.Namespace:
                    NamespaceInfo nsInfo = (NamespaceInfo)asset.Target;
                    ret = ReflectionServices.GetAsset(nsInfo.Assembly);
                    break;
                case AssetType.Type:
                    Type type = (Type)asset.Target;
                    if (type.IsNested)
                        ret = ReflectionServices.GetAsset(type.DeclaringType);
                    else
                        ret = ReflectionServices.GetAsset(type.Assembly, type.Namespace);
                    break;
                case AssetType.Method:
                case AssetType.Field:
                case AssetType.Event:
                case AssetType.Property:
                    Type parent = ((MemberInfo)asset.Target).ReflectedType;
                    ret = ReflectionServices.GetAsset(parent);
                    break;
                case AssetType.Assembly:
                    ret = null;
                    break;
                default:
                    throw new ArgumentException(string.Format("Cannot find parent of asset of type {0}", asset.Type));
            }

            return ret;
        }

        public IEnumerable<Asset> GetChildren(Asset asset)
        {
            IEnumerable<Asset> ret;
            switch (asset.Type)
            {
                case AssetType.Namespace:
                    NamespaceInfo nsInfo = (NamespaceInfo)asset.Target;
                    ret = nsInfo.Assembly.GetTypes()
                                .Where(t => t.Namespace == nsInfo.Name)
                                .Select(ReflectionServices.GetAsset);
                    break;
                case AssetType.Type:
                    Type type = (Type)asset.Target;
                    ret = type.GetMembers().Concat(type.GetNestedTypes()).Select(ReflectionServices.GetAsset);
                    break;
                
                case AssetType.Method:
                case AssetType.Field:
                case AssetType.Event:
                case AssetType.Property:
                    ret = Enumerable.Empty<Asset>();
                    break;
                case AssetType.Assembly:
                    Assembly asm = (Assembly)asset.Target;
                    string[] namespaces = asm.GetTypes()
                                             .Select(t => t.Namespace ?? string.Empty)
                                             .Distinct(StringComparer.Ordinal)
                                             .ToArray();

                    HashSet<string> uniqueNamespaces = new HashSet<string>(namespaces, StringComparer.Ordinal);

                    foreach (var ns in namespaces)
                    {
                        string[] parts = ns.Split('.');
                        if (parts.Length > 1)
                        {
                            for (int i = 1; i <= parts.Length; i++)
                                uniqueNamespaces.Add(string.Join(".", parts, 0, i));
                        }
                    }

                    ret = uniqueNamespaces.Select(ns => ReflectionServices.GetAsset(asm, ns));
                    break;
                default:
                    throw new ArgumentException(string.Format("Cannot find children of asset of type {0}", asset.Type));
            }

            return ret;
        }       
    }
}