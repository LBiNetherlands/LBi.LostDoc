using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LBi.LostDoc.Core.Filters
{
    public class CompilerGeneratedFilter : MemberInfoFilter
    {
        public override bool Filter(IFilterContext context, MemberInfo m)
        {
            IList<CustomAttributeData> data = m.GetCustomAttributesData();
            foreach (CustomAttributeData attrData in data)
            {
                if (attrData.Constructor.DeclaringType.Equals(typeof(CompilerGeneratedAttribute)))
                    return true;
            }

            return false;
        }
    }
}