using System.Collections.Generic;
using System.Linq;

namespace Company.Project.Library
{
    public class AnotherInheritedGenericClass : GenericClass<IEnumerable<string>>
    {
        public AnotherInheritedGenericClass()
            : base(Enumerable.Empty<string>())
        {
        }
    }
}