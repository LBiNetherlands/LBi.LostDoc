using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public abstract class ReferenceTypeAsset : TypeAsset
    {
        protected ReferenceTypeAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }

        public abstract IEnumerable<InterfaceAsset> DeclaredInterfaces { get; }
        public abstract ReferenceTypeAsset BaseType { get; }
        public abstract bool IsSealed { get; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VistReferenceType(this);
        }
    }
}