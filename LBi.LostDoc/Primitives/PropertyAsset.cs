using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class PropertyAsset : MemberAsset
    {
        protected PropertyAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Property, "Invalid AssetIdentifier for PropertyAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitProperty(this);
        }
    }
}