using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public interface IReferenceType : IType, IImplementsInterfaces, IGenericType
    {
        IReferenceType BaseType { get; }
        bool IsSealed { get; }
    }
}