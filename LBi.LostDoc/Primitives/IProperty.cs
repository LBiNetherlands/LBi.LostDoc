using System.Collections.Generic;

namespace LBi.LostDoc.Primitives
{
    public interface IProperty  : IMember, ISignature
    {      
        bool IsSpecialName { get; }
        
        IMethod SetMethod { get; }

        IMethod  GetMethod { get; }
    }
}