using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public abstract class ReferenceTypeAsset : TypeAsset, IImplementsInterfaces, IGenericType
    {
        protected ReferenceTypeAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }

        public abstract IEnumerable<InterfaceAsset> DeclaredInterfaces { get; }
        public abstract ReferenceTypeAsset BaseType { get; }
        public abstract bool IsSealed { get; }
        public abstract bool ContainsTypeParameters { get; }
        public abstract IEnumerable<TypeParameter> DeclaredTypeParameters { get; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VistReferenceType(this);
        }
    }
}