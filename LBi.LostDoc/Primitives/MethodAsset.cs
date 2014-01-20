using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class MethodAsset : MemberAsset
    {
        protected MethodAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Method, "Invalid AssetIdentifier for MethodAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitMethod(this);
        }
    }
}