using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public abstract class InterfaceAsset : TypeAsset, IImplementsInterfaces, IGenericType
    {
        protected InterfaceAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }

        public abstract IEnumerable<InterfaceAsset> DeclaredInterfaces { get; }

        public abstract bool ContainsTypeParameters { get; }

        public abstract IEnumerable<TypeParameter> DeclaredTypeParameters { get; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VistInterface(this);
        }
    }
}