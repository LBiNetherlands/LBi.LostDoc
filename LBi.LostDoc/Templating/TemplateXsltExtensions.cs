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
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Xml.XPath;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Templating
{
    public class TemplateXsltExtensions
    {
        private readonly ConcurrentDictionary<string, AssetIdentifier> _aidCache;
        private readonly ConcurrentDictionary<AssetIdentifier, string> _resolveCache;
        private readonly ITemplatingContext _context;
        private readonly Uri _currentUri;

        public TemplateXsltExtensions(ITemplatingContext context, Uri currentUri)
        {
            this._context = context;
            this._currentUri = currentUri;

            this._aidCache = new ConcurrentDictionary<string, AssetIdentifier>();
            this._resolveCache = new ConcurrentDictionary<AssetIdentifier, string>();
        }

        private AssetIdentifier Parse(string str)
        {
            return _aidCache.GetOrAdd(str, AssetIdentifier.Parse);
        }

        private string ResolveAssetIdentifier(AssetIdentifier aid)
        {
            for (int i = 0; i < this._context.AssetUriResolvers.Length; i++)
            {
                Uri target = this._context.AssetUriResolvers[i].ResolveAssetId(aid.AssetId,
                                                                               aid.HasVersion ? aid.Version : null);
                if (target != null)
                {
                    if (!target.IsAbsoluteUri)
                        target = this._currentUri.GetRelativeUri(target);

                    return target.ToString();
                }
            }
            return null;
        }

        

        #region Extension methods
        // ReSharper disable InconsistentNaming

        public string resolve(string assetId)
        {
            string ret;
            if (string.IsNullOrEmpty(assetId))
                ret = "urn:null-asset";
            else
            {
                AssetIdentifier aid = this.Parse(assetId);
                ret = this.resolveAsset(aid.AssetId, aid.HasVersion ? aid.Version.ToString() : null);
            }
            return ret;
        }

        public string resolveAsset(string assetId, string version)
        {
            AssetIdentifier aid = null;
            // perform asset redirect
            if (!string.IsNullOrEmpty(version))
            {
                aid = new AssetIdentifier(assetId, Version.Parse(version));
                AssetIdentifier targetAid;
                if (this._context.TemplateData.AssetRedirects.TryGet(aid, out targetAid))
                {
                    aid = targetAid;
                    TraceSources.AssetResolverSource.TraceVerbose("Redirected assetd {0} => {1}", aid, targetAid);
                }
            }

            if (aid == null)
                aid = new AssetIdentifier(assetId, new Version());

            string ret = this._resolveCache.GetOrAdd(aid, this.ResolveAssetIdentifier);

            if (ret == null)
            {
                ret = "urn:asset-not-found:" + aid.AssetId + "," + aid.Version;
                TraceSources.AssetResolverSource.TraceWarning("{0}, {1} => {2}", aid.AssetId, aid.Version, ret);
            }
            else
                TraceSources.AssetResolverSource.TraceVerbose("{0}, {1} => {2}", aid.AssetId, aid.Version, ret);

            return ret;
        }

        public string toAssetId(string asset, string version)
        {
            return new AssetIdentifier(asset, Version.Parse(version)).ToString();
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

            if (this._context.TemplateData.IgnoredVersionComponent.HasValue)
                return qn.Version.ToString((int)this._context.TemplateData.IgnoredVersionComponent.Value);

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
            if (string.IsNullOrWhiteSpace(aid))
                throw new ArgumentException("Asset id cannot be empty.");
            AssetIdentifier ai = this.Parse(aid);
            return ai.AssetId;
        }

        public string relative(string uri)
        {
            Uri targetUri = new Uri(uri, UriKind.RelativeOrAbsolute);
            return this._currentUri.GetRelativeUri(targetUri).ToString();
        }
        
        public XPathNodeIterator key(string keyName, object value)
        {
            return this._context.DocumentIndex.Get(keyName, value);
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
            if (lastIndexOf <= 0)
                return string.Empty;
            return str.Substring(0, lastIndexOf);
        }

        public string substringAfterLast(string str, string last)
        {
            int lastIndexOf = str.LastIndexOf(last, StringComparison.OrdinalIgnoreCase);
            if (lastIndexOf == -1)
                return string.Empty;
            return str.Substring(lastIndexOf + 1);
        }

        public string join(object iterator, string sep)
        {
            string ret = string.Join(sep, ((IEnumerable)iterator).Cast<XPathNavigator>().Select(n => n.ToString()));
            return ret;
        }

        public string replace(string str, string target, string replacement)
        {
            return str.Replace(target, replacement);
        }

        public string @break()
        {
            System.Diagnostics.Debugger.Break();
            return string.Empty;
        }

        public string generator()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            AssemblyName name = asm.GetName();
            string appTitle = asm.GetCustomAttribute<AssemblyProductAttribute>().Product;
            return string.Format("{0} {1}", appTitle, name.Version);
        }

        // ReSharper restore InconsistentNaming
        #endregion
    }
}
