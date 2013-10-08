using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public interface ICciEnricher
    {
        void Enrich(ICciProcessingContext context, IDefinition definition);

        void RegisterNamespace(ICciProcessingContext context);
    }
}