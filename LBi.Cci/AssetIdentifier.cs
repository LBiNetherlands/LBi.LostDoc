using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cci;

namespace LBi.Cci
{
    public class AssetIdentifier : IEquatable<AssetIdentifier>, IComparable<AssetIdentifier>
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

            SkipWhitespace(assetId, ref startIndex);

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

            // Skip an explicit name, which is of the form <explicit interface>.<method name> where the explicit
            // interface name has . replaced by #, , by @ etc.
            // Note we assume that these are never nested in asset ids, and only occur once.
            int explicitEnd = assetId.LastIndexOf('#', assetId.Length - 1, assetId.Length - startIndex);
            if (explicitEnd > startIndex)
                startIndex = explicitEnd + 1;

            int endOfAsset = assetId.IndexOfAny(new[] { ',', ' ', ')', '}' }, startIndex);

            int typeArgStart = assetId.IndexOf('{', startIndex);
            int paramStart = assetId.IndexOf('(', startIndex);

            if (typeArgStart != -1 && (typeArgStart < paramStart || paramStart == -1) &&
                (typeArgStart < endOfAsset || endOfAsset == -1))
            {
                ParseTypeArgs(assetId, ref typeArgStart);

                startIndex = typeArgStart;

                // If a ref parameter there is a @ suffix.
                if (assetId[startIndex] == '@')
                    ++startIndex;
            }
            else if (endOfAsset == -1)
            {
                startIndex = assetId.Length;
            }
            else if (paramStart != -1 && paramStart < endOfAsset)
            {
                ParseParams(assetId, ref paramStart);

                startIndex = paramStart;
            }
            else
            {
                startIndex = endOfAsset;
            }

            if (startIndex < assetId.Length)
            {
                if (assetId[startIndex] == '+' || assetId[startIndex] == '.' || assetId[startIndex] == '~')
                {
                    endOfAsset = startIndex;
                    ParseSimple(assetId, ref endOfAsset);
                    startIndex = endOfAsset;
                }
            }

            retAssetId = assetId.Substring(origStartIx, startIndex - origStartIx);

            int typeMarker = retAssetId.IndexOf(':');

            if (typeMarker == -1)
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

                SkipWhitespace(assetId, ref startIndex);

                ret.AddLast(ParseInternal(assetId, ref startIndex));

                SkipWhitespace(assetId, ref startIndex);
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

            SkipWhitespace(assetId, ref startIndex);

            if (assetId[startIndex] != '{')
                throw new ArgumentException("First char must be '{'", "assetId");


            // skip opening {
            ++startIndex;

            SkipWhitespace(assetId, ref startIndex);

            ret = ParseInternal(assetId, ref startIndex);

            SkipWhitespace(assetId, ref startIndex);

            if (assetId[startIndex] == ',')
            {
                // skip comma
                ++startIndex;

                SkipWhitespace(assetId, ref startIndex);

                if (assetId[startIndex] == 'V' && assetId[startIndex + 1] == ':')
                {
                    // skip marker
                    startIndex += 2;

                    SkipWhitespace(assetId, ref startIndex);

                    int endIndex = startIndex;
                    while (char.IsNumber(assetId[endIndex]) || assetId[endIndex] == '.')
                        ++endIndex;

                    version = Version.Parse(assetId.Substring(startIndex, endIndex - startIndex));
                    startIndex = endIndex;
                }
            }

            SkipWhitespace(assetId, ref startIndex);

            // skip closing }
            Debug.Assert(assetId[startIndex] == '}');
            ++startIndex;
            return new AssetIdentifier(ret.AssetId, version);
        }

        private static void SkipWhitespace(string assetId, ref int startIndex)
        {
            // trim leading whitespace
            while (char.IsWhiteSpace(assetId[startIndex]))
                ++startIndex;
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

                SkipWhitespace(assetId, ref startIndex);

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

        public static AssetIdentifier FromNamespace(INamespaceDefinition nsDef)
        {
            IAssembly asm = (IAssembly)nsDef.RootOwner;
            return new AssetIdentifier("N:", asm.Version);
        }

        public static AssetIdentifier FromAssembly(IAssembly asm)
        {
            return new AssetIdentifier("A:" + asm.Name, asm.Version);
        }

        public static AssetIdentifier FromType(ITypeReference type)
        {
            string assetId = TypeHelper.GetTypeName(type, NameFormattingOptions.DocumentationId);
            IAssembly asm = type.GetAssembly();
            return new AssetIdentifier(assetId, asm.Version);
        }

        public static AssetIdentifier FromMemberInfo(ITypeMemberReference memberRef)
        {
            string assetId = MemberHelper.GetMemberSignature(memberRef, NameFormattingOptions.DocumentationId);

            IAssembly asm = memberRef.ContainingType.GetAssembly();

            return new AssetIdentifier(assetId, asm.Version);
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
            AssetIdentifier other = obj as AssetIdentifier;
            return other != null && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this._assetId.GetHashCode();
        }

        public int CompareTo(AssetIdentifier other)
        {
            if (other == null)
                return -1;

            int ret = StringComparer.Ordinal.Compare(this.AssetId, other.AssetId);

            if (ret == 0)
                ret = this.Version.CompareTo(other.Version);

            return ret;
        }

        public override string ToString()
        {
            return this.ToString(true);
        }
    }
}
