using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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

        public IEnumerable<AssetIdentifier> GetAssetHierarchy(AssetIdentifier assetId)
        {
            yield return assetId;

            switch (assetId.Type)
            {
                case AssetType.Namespace:
                    string ns = (string)this.Resolve(assetId);
                    Assembly[] matchingAssemblies =
                        this._assemblies.Where(a => a.GetName().Version == assetId.Version).Where(
                                                                                                  a =>
                                                                                                  a.GetTypes().Any(
                                                                                                                   t1 =>
                                                                                                                   t1.
                                                                                                                       Namespace !=
                                                                                                                   null &&
                                                                                                                   (StringComparer
                                                                                                                        .
                                                                                                                        Ordinal
                                                                                                                        .
                                                                                                                        Equals
                                                                                                                        (t1
                                                                                                                             .
                                                                                                                             Namespace,
                                                                                                                         ns) ||
                                                                                                                    t1.
                                                                                                                        Namespace
                                                                                                                        .
                                                                                                                        StartsWith
                                                                                                                        (ns +
                                                                                                                         ".",
                                                                                                                         StringComparison
                                                                                                                             .
                                                                                                                             Ordinal))))
                            .ToArray();

                    if (matchingAssemblies.Length == 0)
                        throw new InvalidOperationException("Found no assembly containing namespace: " + ns);


// if (matchingAssemblies.Length > 1)
                    // throw new AmbiguousMatchException("Found more than one assembly containing namespace: " + ns);
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

        private object ResolveInternal(AssetIdentifier assetIdentifier)
        {
            switch (assetIdentifier.Type)
            {
                case AssetType.Unknown:
                    throw new ArgumentOutOfRangeException("assetIdentifier", "Cannot resolve asset of unknown type.");
                case AssetType.Namespace:
                    return assetIdentifier.AssetId.Substring(assetIdentifier.TypeMarker.Length + 1);
                case AssetType.Type:
                    return this.ResolveType(assetIdentifier);
                case AssetType.Method:
                    return this.ResolveMethod(assetIdentifier.AssetId);
                case AssetType.Field:
                    return this.ResolveField(assetIdentifier);
                case AssetType.Event:
                    return this.ResolveEvent(assetIdentifier);
                case AssetType.Property:
                    return this.ResolveProperty(assetIdentifier.AssetId);
                case AssetType.Assembly:
                    return this.ResolveAssembly(assetIdentifier);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object ResolveField(AssetIdentifier assetIdentifier)
        {
            string asset = assetIdentifier.AssetId.Substring(assetIdentifier.TypeMarker.Length + 1);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex);

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

        private EventInfo ResolveEvent(AssetIdentifier assetIdentifier)
        {
            string asset = assetIdentifier.AssetId.Substring(assetIdentifier.TypeMarker.Length + 1);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex);

            EventInfo[] allEvents =
                type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                               BindingFlags.Instance);

            return allEvents.Single(e => AssetIdentifier.FromMemberInfo(e).Equals(assetIdentifier));
        }

        private object ResolveAssembly(AssetIdentifier assetId)
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

        private MemberInfo ResolveMethod(string assetId)
        {
            string asset = assetId.Substring(2);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex);

            MethodBase[] allMethods =
                type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                BindingFlags.Instance).Concat<MethodBase>(
                                                                          type.GetConstructors(BindingFlags.Public |
                                                                                               BindingFlags.NonPublic |
                                                                                               BindingFlags.Static |
                                                                                               BindingFlags.Instance)).
                    ToArray();

            MethodBase method =
                allMethods.SingleOrDefault(
                                           m =>
                                           (m is ConstructorInfo &&
                                            assetId.Equals(Naming.GetAssetId((ConstructorInfo)m),
                                                           StringComparison.Ordinal)) ||
                                           (m is MethodInfo &&
                                            assetId.Equals(Naming.GetAssetId((MethodInfo)m), StringComparison.Ordinal)));

            return method;
        }

        // private ConstructorInfo ResolveConstructor(AssetIdentifier assetId)
        // {

        // string asset = assetId.AssetId.Substring(assetId.TypeMarker.Length + 1);

        // int startIndex = 0;
        // Type type = GetDeclaringType(asset, ref startIndex);

        // var allCtors =
        // type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
        // BindingFlags.Instance);


        // return allCtors.Single(m => assetId.Equals(AssetIdentifier.FromType(m)));
        // }

        private PropertyInfo ResolveProperty(string assetId)
        {
            string asset = assetId.Substring(2);

            int startIndex = 0;
            Type type = this.GetDeclaringType(asset, ref startIndex);

            PropertyInfo[] allProps =
                type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                   BindingFlags.Instance);
            PropertyInfo method =
                allProps.Single(p => assetId.Equals(Naming.GetAssetId(p), StringComparison.Ordinal));
            return method;
        }

        private bool TryGetType(string typeName, out Type type)
        {
            Type[] candidates =
                this._assemblies.Select(a => a.GetType(typeName, false, false)).Where(t => t != null).ToArray();
            type = candidates.Distinct().SingleOrDefault(t => t != null);
            return type != null;
        }

        private object ResolveType(AssetIdentifier assetId)
        {
            string typeName = assetId.AssetId.Substring(assetId.TypeMarker.Length + 1);
            int startIndex = 0;
            return this.GetDeclaringType(typeName, ref startIndex);
        }


        protected internal Type GetDeclaringType(string typeName, ref int startIndex)
        {
            Type ret = null;

            string[] fragments = typeName.Substring(startIndex).Split('.');

            for (int i = 0; i < fragments.Length; i++)
            {
                int fragmentEndsAt = fragments[i].IndexOfAny(new[] {'{', '+'});
                string fragment;

                fragment = fragmentEndsAt == -1 ? fragments[i] : fragments[i].Substring(0, fragmentEndsAt);

                if (ret == null)
                {
                    string possibleTypeName = string.Join(".", fragments, 0, i);

                    if (possibleTypeName.Length <= 0)
                        possibleTypeName = fragment;
                    else
                        possibleTypeName += '.' + fragment;

                    if (this.TryGetType(possibleTypeName, out ret))
                        startIndex += possibleTypeName.Length;
                }
                else
                {
                    Type nested = ret.GetNestedType(fragment, BindingFlags.Public | BindingFlags.NonPublic);
                    if (nested == null)
                        break;

                    ret = nested;
                    startIndex += fragment.Length + 1;


// +1 to account for the seperator that is requried for nested types
                }

                if (fragmentEndsAt != -1 && fragments[i][fragmentEndsAt] == '{')
                {
                    Debug.Assert(ret != null);
                    AssetIdentifier[] typeArgs = AssetIdentifier.ParseTypeArgs(typeName, ref startIndex).ToArray();
                    Type[] genTypeArgs = new Type[typeArgs.Length];

                    for (int j = 0; j < typeArgs.Length; j++)
                        genTypeArgs[j] = (Type)this.ResolveType(typeArgs[j]);

                    ret = ret.MakeGenericType(genTypeArgs);
                    break;
                }
            }

            return ret;
        }
    }
}