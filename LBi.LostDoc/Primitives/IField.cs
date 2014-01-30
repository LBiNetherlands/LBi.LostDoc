using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public interface IField : IMember
    {
        IType FieldType { get; }
    }
}