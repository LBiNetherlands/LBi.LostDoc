using System.Reflection;

namespace LBi.LostDoc.Core
{
    public abstract class MemberInfoFilter : IAssetFilter
    {
        #region IAssetFilter Members

        public bool Filter(IFilterContext context, AssetIdentifier asset)
        {
            MemberInfo mi = context.AssetResolver.Resolve(asset) as MemberInfo;
            if (mi != null)
                return Filter(context, mi);

            return false;
        }

        #endregion

        public abstract bool Filter(IFilterContext context, MemberInfo m);
    }
}