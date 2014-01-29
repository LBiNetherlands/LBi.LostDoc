namespace LBi.LostDoc.Primitives
{
    public abstract class ArrayAsset : TypeAsset
    {
        protected ArrayAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }

        public abstract int Rank { get; }

        public abstract TypeAsset ElementType { get; }
    }
}