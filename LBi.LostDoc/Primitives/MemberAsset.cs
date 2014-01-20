namespace LBi.LostDoc.Primitives
{
    public abstract class MemberAsset : Asset
    {
        protected MemberAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }
    }
}