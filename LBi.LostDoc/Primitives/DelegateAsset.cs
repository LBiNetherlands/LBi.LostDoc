using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public abstract class DelegateAsset : TypeAsset, IGenericType
    {
        protected DelegateAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }

        public abstract TypeAsset Returns { get; }

        public abstract IEnumerable<Parameter> Parameters { get; }

        public abstract bool ContainsTypeParameters { get; }

        public abstract IEnumerable<TypeParameter> DeclaredTypeParameters { get; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitDelegate(this);
        }

    }
}