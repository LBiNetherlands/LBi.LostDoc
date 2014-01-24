namespace LBi.LostDoc.Primitives
{
    public abstract class MemberAsset : Asset, INestedAsset<TypeAsset>
    {
        protected MemberAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }

        public abstract TypeAsset DeclaringAsset { get; }
    }
}