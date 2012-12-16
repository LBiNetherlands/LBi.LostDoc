using System;
using System.Collections.Generic;

namespace LBi.LostDoc.Core.Templating.AssetResolvers
{
    public class FileResolver : IAssetUriResolver
    {
        private Dictionary<string, Dictionary<Version, Uri>> _lookupCache =
            new Dictionary<string, Dictionary<Version, Uri>>();

        #region IAssetUriResolver Members

        public Uri ResolveAssetId(string assetId, Version version)
        {
            Uri ret;

            Dictionary<Version, Uri> innerDict;
            if (!this._lookupCache.TryGetValue(assetId, out innerDict)
                || !innerDict.TryGetValue(version, out ret))
                return ret = null;

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
    }
}