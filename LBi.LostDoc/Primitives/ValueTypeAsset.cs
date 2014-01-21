namespace LBi.LostDoc.Primitives
{
    public abstract class ValueTypeAsset : TypeAsset
    {
        protected ValueTypeAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VistValueType(this);
        }
    }
}