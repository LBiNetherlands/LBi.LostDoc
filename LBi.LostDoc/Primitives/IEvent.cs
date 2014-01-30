using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public interface IEvent : IMember
    {
        IMethod AddMethod { get; }

        IMethod RemoveMethod { get; }

        IDelegate EventHandlerType { get; }
    }
}