/*
 * Copyright 2013-2014 DigitasLBi Netherlands B.V.
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
using System.Security.Cryptography;

namespace LBi.LostDoc.Reflection
{
    public class ReflectionExplorer : IAssetExplorer
    {
        private readonly IAssemblyLoader _assemblyLoader;

        public ReflectionExplorer(IAssemblyLoader assemblyLoader)
        {
            this._assemblyLoader = assemblyLoader;
        }

        public IEnumerable<Asset> GetReferences(Asset asset)
        {

            //switch (asset.Type)
            //{
            //    case AssetType.Namespace:
            //        // nothing
            //        yield break;

            //    case AssetType.Type:
            //        // base class & implemented interface types
            //        Type type = (Type)asset.Target;
            //        yield return ReflectionServices.GetAsset(type.BaseType);
            //        foreach (var interfaceType in type.GetImplementedInterfaces())
            //            yield return ReflectionServices.GetAsset(interfaceType);

            //        if (type.IsGenericType)
            //        {
            //            foreach (Type typeArg in type.GetGenericArguments())
            //            {
            //                if (typeArg.IsGenericParameter)
            //                    continue;

            //                yield return 
            //            }
            //        } else if (type.IsG)
                        

            //        break;
            //    case AssetType.Method:
            //        // parameter & return types
            //        MethodInfo method = (MethodInfo)asset.Target;
            //        yield return ReflectionServices.GetAsset(method.ReturnType);

            //        break;
            //    case AssetType.Field:
            //        // field type
            //        break;
            //    case AssetType.Event:
            //        // parameter & return types
            //        break;
            //    case AssetType.Property:
            //        // property type, indexer types
            //        break;
            //    case AssetType.Assembly:
            //        // references
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}

            Assembly assembly = asset.GetAssembly();

            AssemblyName[] references = assembly.GetReferencedAssemblies();

            foreach (AssemblyName reference in references)
            {
                Assembly asm = this._assemblyLoader.Load(reference.ToString());

                if (asm == null)
                    throw new FileNotFoundException("Assembly not found: " + reference.ToString());

                Asset rasset = ReflectionServices.GetAsset(asm);

                yield return rasset;
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
                    MemberInfo[] allMembers = type.GetMembers(BindingFlags.Public |
                                                              BindingFlags.NonPublic |
                                                              BindingFlags.Static |
                                                              BindingFlags.Instance);

                    ret = allMembers.Concat(type.GetNestedTypes())
                                    .Select(ReflectionServices.GetAsset);
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