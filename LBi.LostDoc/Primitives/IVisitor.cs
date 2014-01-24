namespace LBi.LostDoc.Primitives
{
    public interface IVisitor
    {
        void VisitAssembly(AssemblyAsset asset);
        void VisitNamespace(NamespaceAsset asset);
        void VisitField(FieldAsset asset);
        void VisitEvent(EventAsset asset);
        void VisitProperty(PropertyAsset asset);
        void VisitUnknown(Asset asset);
        void VisitMethod(MethodAsset asset);
        void VisitConstructor(ConstructorAsset asset);
        void VistEnum(EnumAsset asset);
        void VistInterface(InterfaceAsset asset);
        void VistReferenceType(ReferenceTypeAsset asset);
        void VistValueType(ValueTypeAsset asset);
        void VisitOperator(OperatorAsset asset);
        void VisitDelegate(DelegateAsset asset);
    }

    public interface IVisitor<out T>
    {
        T VisitAssembly(AssemblyAsset asset);
        T VisitNamespace(NamespaceAsset asset);
        T VisitType(TypeAsset asset);
        T VisitField(FieldAsset asset);
        T VisitEvent(EventAsset asset);
        T VisitProperty(PropertyAsset asset);
        T VisitUnknown(Asset asset);
        T VisitMethod(MethodAsset asset);
        T VisitConstructor(ConstructorAsset asset);
    }
}