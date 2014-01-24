namespace LBi.LostDoc.Primitives
{
    public abstract class EnumAsset : TypeAsset
    {
        protected EnumAsset(AssetIdentifier id, object target) : base(id, target)
        {
        }

        public abstract ValueTypeAsset UnderlyingType { get; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VistEnum(this);
        }
    }
}