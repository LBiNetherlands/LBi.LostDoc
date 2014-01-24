using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public interface IGenericType
    {
        bool ContainsTypeParameters { get; }
        IEnumerable<TypeParameter> DeclaredTypeParameters { get; }
    }
}