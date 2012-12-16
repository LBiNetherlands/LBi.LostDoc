using System;

namespace LBi.LostDoc.Core
{
    public abstract class TypeFilter : IAssetFilter
    {
        #region IAssetFilter Members

        public bool Filter(IFilterContext context, AssetIdentifier asset)
        {
            Type t = context.AssetResolver.Resolve(asset) as Type;
            if (t != null)
                return Filter(context, t);

            return false;
        }

        #endregion

        protected abstract bool Filter(IFilterContext context, Type type);
    }

    public interface IAssetFilter
    {
        bool Filter(IFilterContext context, AssetIdentifier asset);
    }
}