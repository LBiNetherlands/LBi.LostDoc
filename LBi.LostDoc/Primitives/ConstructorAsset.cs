using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class ConstructorAsset : MemberAsset
    {
        protected ConstructorAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Method, "Invalid AssetIdentifier for ConstructorAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitConstructor(this);
        }
    }
}