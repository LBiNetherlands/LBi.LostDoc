using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public interface IImplementsInterfaces
    {
        IEnumerable<IInterface> DeclaredInterfaces { get; }
    }
}