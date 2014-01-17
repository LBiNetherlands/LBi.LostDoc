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
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Security.Cryptography;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Reflection;

namespace LBi.LostDoc.Enrichers
{
    internal class AssetVersionResolver
    {
        private readonly Asset _asset;
        private readonly IProcessingContext _context;

        public AssetVersionResolver(IProcessingContext context, Asset asset)
        {
            this._context = context;
            this._asset = asset;
        }

        protected IEnumerable<Asset> FindAllReferences(Type type)
        {
            if (type.IsGenericParameter)
            {
                foreach (Type constraintType in type.GetGenericParameterConstraints())
                {
                    foreach (Asset asset in this.FindAllReferences(constraintType))
                        yield return asset;
                }
            }
            else
            {
                yield return ReflectionServices.GetAsset(type);

                foreach (Type interfaceType in type.GetImplementedInterfaces())
                {
                    foreach (Asset asset in this.FindAllReferences(interfaceType))
                        yield return asset;
                }

                if (type.BaseType != null)
                {
                    foreach (Asset asset in this.FindAllReferences(type.BaseType))
                        yield return asset;
                }

                if (type.IsGenericType)
                {
                    foreach (Type typeArg in type.GetGenericArguments())
                    {
                        foreach (Asset asset in this.FindAllReferences(typeArg))
                            yield return asset;
                    }
                }
            }
        }

        protected IEnumerable<Asset> FindAllReferences(MethodInfo method)
        {
            yield return ReflectionServices.GetAsset(method);

            foreach (var asset in this.FindAllReferences(method.ReturnType))
                yield return asset;

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                foreach (var asset in this.FindAllReferences(parameter.ParameterType))
                    yield return asset;
            }

            if (method.IsGenericMethod)
            {
                foreach (Type typeArg in method.GetGenericArguments())
                {
                    foreach (Asset asset in this.FindAllReferences(typeArg))
                        yield return asset;
                }
            }
        }

        protected IEnumerable<Asset> FindAllReferences(ConstructorInfo ctorInfo)
        {
            yield return ReflectionServices.GetAsset(ctorInfo);

            foreach (ParameterInfo parameter in ctorInfo.GetParameters())
            {
                foreach (var asset in this.FindAllReferences(parameter.ParameterType))
                    yield return asset;
            }

            if (ctorInfo.IsGenericMethod)
            {
                foreach (Type typeArg in ctorInfo.GetGenericArguments())
                {
                    foreach (Asset asset in this.FindAllReferences(typeArg))
                        yield return asset;
                }
            }
        }

        protected IEnumerable<Asset> FindAllReferences(FieldInfo fieldInfo)
        {
            yield return ReflectionServices.GetAsset(fieldInfo);

            foreach (var asset in this.FindAllReferences(fieldInfo.FieldType))
                yield return asset;
        }

        protected IEnumerable<Asset> FindAllReferences(EventInfo eventInfo)
        {
            yield return ReflectionServices.GetAsset(eventInfo);

            foreach (var asset in this.FindAllReferences(eventInfo.GetAddMethod(true)))
                yield return asset;

            foreach (var asset in this.FindAllReferences(eventInfo.GetRemoveMethod(true)))
                yield return asset;
        }

        protected IEnumerable<Asset> FindAllReferences(PropertyInfo propertyInfo)
        {
            yield return ReflectionServices.GetAsset(propertyInfo);

            foreach (var asset in this.FindAllReferences(propertyInfo.PropertyType))
                yield return asset;

            foreach (var parameter in propertyInfo.GetIndexParameters())
            {
                foreach (Asset asset in this.FindAllReferences(parameter.ParameterType))
                    yield return asset;
            }
        }

        protected IEnumerable<Asset> FindAllReferences(Asset asset)
        {
            switch (asset.Type)
            {
                case AssetType.Namespace:
                    // nothing
                    yield break;

                case AssetType.Type:
                    // base class & implemented interface types
                    Type type = (Type)asset.Target;

                    foreach (Asset ret in this.FindAllReferences(type))
                        yield return ret;

                    break;
                case AssetType.Method:
                    // parameter & return types
                    MethodInfo method = asset.Target as MethodInfo;

                    if (method != null)
                    {
                        foreach (Asset ret in this.FindAllReferences(method))
                            yield return ret;
                    }
                    else
                    {
                        ConstructorInfo ctor = (ConstructorInfo)asset.Target;
                        foreach (Asset ret in this.FindAllReferences(ctor))
                            yield return ret;
                    }


                    break;
                case AssetType.Field:
                    // field type
                    FieldInfo field = (FieldInfo)asset.Target;

                    foreach (Asset ret in this.FindAllReferences(field))
                        yield return ret;

                    break;
                case AssetType.Event:
                    // parameter & return types
                    EventInfo eventInfo = (EventInfo)asset.Target;

                    foreach (Asset ret in this.FindAllReferences(eventInfo))
                        yield return ret;

                    break;
                case AssetType.Property:
                    // property type, indexer types
                    PropertyInfo property = (PropertyInfo)asset.Target;

                    foreach (Asset ret in this.FindAllReferences(property))
                        yield return ret;

                    break;
                case AssetType.Assembly:
                    // references
                    foreach (var ret in this._context.AssetExplorer.GetReferences(asset))
                        yield return ret;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected IEnumerable<Asset> FindAllAssemblies(Asset assemblyAsset)
        {
            HashSet<AssetIdentifier> seenAssemblies = new HashSet<AssetIdentifier>();

            yield return assemblyAsset;

            foreach (Asset asset in this.FindAllAssemblies(assemblyAsset, seenAssemblies))
                yield return asset;
        }

        private IEnumerable<Asset> FindAllAssemblies(Asset assemblyAsset, HashSet<AssetIdentifier> seenAssemblies)
        {
            if (!seenAssemblies.Add(assemblyAsset.Id))
                yield break;

            Asset[] references = this._context.AssetExplorer.GetReferences(assemblyAsset).ToArray();

            foreach (Asset reference in references)
                yield return reference;

            foreach (var reference in references)
            {
                foreach (var childReference in this.FindAllAssemblies(reference))
                    yield return childReference;
            }
        }

        public string getVersionedId(string assetId)
        {
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            AssetIdentifier ret = null;

            if (assetId == this._asset.Id.AssetId)
                ret = this._asset.Id;
            else if (aid.Type == AssetType.Assembly)
            {
                Asset originAssembly = this._context.AssetExplorer.GetRoot(this._asset);
                if (assetId == originAssembly.Id.AssetId)
                    ret = originAssembly.Id;
                else
                {
                    foreach (Asset referencedAssembly in this.FindAllAssemblies(originAssembly))
                    {
                        if (assetId == referencedAssembly.Id.AssetId)
                        {
                            ret = referencedAssembly.Id;
                            break;
                        }
                    }
                }
            }
            else if (aid.Type == AssetType.Namespace)
            {
                Asset assemblyAsset = this._context.AssetExplorer.GetRoot(this._asset);
                foreach (Asset referencedAssembly in this.FindAllAssemblies(assemblyAsset))
                {
                    var namespaceAssets = this._context.AssetExplorer.GetChildren(referencedAssembly).Where(a => a.Type == AssetType.Namespace);

                    foreach (var namespaceAsset in namespaceAssets)
                    {
                        if (assetId == namespaceAsset.Id.AssetId)
                        {
                            ret = namespaceAsset.Id;
                            break;
                        }
                    }
                }
            }
            else if (aid.Type != AssetType.Unknown)
            {
                foreach (Asset asset in this.FindAllReferences(this._asset))
                {
                    if (asset.Id.AssetId == assetId)
                    {
                        ret = asset.Id;
                        break;
                    }
                }
                if (ret == null)
                {
                    Asset assemblyAsset = this._context.AssetExplorer.GetRoot(this._asset);
                    foreach (Asset referencedAssembly in this.FindAllAssemblies(assemblyAsset))
                    {
                        IEnumerable<Asset> allAssets = this._context.AssetExplorer.Discover(referencedAssembly);
                        foreach (var asset in allAssets)
                        {
                            if (asset.Id.AssetId == assetId)
                            {
                                ret = asset.Id;
                                break;
                            }
                        }

                        if (ret != null)
                            break;
                    }
                }
            }
            //foreach (Asset asset in this.FindAllReferences(this._asset))
            //{
            //    if (asset.Id.AssetId == assetId)
            //        return asset.ToString();
            //}

            //this._context.AssetExplorer.GetDescendants()

            //this._context.AssetExplorer.GetSiblings()

            //this._context.AssetExplorer.GetAssetHierarchy()

            //this._context.AssetExplorer.GetReferences(this._asset);

            //Asset asset = null;
            //for (int depth = 0; depth < this._accessibleAssets.Length; depth++)
            //{
            //    Asset[] matches;
            //    if (!this._accessibleAssets[depth].TryGetValue(assetId, out matches))
            //        continue;

            //    if (matches.Length > 1)
            //    {
            //        IGrouping<Version, Asset>[] groups = matches.GroupBy(ai => ai.Id.Version).ToArray();
            //        asset = groups.OrderByDescending(g => g.Count()).First().First();

            //        if (groups.Length > 1)
            //        {
            //            TraceSources.AssetResolverSource.TraceWarning("Asset {0} found in with several versions ({1}) using {2} from {3}",
            //                                                          assetId,
            //                                                          string.Join(", ", groups.Select(g => g.Key)),
            //                                                          asset.Id.Version,
            //                                                          this._context.AssetExplorer.GetRoot(asset).Id.ToString());
            //        }

            //    }
            //    if (matches.Length > 0)
            //        asset = matches[0];
            //}

            //if (asset != null)
            //{
            //    TraceSources.AssetResolverSource.TraceVerbose("Resolved {0} to version: {1}", assetId, asset.Id.Version);
            //    this._context.AddReference(asset);
            //    assetId = asset.Id.ToString();
            //}

            if (ret != null)
                return ret.ToString();

            return assetId;
        }
    }
}
