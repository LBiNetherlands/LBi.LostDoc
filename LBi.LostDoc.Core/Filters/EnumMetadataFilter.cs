using System.Reflection;

namespace LBi.LostDoc.Core.Filters
{
    public class EnumMetadataFilter : IAssetFilter
    {
        #region IAssetFilter Members

        public bool Filter(IFilterContext context, AssetIdentifier asset)
        {
            object obj = context.AssetResolver.Resolve(asset);
            if (!(obj is FieldInfo) || !((FieldInfo)obj).DeclaringType.IsEnum)
                return false;

            return !(((FieldInfo)obj).IsStatic && ((FieldInfo)obj).IsPublic);
        }

        #endregion
    }
}