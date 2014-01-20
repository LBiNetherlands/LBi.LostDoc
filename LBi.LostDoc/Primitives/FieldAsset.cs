using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class FieldAsset : MemberAsset
    {
        protected FieldAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Field, "Invalid AssetIdentifier for FieldAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitField(this);
        }
    }
}