using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public interface ISignature
    {
        IType Returns { get; }

        IEnumerable<Parameter> Parameters { get; }
    }
}