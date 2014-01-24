using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class NamespaceAsset : Asset, INestedAsset<Asset>
    {
        protected NamespaceAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Namespace, "Invalid AssetIdentifier for NamespaceAsset");

        }
        
        public override void Visit(IVisitor visitor)
        {
            visitor.VisitNamespace(this);
        }

        public Asset DeclaringAsset { get; private set; }
    }
}