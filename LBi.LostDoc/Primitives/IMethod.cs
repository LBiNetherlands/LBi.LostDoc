using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public interface IMethod : IMember, ISignature
    {        
        bool IsAbstract { get; }

        bool IsVirtual { get; }
    }
}