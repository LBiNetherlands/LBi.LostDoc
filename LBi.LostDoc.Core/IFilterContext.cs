namespace LBi.LostDoc.Core
{
    public interface IFilterContext : IContextBase
    {
        IAssetResolver AssetResolver { get; }
    }
}