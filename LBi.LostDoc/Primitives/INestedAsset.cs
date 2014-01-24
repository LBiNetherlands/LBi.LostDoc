namespace LBi.LostDoc.Primitives
{
    public interface INestedAsset<out T> where T : Asset
    {
        T DeclaringAsset { get; }
    }
}