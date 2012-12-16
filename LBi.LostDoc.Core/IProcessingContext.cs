using System.Collections.Generic;
using System.Xml.Linq;

namespace LBi.LostDoc.Core
{
    public interface IProcessingContext : IContextBase
    {
        XElement Element { get; }
        IEnumerable<AssetIdentifier> References { get; }
        IAssetResolver AssetResolver { get; }
        int Phase { get; }
        bool AddReference(string assetId);
        IProcessingContext Clone(XElement newElement);
        bool IsFiltered(AssetIdentifier interfaceAssetId);
    }
}