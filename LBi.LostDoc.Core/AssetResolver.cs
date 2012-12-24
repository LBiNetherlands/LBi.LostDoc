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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LBi.LostDoc.Core.Diagnostics;

namespace LBi.LostDoc.Core
{
    public class AssetResolver : IAssetResolver
    {
        private Assembly[] _assemblies;
        private Dictionary<AssetIdentifier, object> _cache;

        public AssetResolver(IEnumerable<Assembly> sourceAssemblies)
        {
            this._assemblies = sourceAssemblies.ToArray();
            this._cache = new Dictionary<AssetIdentifier, object>();
        }

        #region IAssetResolver Members

        public object Resolve(AssetIdentifier assetIdentifier)
        {
            object ret;
            if (!this._cache.TryGetValue(assetIdentifier, out ret))
                this._cache.Add(assetIdentifier, ret = this.ResolveInternal(assetIdentifier));

            return ret;
        }

        public object Resolve(AssetIdentifier assetIdentifier, AssetIdentifier hintAssembly)
        {
            object ret;
            if (!this._cache.TryGetValue(assetIdentifier, out ret))
                this._cache.Add(assetIdentifier, ret = this.ResolveInternal(assetIdentifier, (Assembly)this.ResolveInternal(hintAssembly)));

            return ret;
        }

        public IEnumerable<AssetIdentifier> GetAssetHierarchy(AssetIdentifier assetId)
        {
            yield return assetId;

            switch (assetId.Type)
            {
                case AssetType.Namespace:
                    string ns = (string)this.Resolve(assetId);
                    Assembly[] matchingAssemblies =
                        this._assemblies.Where(a => a.GetName().Version == assetId.Version)
                            .Where(a => a.GetTypes().Any(
                                t1 =>
                                t1.Namespace != null &&
                                (StringComparer.Ordinal.Equals(t1.Namespace, ns) ||
                                t1.Namespace.StartsWith(ns + ".", StringComparison.Ordinal))))
                            .ToArray();

                    if (matchingAssemblies.Length == 0)
                        throw new InvalidOperationException("Found no assembly containing namespace: " + ns);

                    if (matchingAssemblies.Length > 1)
                    {
                        TraceSources.AssetResolverSource.TraceWarning(
                            "Namespace {0} found in more than one assembly: {1}",
                            ns,
                            string.Join(", ", matchingAssemblies.Select(a => a.GetName().Name)));
                    }
                    yield return AssetIdentifier.FromAssembly(matchingAssemblies[0]);
                    break;
                case AssetType.Type:

                    Type t = (Type)this.Resolve(assetId);
                    while (t.IsNested)
                    {
                        t = t.DeclaringType;
                        yield return AssetIdentifier.FromMemberInfo(t);
                    }

                    yield return AssetIdentifier.FromNamespace(t.Namespace, t.Assembly.GetName().Version);
                    yield return AssetIdentifier.FromAssembly(t.Assembly);

                    break;
                case AssetType.Method:
                case AssetType.Field:
                case AssetType.Event:
                case AssetType.Property:
                    object resolve = this.Resolve(assetId);
                    MemberInfo mi = (MemberInfo)resolve;

                    foreach (
                        AssetIdentifier aid in
                            this.GetAssetHierarchy(AssetIdentifier.FromMemberInfo(mi.ReflectedType)))
                        yield return aid;


                    break;
                case AssetType.Assembly:
                    yield break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IEnumerable<Assembly> Context
        {
            get { return this._assemblies; }
        }

        #endregion

        private object ResolveInternal(AssetIdentifier assetIdentifier, Assembly hintAssembly = null)
        {
            switch (assetIdentifier.Type)
            {
                case AssetType.Unknown:
                    if (StringComparer.Ordinal.Equals("Overload", assetIdentifier.TypeMarker))
                    {
                        return this.ResolveOverloads(assetIdentifier, hintAssembly);
                    }
                    throw new ArgumentException(string.Format("Type '{0}' not supported.", assetIdentifier.TypeMarker),
                                                "assetIdentifier");

                case AssetType.Namespace:
                    return assetIdentifier.AssetId.Substring(assetIdentifier.TypeMarker.Length + 1);
                case AssetType.Type:
                    return this.ResolveType(assetIdentifier, hintAssembly);
                case AssetType.Method:
                    return this.ResolveMethod(assetIdentifier, hintAssembly);
                case AssetType.Field:
                    return this.ResolveField(assetIdentifier, hintAssembly);
                case AssetType.Event:
                    return this.ResolveEvent(assetIdentifier, hintAssembly);
                case AssetType.Property:
                    return this.ResolveProperty(assetIdentifier, hintAssembly);
                case AssetType.Assembly:
                    return this.ResolveAssembly(assetIdentifier, hintAssembly);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MethodInfo[] ResolveOverloads(AssetIdentifier assetIdentifier, Assembly hintAssembly)
        {
            string asset = assetIdentifier.AssetId.Substring(assetIdentifier.TypeMarker.Length + 1);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex, hintAssembly);

            string methodName = asset.Substring(startIndex + 1);

            var allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            return allMethods
                .Where(m => m.Name.Equals(methodName, StringComparison.Ordinal))
                .ToArray();
        }

        private object ResolveField(AssetIdentifier assetIdentifier, Assembly hintAssembly)
        {
            string asset = assetIdentifier.AssetId.Substring(assetIdentifier.TypeMarker.Length + 1);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex, hintAssembly);

            if (type.IsEnum)
            {
                MemberInfo[] members =
                    type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                    BindingFlags.Static);

                foreach (MemberInfo memberInfo in members)
                {
                    AssetIdentifier ret = AssetIdentifier.FromMemberInfo(memberInfo);
                    if (ret.AssetId == assetIdentifier.AssetId)
                        return memberInfo;
                }

                return null;
            }
            else
            {
                FieldInfo[] allFields =
                    type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                   BindingFlags.Instance);

                return allFields.Single(f => Naming.GetAssetId(f).Equals(assetIdentifier.AssetId));
            }
        }

        private EventInfo ResolveEvent(AssetIdentifier assetIdentifier, Assembly hintAssembly)
        {
            string asset = assetIdentifier.AssetId.Substring(assetIdentifier.TypeMarker.Length + 1);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex, hintAssembly);

            EventInfo[] allEvents =
                type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                               BindingFlags.Instance);

            return allEvents.Single(e => Naming.GetAssetId(e).Equals(assetIdentifier.AssetId));
        }

        private object ResolveAssembly(AssetIdentifier assetId, Assembly hintAssembly)
        {
            IEnumerable<Assembly> referencedAssemblies =
                this._assemblies.SelectMany(
                                            a =>
                                            a.GetReferencedAssemblies().Select(
                                                                               n =>
                                                                               Assembly.ReflectionOnlyLoad(n.FullName)));

            IEnumerable<Assembly> assemblies = this._assemblies.Concat(referencedAssemblies);
            foreach (Assembly assembly in assemblies)
            {
                if (AssetIdentifier.FromAssembly(assembly).Equals(assetId))
                    return assembly;
            }

            return null;
        }

        private MemberInfo ResolveMethod(AssetIdentifier assetId, Assembly hintAssembly)
        {
            string asset = assetId.AssetId.Substring(2);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex, hintAssembly);

            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            var allMethods =
                type.GetMethods(bindingFlags)
                    .Concat<MethodBase>(type.GetConstructors(bindingFlags));

            MethodBase[] methods =
                allMethods.Where(
                    m =>
                    (m is ConstructorInfo &&
                     assetId.AssetId.Equals(Naming.GetAssetId((ConstructorInfo)m),
                                    StringComparison.Ordinal)) ||
                    (m is MethodInfo &&
                     assetId.AssetId.Equals(Naming.GetAssetId((MethodInfo)m), StringComparison.Ordinal)))
                          .ToArray();


            // if there is a "new" method on the type we'll find both it and the method it hides.
            if (methods.Length == 2 && methods[1].DeclaringType == type)
                return methods[1];

            Debug.Assert(methods.Length == 1 || methods.Length == 2,
             string.Format("Found {0} methods, expected 1 or 2.", methods.Length));

            return methods[0];
        }

        private PropertyInfo ResolveProperty(AssetIdentifier assetId, Assembly hintAssembly)
        {
            string asset = assetId.AssetId.Substring(2);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex, hintAssembly);

            PropertyInfo[] allProps =
                type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                   BindingFlags.Instance);
            PropertyInfo[] properties =
                allProps
                    .Where(p => assetId.AssetId.Equals(Naming.GetAssetId(p), StringComparison.Ordinal))
                    .ToArray();

            // if there is a "new" property on the type we'll find both it and the property it hides.
            if (properties.Length == 2 && properties[1].DeclaringType == type)
                return properties[1];

            Debug.Assert(properties.Length == 1 || properties.Length == 2,
                         string.Format("Found {0} properties, expected 1 or 2.", properties.Length));

            return properties[0];
        }

        private bool TryGetType(string typeName, Assembly hintAssembly, out Type type)
        {
            List<Type> allMatches = new List<Type>();
            foreach (var assembly in this.GetAssemblies(hintAssembly))
            {
                type = assembly.GetType(typeName, false, false);
                if (type != null)
                    allMatches.Add(type);
            }

            if (allMatches.Count > 1)
            {
                TraceSources.AssetResolverSource.TraceWarning("More than one type ({0}) found: {1}",
                                                              typeName,
                                                              string.Join(", ",
                                                                          allMatches.Select(t => t.Assembly.GetName().Name)));
            }

            if (allMatches.Count > 0)
                type = allMatches[0];
            else
                type = null;

            return type != null;
        }

        private IEnumerable<Assembly> GetAssemblies(Assembly hintAssembly)
        {
            if (hintAssembly == null)
                return this._assemblies;

            // TODO probably cache this
            return this.GetAssembliesInternal(new HashSet<Assembly>(), hintAssembly);
        }

        private static bool Equals(AssemblyName n1, AssemblyName n2)
        {
            return string.Equals(n1.FullName, n2.FullName, StringComparison.Ordinal);
        }

        private IEnumerable<Assembly> GetAssembliesInternal(HashSet<Assembly> seen, Assembly assembly)
        {
            if (!seen.Add(assembly))
                yield break;

            yield return assembly;

            var references = assembly.GetReferencedAssemblies();

            foreach (var assemblyName in references)
            {
                yield return this._assemblies.Single(a => Equals(a.GetName(), assemblyName));
            }

            foreach (var assemblyName in references)
            {
                var asm = this._assemblies.Single(a => Equals(a.GetName(), assemblyName));
                foreach (Assembly ret in this.GetAssembliesInternal(seen, asm))
                    yield return ret;
            }
        }

        private object ResolveType(AssetIdentifier assetId, Assembly hintAssembly)
        {
            string typeName = assetId.AssetId.Substring(assetId.TypeMarker.Length + 1);
            int startIndex = 0;
            return this.GetDeclaringType(typeName, ref startIndex, hintAssembly);
        }


        protected internal Type GetDeclaringType(string typeName, ref int startIndex, Assembly hintAssembly)
        {
            Type ret = null;

            string[] fragments = typeName.Substring(startIndex).Split('.');

            for (int i = 0; i < fragments.Length; i++)
            {
                int fragmentEndsAt = fragments[i].IndexOfAny(new[] { '{', '+' });
                string fragment;

                fragment = fragmentEndsAt == -1 ? fragments[i] : fragments[i].Substring(0, fragmentEndsAt);

                if (ret == null)
                {
                    string possibleTypeName = string.Join(".", fragments, 0, i);

                    if (possibleTypeName.Length <= 0)
                        possibleTypeName = fragment;
                    else
                        possibleTypeName += '.' + fragment;

                    if (this.TryGetType(possibleTypeName, hintAssembly, out ret))
                        startIndex += possibleTypeName.Length;
                    else if (fragmentEndsAt >= 0 &&
                             this.TryGetType(possibleTypeName + fragments[i].Substring(fragmentEndsAt), hintAssembly, out ret))
                    {
                        startIndex += possibleTypeName.Length + (fragments[i].Length - fragmentEndsAt);
                        fragmentEndsAt = -1;
                    }
                }
                else
                {
                    Type nested = ret.GetNestedType(fragment, BindingFlags.Public | BindingFlags.NonPublic);
                    if (nested == null)
                        break;

                    ret = nested;
                    // +1 to account for the seperator that is requried for nested types
                    startIndex += fragment.Length + 1;
                }

                if (fragmentEndsAt != -1 && fragments[i][fragmentEndsAt] == '{')
                {
                    Debug.Assert(ret != null);
                    AssetIdentifier[] typeArgs = AssetIdentifier.ParseTypeArgs(typeName, ref startIndex).ToArray();
                    Type[] genTypeArgs = new Type[typeArgs.Length];

                    for (int j = 0; j < typeArgs.Length; j++)
                        genTypeArgs[j] = (Type)this.ResolveType(typeArgs[j], hintAssembly);

                    ret = ret.MakeGenericType(genTypeArgs);
                    break;
                }
            }

            return ret;
        }
    }
}
