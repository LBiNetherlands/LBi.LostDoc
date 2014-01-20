using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class OperatorAsset : MemberAsset
    {
        protected OperatorAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Method, "Invalid AssetIdentifier for OperatorAsset");
        }
    }
}