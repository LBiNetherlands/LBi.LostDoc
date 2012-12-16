using System;

namespace LBi.LostDoc.Core.Templating
{
    public interface IAssetUriResolver
    {
        Uri ResolveAssetId(string assetId, Version version);
    }
}