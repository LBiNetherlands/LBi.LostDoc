using System;
using System.Reflection;

namespace LBi.LostDoc.Core
{
    public interface IEnricher
    {
        void EnrichType(IProcessingContext context, Type type);

        void EnrichConstructor(IProcessingContext context, ConstructorInfo ctor);

        void EnrichAssembly(IProcessingContext context, Assembly asm);

        void RegisterNamespace(IProcessingContext context);

        void EnrichMethod(IProcessingContext context, MethodInfo mInfo);

        void EnrichField(IProcessingContext context, FieldInfo fieldInfo);

        void EnrichProperty(IProcessingContext context, PropertyInfo propertyInfo);

        void EnrichReturnValue(IProcessingContext context, MethodInfo methodInfo);

        void EnrichParameter(IProcessingContext context, ParameterInfo item);

        void EnrichTypeParameter(IProcessingContext context, Type typeParameter);

        void EnrichNamespace(IProcessingContext context, string ns);

        void EnrichEvent(IProcessingContext clone, EventInfo eventInfo);
    }
}