using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace LBi.LostDoc.Core
{
    public class AssetIdentifier : IEquatable<AssetIdentifier>
    {
        private readonly string _assetId;
        private readonly AssetType _type;
        private readonly Version _version;

        public AssetIdentifier(string assetId, Version version)
        {
            this._assetId = assetId;
            this._version = version;
            string assetMarker = this._assetId.Substring(0, this._assetId.IndexOf(':'));
            switch (assetMarker)
            {
                case "T":
                    this._type = AssetType.Type;
                    break;
                case "P":
                    this._type = AssetType.Property;
                    break;
                case "M":
                    this._type = AssetType.Method;
                    break;
                case "F":
                    this._type = AssetType.Field;
                    break;
                case "E":
                    this._type = AssetType.Event;
                    break;
                case "N":
                    this._type = AssetType.Namespace;
                    break;


// case "C":
                    // this._type = AssetType.Constructor;
                    // break;
                case "A":
                    this._type = AssetType.Assembly;
                    break;
                default:
                    this._type = AssetType.Unknown;
                    break;
            }
        }

        public AssetType Type
        {
            get { return this._type; }
        }

        public string AssetId
        {
            get { return this._assetId; }
        }

        public string TypeMarker
        {
            get { return this._assetId.Substring(0, this._assetId.IndexOf(':')); }
        }

        public Version Version
        {
            get { return this._version; }
        }

        public bool HasVersion
        {
            get { return this._version != null; }
        }

        #region IEquatable<AssetIdentifier> Members

        public bool Equals(AssetIdentifier other)
        {
            if (!StringComparer.Ordinal.Equals(this._assetId, other._assetId))
                return false;

            if (this._version != null && this._version != other._version)
                return false;

            return true;
        }

        #endregion

        public static AssetIdentifier Parse(string assetId)
        {
            int startIndex = 0;


// return Parse(assetId, ref startIndex);
            AssetIdentifier ret = ParseInternal(assetId, ref startIndex);
            Debug.Assert(startIndex == assetId.Length);
            return ret;
        }

        private static AssetIdentifier ParseInternal(string assetId, ref int startIndex)
        {
#if DBG_VERBOSE
            Debug.WriteLine("ParseInternal: " + assetId);
            Debug.WriteLine("               " + new string(' ', startIndex) + assetId.Substring(startIndex));
            Debug.WriteLine("               " + new string(' ', startIndex) + "^");
#endif


// trim leading whitespace
            while (char.IsWhiteSpace(assetId[startIndex]))
                ++startIndex;

            if (assetId[startIndex] == '{')
                return ParseComplex(assetId, ref startIndex);

            return ParseSimple(assetId, ref startIndex);
        }

        private static AssetIdentifier ParseSimple(string assetId, ref int startIndex)
        {
#if DBG_VERBOSE
            Debug.WriteLine("ParseSimple: " + assetId);
            Debug.WriteLine("             " + new string(' ', startIndex) + assetId.Substring(startIndex));
            Debug.WriteLine("             " + new string(' ', startIndex) + "^");
#endif
            string retAssetId;
            int origStartIx = startIndex;
            int endOfAsset = assetId.IndexOfAny(new[] {',', ' ', ')', '}'}, startIndex);

            int typeArgStart = assetId.IndexOf('{', startIndex);
            int paramStart = assetId.IndexOf('(', startIndex);

            if (typeArgStart != -1 && (typeArgStart < paramStart || paramStart == -1) &&
                (typeArgStart < endOfAsset || endOfAsset == -1))
            {
                ParseTypeArgs(assetId, ref typeArgStart);


// retAssetId = assetId.Substring(startIndex, typeArgStart - startIndex);
                startIndex = typeArgStart;
            }
            else if (endOfAsset == -1)
            {
                // retAssetId = assetId.Substring(startIndex);
                startIndex = assetId.Length;


// typeArgs = null;
            }
            else if (paramStart != -1 && paramStart < endOfAsset)
            {
                ParseParams(assetId, ref paramStart);


// retAssetId = assetId.Substring(startIndex, paramStart - startIndex);
                startIndex = paramStart;


// typeArgs = null;
            }
            else
            {
                // retAssetId = assetId.Substring(startIndex, endOfAsset - startIndex);
                startIndex = endOfAsset;


// typeArgs = null;
            }

            if (startIndex < assetId.Length)
            {
                if (assetId[startIndex] == '+' || assetId[startIndex] == '.' || assetId[startIndex] == '~')
                {
                    endOfAsset = startIndex;
                    ParseSimple(assetId, ref endOfAsset);


// retAssetId += assetId.Substring(startIndex, endOfAsset - startIndex);
                    startIndex = endOfAsset;
                }
            }

            retAssetId = assetId.Substring(origStartIx, startIndex - origStartIx);

            int typeMarker = retAssetId.IndexOf(':');

            if (typeMarker == -1 || typeMarker > 2)
                retAssetId = "T:" + retAssetId;

            var ret = new AssetIdentifier(retAssetId, null);

            return ret;
        }

        internal static IEnumerable<AssetIdentifier> ParseTypeArgs(string assetId, ref int startIndex)
        {
#if DBG_VERBOSE
            Debug.WriteLine("ParseTypeArgs: " + assetId);
            Debug.WriteLine("               " + new string(' ', startIndex) + assetId.Substring(startIndex));
            Debug.WriteLine("               " + new string(' ', startIndex) + "^");
#endif

            LinkedList<AssetIdentifier> ret = new LinkedList<AssetIdentifier>();

            if (assetId[startIndex] != '{')
                throw new ArgumentException("First char must be '{'", "assetId");


// skip opening {
            ++startIndex;

            do
            {
                // allows for malformed lists being parsed, but shouldn't be an issue.
                if (assetId[startIndex] == ',')
                    ++startIndex;

                // trim leading whitespace
                while (char.IsWhiteSpace(assetId[startIndex]))
                    ++startIndex;

                ret.AddLast(ParseInternal(assetId, ref startIndex));

                // trim leading whitespace
                while (char.IsWhiteSpace(assetId[startIndex]))
                    ++startIndex;
            }
 while (assetId[startIndex] == ',');

            // skip closing }
            Debug.Assert(assetId[startIndex] == '}');
            ++startIndex;
            return ret;
        }

        private static AssetIdentifier ParseComplex(string assetId, ref int startIndex)
        {
#if DBG_VERBOSE
            Debug.WriteLine("ParseComplex: " + assetId);
            Debug.WriteLine("              " + new string(' ', startIndex) + assetId.Substring(startIndex));
            Debug.WriteLine("              " + new string(' ', startIndex) + "^");
#endif
            AssetIdentifier ret;

            Version version = null;

            // trim leading whitespace
            while (char.IsWhiteSpace(assetId[startIndex]))
                ++startIndex;

            if (assetId[startIndex] != '{')
                throw new ArgumentException("First char must be '{'", "assetId");


// skip opening {
            ++startIndex;

            // trim leading whitespace
            while (char.IsWhiteSpace(assetId[startIndex]))
                ++startIndex;

            ret = ParseInternal(assetId, ref startIndex);

            // trim leading whitespace
            while (char.IsWhiteSpace(assetId[startIndex]))
                ++startIndex;

            if (assetId[startIndex] == ',')
            {
                // skip comma
                ++startIndex;


// trim leading whitespace
                while (char.IsWhiteSpace(assetId[startIndex]))
                    ++startIndex;

                if (assetId[startIndex] == 'V' && assetId[startIndex + 1] == ':')
                {
                    // skip marker
                    startIndex += 2;


// trim leading whitespace
                    while (char.IsWhiteSpace(assetId[startIndex]))
                        ++startIndex;

                    int endIndex = startIndex;
                    while (char.IsNumber(assetId[endIndex]) || assetId[endIndex] == '.')
                        ++endIndex;

                    version = Version.Parse(assetId.Substring(startIndex, endIndex - startIndex));
                    startIndex = endIndex;
                }
            }

            // trim leading whitspace
            while (char.IsWhiteSpace(assetId[startIndex]))
                ++startIndex;

            // skip closing }
            Debug.Assert(assetId[startIndex] == '}');
            ++startIndex;
            return new AssetIdentifier(ret.AssetId, version);
        }

        private static IEnumerable<AssetIdentifier> ParseParams(string assetId, ref int startIndex)
        {
#if DBG_VERBOSE
            Debug.WriteLine("ParseParams: " + assetId);
            Debug.WriteLine("             " + new string(' ', startIndex) + assetId.Substring(startIndex));
            Debug.WriteLine("             " + new string(' ', startIndex) + "^");
#endif
            LinkedList<AssetIdentifier> ret = new LinkedList<AssetIdentifier>();
            if (assetId[startIndex] != '(')
                throw new ArgumentException("First char must be '('", "assetId");

            do
            {
                // skip opening ( and ,
                startIndex++;

                // trim leading whitespace
                while (char.IsWhiteSpace(assetId[startIndex]))
                    ++startIndex;

                ret.AddLast(ParseInternal(assetId, ref startIndex));
            }
 while (assetId[startIndex] == ',');

            // skip closing )
            Debug.Assert(assetId[startIndex] == ')');
            startIndex++;
            return ret;
        }

        public static implicit operator string(AssetIdentifier id)
        {
            return id.ToString(true);
        }

        public static AssetIdentifier FromNamespace(string namespaceName, Version version)
        {
            return new AssetIdentifier(Naming.GetAssetId(namespaceName),
                                       version);
        }

        public static AssetIdentifier FromAssembly(Assembly asm)
        {
            return new AssetIdentifier("A:" + asm.GetName().Name, asm.GetName().Version);
        }

        public static AssetIdentifier FromType(Type type)
        {
            return FromMemberInfo(type);
        }

        public static AssetIdentifier FromMemberInfo(MemberInfo memberInfo)
        {
            Module declaringModule = (memberInfo.ReflectedType ?? memberInfo).Module;
            Version version = declaringModule.Assembly.GetName().Version;
            string assetId;

            if (memberInfo is Type)
            {
                assetId = Naming.GetAssetId((Type)memberInfo);
                Type t = (Type)memberInfo;
                if (t.IsGenericParameter)
                    version = (t.DeclaringMethod ?? (MemberInfo)t.ReflectedType).Module.Assembly.GetName().Version;
            }
            else if (memberInfo is ConstructorInfo)
            {
                assetId = Naming.GetAssetId((ConstructorInfo)memberInfo);
            }
            else if (memberInfo is PropertyInfo)
            {
                assetId = Naming.GetAssetId((PropertyInfo)memberInfo);
            }
            else if (memberInfo is MethodInfo)
            {
                assetId = Naming.GetAssetId((MethodInfo)memberInfo);
            }
            else if (memberInfo is EventInfo)
            {
                assetId = Naming.GetAssetId((EventInfo)memberInfo);
            }
            else if (memberInfo is FieldInfo)
            {
                assetId = Naming.GetAssetId((FieldInfo)memberInfo);
            }
            else
                throw new ArgumentOutOfRangeException("memberInfo",
                                                      string.Format("Invalid memeberInfo type: {0}",
                                                                    memberInfo.GetType().Name));


            return new AssetIdentifier(assetId, version);
        }


        public string ToString(bool includeVersion)
        {
            StringBuilder ret = new StringBuilder();

            if (includeVersion && this.Version != null)
                ret.Append('{');

            ret.Append(this.AssetId);

            if (includeVersion && this.Version != null)
                ret.Append(", V:").Append(this.Version).Append('}');


            return ret.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is AssetIdentifier)
                return this.Equals((AssetIdentifier)obj);

            return false;
        }

        public override int GetHashCode()
        {
            return this._assetId.GetHashCode();
        }

        public override string ToString()
        {
            return this.ToString(true);
        }
    }
}