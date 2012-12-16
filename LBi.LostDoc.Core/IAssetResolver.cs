using System.Collections.Generic;
using System.Reflection;

namespace LBi.LostDoc.Core
{
    public interface IAssetResolver
    {
        IEnumerable<Assembly> Context { get; }
        object Resolve(AssetIdentifier assetId);
        IEnumerable<AssetIdentifier> GetAssetHierarchy(AssetIdentifier assetId);


// IEnumerable<AssetIdentifier> GetParents(AssetIdentifier assetId);
    }
}