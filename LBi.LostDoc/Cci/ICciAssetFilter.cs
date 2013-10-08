using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public interface ICciAssetFilter
    {
        bool Filter(ICciFilterContext context, IDefinition definition);
    }
}