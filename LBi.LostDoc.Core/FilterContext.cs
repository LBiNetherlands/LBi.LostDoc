namespace LBi.LostDoc.Core
{
    public class FilterContext : IFilterContext
    {
        private readonly IAssetResolver _assetResolver;

        public FilterContext(IAssetResolver assetResolver)
        {
            this._assetResolver = assetResolver;
        }

        #region IFilterContext Members

        public IAssetResolver AssetResolver
        {
            get { return this._assetResolver; }
        }

        #endregion
    }
}