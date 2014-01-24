using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public interface IImplementsInterfaces
    {
        IEnumerable<InterfaceAsset> DeclaredInterfaces { get; }
    }
}