namespace LBi.LostDoc.Primitives
{
    public interface IVisitor
    {
        void VisitAssembly(IAssembly asset);
        void VisitNamespace(INamespace asset);
        void VisitField(IField asset);
        void VisitEvent(IEvent asset);
        void VisitProperty(IProperty asset);
        void VisitUnknown(IAsset asset);
        void VisitMethod(IMethod asset);
        void VisitConstructor(IConstructor asset);
        void VistEnum(IEnum asset);
        void VistInterface(IInterface asset);
        void VistReferenceType(IReferenceType asset);
        void VistValueType(IValueType asset);
        void VisitOperator(IOperator asset);
        void VisitDelegate(IDelegate asset);
    }
}