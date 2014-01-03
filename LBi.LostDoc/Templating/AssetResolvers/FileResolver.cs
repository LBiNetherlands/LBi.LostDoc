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
using System.Collections.Generic;
using System.Linq;

namespace LBi.LostDoc.Templating.AssetResolvers
{
    public class FileResolver : IAssetUriResolver, IEnumerable<KeyValuePair<AssetIdentifier, Uri>>, IEqualityComparer<Uri>
    {
        private readonly Dictionary<string, Dictionary<Version, Uri>> _lookupCache;
        private readonly StringComparer _comparer;

        public FileResolver()
            : this(false)
        {
        }

        public FileResolver(bool caseSensitiveFs)
        {
            _lookupCache = new Dictionary<string, Dictionary<Version, Uri>>();
            _comparer = caseSensitiveFs ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        }

        #region IAssetUriResolver Members

        public Uri ResolveAssetId(string assetId, Version version)
        {
            Uri ret;

            Dictionary<Version, Uri> innerDict;
            if (!this._lookupCache.TryGetValue(assetId, out innerDict)
                || !innerDict.TryGetValue(version, out ret))
                ret = null;

            return ret;
        }

        #endregion

        public void Add(string assetId, Version version, Uri uri)
        {
            Dictionary<Version, Uri> innerDict;
            if (!this._lookupCache.TryGetValue(assetId, out innerDict))
                this._lookupCache.Add(assetId, innerDict = new Dictionary<Version, Uri>());

            if (!innerDict.ContainsKey(version))
                innerDict.Add(version, uri);
        }
       

        public IEnumerator<KeyValuePair<AssetIdentifier, Uri>> GetEnumerator()
        {
            var enumerable = (this._lookupCache.SelectMany(kvp => kvp.Value,
                                                           (kvp, innerKvp) =>
                                                           new KeyValuePair<AssetIdentifier, Uri>(
                                                               new AssetIdentifier(kvp.Key, innerKvp.Key),
                                                               innerKvp.Value)));

            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        bool IEqualityComparer<Uri>.Equals(Uri x, Uri y)
        {
            return this._comparer.Equals(x.ToString(), y.ToString());
        }

        int IEqualityComparer<Uri>.GetHashCode(Uri obj)
        {
            return this._comparer.GetHashCode(obj.ToString());
        }

        public void Clear()
        {
            this._lookupCache.Clear();
        }
    }
}
