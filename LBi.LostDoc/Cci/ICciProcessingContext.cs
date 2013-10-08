using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public interface ICciProcessingContext
    {
        XElement Element { get; }
        int Phase { get; }
        bool AddReference(IDefinition asset);
        IEnumerable<IDefinition> References { get; }
        ICciProcessingContext Clone(XElement newElement);
        ICciProcessingContext Clone(int newPhase);
        bool IsFiltered(IDefinition asset);
    }
}