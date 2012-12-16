using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using LBi.Diagnostics;
using LBi.LostDoc.Core.Diagnostics;

namespace LBi.LostDoc.Core.Templating
{
    public class TemplateXsltExtensions
    {
        private readonly Dictionary<string, AssetIdentifier> _aidCache;
        private readonly Dictionary<AssetIdentifier, string> _resolveCache;
        private readonly ITemplatingContext _context;
        private readonly Uri _currentUri;
        private readonly IAssetUriResolver[] _resolvers;

        public TemplateXsltExtensions(ITemplatingContext context, Uri currentUri, IAssetUriResolver[] resolvers)
        {
            this._context = context;
            this._currentUri = currentUri;
            this._resolvers = resolvers;
            this._aidCache = new Dictionary<string, AssetIdentifier>();
            this._resolveCache = new Dictionary<AssetIdentifier, string>();
        }

        private AssetIdentifier Parse(string str)
        {
            AssetIdentifier ret;
            if (!this._aidCache.TryGetValue(str, out ret))
                this._aidCache.Add(str, ret = AssetIdentifier.Parse(str));
            return ret;
        }

        public string resolve(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
                return "urn:null-asset";

            AssetIdentifier aid = this.Parse(assetId);
            if (aid.HasVersion)
                return this.resolveAsset(aid.AssetId, aid.Version.ToString());
            else
                return this.resolveAsset(aid.AssetId, null);
        }

        public string resolveAsset(string assetId, string version)
        {
            string ret = null;

            AssetIdentifier aid = null;
            // perform asset redirect
            if (!string.IsNullOrEmpty(version))
            {
                aid = new AssetIdentifier(assetId, Version.Parse(version));
                AssetIdentifier targetAid;
                if (this._context.AssetRedirects.TryGet(aid, out targetAid))
                {
                    assetId = targetAid.AssetId;
                    version = targetAid.Version.ToString();
                    TraceSources.AssetResolveSource.TraceVerbose("Redirected assetd {0} => {1}", aid, targetAid);
                }
            }

            if (aid == null)
                aid = new AssetIdentifier(assetId, new Version());

            // TODO this doesn't seem very threadsafe, use concurrent dictionary?
            if (!_resolveCache.TryGetValue(aid, out ret))
            {
                for (int i = 0; i < this._resolvers.Length; i++)
                {
                    Uri target = this._resolvers[i].ResolveAssetId(assetId,
                                                                   string.IsNullOrEmpty(version)
                                                                       ? null
                                                                       : new Version(version));
                    if (target != null)
                    {
                        if (!target.IsAbsoluteUri)
                            target = this.MakeRelative(target);

                        ret = target.ToString();
                        break;
                    }
                }
                _resolveCache.Add(aid, ret);
            }

            if (ret == null)
            {
                ret = "urn:asset-not-found:" + assetId + "," + version;
                TraceSources.AssetResolveSource.TraceWarning("{0}, {1} => {2}", assetId, version, ret);
            }
            else
                TraceSources.AssetResolveSource.TraceVerbose("{0}, {1} => {2}", assetId, version, ret);


            return ret;
        }

        public bool canResolve(string assetId)
        {
            string ret = this.resolve(assetId);

            return !ret.StartsWith("urn:asset-not-found");
        }

        public string version(string fqn)
        {
            AssetIdentifier qn = this.Parse(fqn);
            return qn.Version.ToString();
        }


        public object iif(bool pred, object then, object otherwise)
        {
            return pred ? then : otherwise;
        }

        public string significantVersion(string fqn)
        {
            AssetIdentifier qn = this.Parse(fqn);

            if (this._context.IgnoredVersionComponent.HasValue)
                return qn.Version.ToString((int)this._context.IgnoredVersionComponent.Value);

            return qn.Version.ToString();
        }


        public string coalesce(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
                return second;
            return first;
        }

        public string asset(string aid)
        {
            AssetIdentifier ai = this.Parse(aid);
            return ai.AssetId;
        }

        public string resource(string resourceUri)
        {
            Uri targetUri = new Uri(resourceUri, UriKind.RelativeOrAbsolute);
            return this.MakeRelative(targetUri).ToString();
        }

        public bool cmpnover(string aid1, string aid2)
        {
            AssetIdentifier ai1 = this.Parse(aid1);
            AssetIdentifier ai2 = this.Parse(aid2);
            if (ai1.AssetId.Equals(ai2.AssetId, StringComparison.Ordinal))
                return ai1.Version != ai2.Version;
            return false;
        }

        public string nover(string aid1)
        {
            return new AssetIdentifier(this.Parse(aid1).AssetId, new Version(0, 0, 0, 0));
        }

        public string substringBeforeLast(string str, string last)
        {
            int lastIndexOf = str.LastIndexOf(last, StringComparison.OrdinalIgnoreCase);
            if (lastIndexOf == -1)
                return string.Empty;
            return str.Substring(0, lastIndexOf);
        }

        public string join(object iterator, string sep)
        {
            string ret = string.Join(sep, ((IEnumerable)iterator).Cast<XPathNavigator>().Select(n => n.ToString()));
            return ret;
        }

        private Uri MakeRelative(Uri target)
        {
            string targetStr = target.ToString();
            string[] targetFragments = targetStr.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string currentStr = this._currentUri.ToString();
            string[] currentFragments = currentStr.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            int maxCommonFragments = Math.Min(targetFragments.Length - 1, currentFragments.Length - 1);


            // foo/bar/baz/lur
            // foo/bar/pul/fur
            // 0   1   2   3
            int j;
            for (j = 0; j < maxCommonFragments; j++)
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(currentFragments[j], targetFragments[j]))
                    break;
            }

            StringBuilder relativeUri = new StringBuilder();
            for (int k = 0; k < maxCommonFragments - j; k++)
                relativeUri.Append("../");

            if (maxCommonFragments == 0)
            {
                for (int i = 0; i < currentFragments.Length - 1; i++)
                    relativeUri.Append("../");
            }

            for (int k = j; k < targetFragments.Length; k++)
            {
                relativeUri.Append(targetFragments[k]);
                if (k < targetFragments.Length - 1)
                    relativeUri.Append('/');
            }

            target = new Uri(relativeUri.ToString(), UriKind.Relative);
            return target;
        }
    }
}